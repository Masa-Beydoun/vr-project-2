using System.Collections.Generic;
using UnityEngine;

public class EnhancedCollisionManager : MonoBehaviour
{
    [Header("Broad Phase Configuration")]
    public BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.SweepAndPrune;
    [SerializeField] private float uniformGridCellSize = 1.0f;
    [SerializeField] private Vector3 octreeBounds = Vector3.one * 10000;

    [Header("simple Collision Handling")]
    private EnhancedCollisionHandler collisionHandler;
    [SerializeField] private SimpleCollisionHandler simpleHandler;

    [Header("Performance Settings")]
    [SerializeField] private int maxCollisionsPerFrame = 20; // Reduced from 100
    [SerializeField] private bool enableCollisionCaching = true;
    [SerializeField] private float cacheValidityTime = 0.1f; // Increased from 0.016f
    [SerializeField] private float minCollisionInterval = 0.05f; // NEW: Minimum time between same-pair collisions

    [Header("Penetration Control")]
    [SerializeField] private float maxAcceptablePenetration = 5.0f; // NEW: Reject extreme penetrations
    [SerializeField] private float minPenetrationDepth = 0.05f; // Increased minimum
    [SerializeField] private bool ignoreExtremeCollisions = true; // NEW: Skip extreme cases


    [Header("Debug Settings")]
    [SerializeField] private bool showBroadPhaseDebug = false;
    [SerializeField] private bool showNarrowPhaseDebug = false; // Turn off by default
    [SerializeField] private bool logCollisionStats = false;

    private PhysicalObject[] physicalObjects;
    private IBroadPhase broadPhase;
    private Dictionary<(int, int), float> collisionCache;
    private Dictionary<(int, int), float> lastCollisionTime; // NEW: Track last collision time
    private int collisionCount;
    private float lastFrameTime;

    // Statistics
    private int broadPhaseChecks;
    private int narrowPhaseChecks;
    private int actualCollisions;
    private int rejectedCollisions; // NEW: Track rejected collisions


    void Start()
    {
        InitializeBroadPhase();
        InitializeCollisionHandler();

        if (enableCollisionCaching)
        {
            collisionCache = new Dictionary<(int, int), float>();
            lastCollisionTime = new Dictionary<(int, int), float>();
        }
    }

    void FixedUpdate()
    {
        ResetFrameStatistics();

        // Get all physical objects in the scene
        physicalObjects = FindObjectsOfType<PhysicalObject>();

        if (physicalObjects.Length < 1) return;

        var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        broadPhaseChecks = candidatePairs.Count;

        ProcessCollisionPairs(candidatePairs);

        if (enableCollisionCaching)
        {
            CleanupCollisionCache();
        }

        if (logCollisionStats)
        {
            LogCollisionStatistics();
        }
    }
    private void InitializeBroadPhase()
    {
        switch (broadPhaseMethod)
        {
            case BroadPhaseMethod.SweepAndPrune:
                broadPhase = new SweepAndPrune();
                break;
            case BroadPhaseMethod.UniformGrid:
                broadPhase = new UniformGrid(cellSize: uniformGridCellSize);
                break;
            case BroadPhaseMethod.Octree:
                broadPhase = new Octree(new Bounds(Vector3.zero, octreeBounds));
                break;
            default:
                Debug.LogError("Unknown broad phase method, defaulting to SweepAndPrune");
                broadPhase = new SweepAndPrune();
                break;
        }
    }

    private void InitializeCollisionHandler()
    {
        // Initialize the simple collision handler
        simpleHandler = GetComponent<SimpleCollisionHandler>();
        if (simpleHandler == null)
        {
            Debug.LogWarning("SimpleCollisionHandler not found on " + gameObject.name);
        }
    }

    private bool ProcessCollisionPairs(List<(PhysicalObject, PhysicalObject)> candidatePairs)
    {
        bool hadCollisions = false;
        int processedThisFrame = 0;

        foreach (var pair in candidatePairs)
        {
            if (processedThisFrame >= maxCollisionsPerFrame) break;

            if (ShouldSkipCollision(pair.Item1, pair.Item2)) continue;

            bool collisionOccurred = HandleCollisionPair(pair.Item1, pair.Item2);
            if (collisionOccurred)
            {
                hadCollisions = true;
                processedThisFrame++;
            }
        }

        return hadCollisions;
    }

    private bool ShouldSkipCollision(PhysicalObject objA, PhysicalObject objB)
    {
        // Check collision cache
        if (enableCollisionCaching && IsCollisionCached(objA, objB))
        {
            return true;
        }

        // Check minimum collision interval
        if (IsCollisionTooRecent(objA, objB))
        {
            return true;
        }

        return false;
    }

    private bool IsCollisionTooRecent(PhysicalObject objA, PhysicalObject objB)
    {
        if (lastCollisionTime == null) return false;

        int idA = objA.GetInstanceID();
        int idB = objB.GetInstanceID();

        // Ensure consistent ordering
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        var key = (idA, idB);

        if (lastCollisionTime.TryGetValue(key, out float lastTime))
        {
            return (Time.fixedTime - lastTime) < minCollisionInterval;
        }

        return false;
    }

    private void RecordCollisionTime(PhysicalObject objA, PhysicalObject objB)
    {
        if (lastCollisionTime == null) return;

        int idA = objA.GetInstanceID();
        int idB = objB.GetInstanceID();

        // Ensure consistent ordering
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        var key = (idA, idB);
        lastCollisionTime[key] = Time.fixedTime;
    }

    private bool HandleCollisionPair(PhysicalObject objA, PhysicalObject objB)
    {
        // Get the systems/controllers for each object
        SpringMassSystem springSystemA = objA.GetComponentInParent<SpringMassSystem>();
        SpringMassSystem springSystemB = objB.GetComponentInParent<SpringMassSystem>();
        FEMController femControllerA = objA.GetComponentInParent<FEMController>();
        FEMController femControllerB = objB.GetComponentInParent<FEMController>();

        // Record collision time
        RecordCollisionTime(objA, objB);

        // Determine collision type and handle appropriately
        if (springSystemA != null && springSystemB != null)
        {
            return HandleSpringMassToSpringMass(springSystemA, springSystemB);
             
        }
        //else if (femControllerA != null && femControllerB != null)
        //{
        //    HandleFEMToFEM(femControllerA, femControllerB);
        //    return true;

        //}
        //else if (springSystemA != null && femControllerB != null)
        //{
        //    HandleSpringMassToFEM(springSystemA, femControllerB);
        //    return true;

        //}
        //else if (femControllerA != null && springSystemB != null)
        //{
        //    HandleFEMToSpringMass(femControllerA, springSystemB);
        //    return true;
        //}
        else
        {
            // Handle static object collisions or other cases
            HandleStaticCollision(objA, objB);
            return true;

        }
        return false;
    }

    // Replace your HandleSpringMassToSpringMass method with this:

    private bool HandleSpringMassToSpringMass(SpringMassSystem systemA, SpringMassSystem systemB)
    {
        if (systemA.allPoints.Count == 0 || systemB.allPoints.Count == 0)
            return false;

        List<CollisionResultEnhanced> collisions = CollisionDetector.CheckCollision(systemA, systemB);

        // Simple filtering - only check basic validity
        var validCollisions = new List<CollisionResultEnhanced>();

        foreach (var collision in collisions)
        {
            if (!collision.collided) continue;

            // Basic penetration check
            if (collision.penetrationDepth < 0.01f) continue;
            if (collision.penetrationDepth > 2.0f) continue; // Skip extreme penetrations

            validCollisions.Add(collision);
        }

        // Process only a few collisions per frame
        int processedCollisions = 0;
        const int maxCollisionsPerPair = 3;

        foreach (var collision in validCollisions)
        {
            if (processedCollisions >= maxCollisionsPerPair) break;

            // Use the simple handler
            //SimpleCollisionHandler simpleHandler = GetComponent<SimpleCollisionHandler>();
            if (simpleHandler != null)
            {
                simpleHandler.HandleSpringMassCollision(collision);
                actualCollisions++;
                processedCollisions++;
            }
        }

        return processedCollisions > 0;
    }
    
    //private void HandleFEMToFEM(FEMController controllerA, FEMController controllerB)
    //{
    //    if (controllerA.GetAllNodes().Length == 0 || controllerB.GetAllNodes().Length == 0)
    //        return;

    //    List<CollisionResultEnhanced_FEM> collisions = CollisionDetector_FEM.CheckCollision(controllerA, controllerB);

    //    foreach (var collision in collisions)
    //    {
    //        if (collision.collided)
    //        {
    //            collisionHandler.HandleFEMCollision(collision);
    //            actualCollisions++;
    //            collisionCount++;

    //            if (showNarrowPhaseDebug)
    //            {
    //                Debug.Log($"FEM Collision: {controllerA.gameObject.name}[{collision.pointA.ID}] <-> {controllerB.gameObject.name}[{collision.pointB.ID}]");
    //                Debug.DrawLine(collision.pointA.Position, collision.pointB.Position, Color.blue, 0.1f);
    //            }
    //        }
    //    }
    //}

    //private void HandleSpringMassToFEM(SpringMassSystem springSystem, FEMController femController)
    //{
    //    if (springSystem.allPoints.Count == 0 || femController.GetAllNodes().Length == 0)
    //        return;

    //    // Check collisions between spring-mass points and FEM nodes
    //    var massPoints = springSystem.allPoints;
    //    var femNodes = femController.GetAllNodes();

    //    foreach (var massPoint in massPoints)
    //    {
    //        foreach (var femNode in femNodes)
    //        {
    //            if (CheckPointToPointCollision(massPoint.position, femNode.Position, out Vector3 normal, out float penetration, out Vector3 contactPoint))
    //            {
    //                collisionHandler.HandleCollision(massPoint, femNode, normal, penetration, contactPoint);
    //                actualCollisions++;
    //                collisionCount++;

    //                if (showNarrowPhaseDebug)
    //                {
    //                    Debug.Log($"Mixed Collision: SpringMass[{massPoint.id}] <-> FEM[{femNode.ID}]");
    //                    Debug.DrawLine(massPoint.position, femNode.Position, Color.green, 0.1f);
    //                }
    //            }
    //        }
    //    }
    //}

    //private void HandleFEMToSpringMass(FEMController femController, SpringMassSystem springSystem)
    //{
    //    // Just call the reverse
    //    HandleSpringMassToFEM(springSystem, femController);
    //}

    private void HandleStaticCollision(PhysicalObject objA, PhysicalObject objB)
    {
        // Handle collisions with static objects
        // This would depend on your specific static collision detection implementation
        if (showNarrowPhaseDebug)
        {
            Debug.Log($"Static collision check between {objA.name} and {objB.name}");
        }
    }

    private bool CheckPointToPointCollision(Vector3 posA, Vector3 posB, out Vector3 normal, out float penetration, out Vector3 contactPoint)
    {
        Vector3 diff = posA - posB;
        float distance = diff.magnitude;
        float minDistance = 0.1f; // Adjust based on your needs

        if (distance < minDistance)
        {
            normal = diff.normalized;
            penetration = minDistance - distance;
            contactPoint = 0.5f * (posA + posB);
            return true;
        }

        normal = Vector3.zero;
        penetration = 0f;
        contactPoint = Vector3.zero;
        return false;
    }

    private bool IsCollisionCached(PhysicalObject objA, PhysicalObject objB)
    {
        int idA = objA.GetInstanceID();
        int idB = objB.GetInstanceID();

        // Ensure consistent ordering
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        var key = (idA, idB);

        if (collisionCache.TryGetValue(key, out float cachedTime))
        {
            return (Time.fixedTime - cachedTime) < cacheValidityTime;
        }

        return false;
    }

    private void CacheCollision(PhysicalObject objA, PhysicalObject objB)
    {
        if (!enableCollisionCaching) return;

        int idA = objA.GetInstanceID();
        int idB = objB.GetInstanceID();

        // Ensure consistent ordering
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        var key = (idA, idB);
        collisionCache[key] = Time.fixedTime;
    }

    private void CleanupCollisionCache()
    {
        if (collisionCache == null) return;

        float currentTime = Time.fixedTime;
        var keysToRemove = new List<(int, int)>();

        foreach (var kvp in collisionCache)
        {
            if (currentTime - kvp.Value > cacheValidityTime * 2)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            collisionCache.Remove(key);
        }

        // Also cleanup lastCollisionTime
        if (lastCollisionTime != null)
        {
            keysToRemove.Clear();
            foreach (var kvp in lastCollisionTime)
            {
                if (currentTime - kvp.Value > minCollisionInterval * 5)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                lastCollisionTime.Remove(key);
            }
        }
    }

    private void ResetFrameStatistics()
    {
        broadPhaseChecks = 0;
        narrowPhaseChecks = 0;
        actualCollisions = 0;
        //cachedCollisions = collisionCache?.Count ?? 0;
    }
    public CollisionStatistics GetCollisionStatistics()
    {
        return new CollisionStatistics
        {
            broadPhaseChecks = broadPhaseChecks,
            narrowPhaseChecks = narrowPhaseChecks,
            actualCollisions = actualCollisions,
            //cachedCollisions = collisionCache?.Count ?? 0
        };
    }
    private void LogCollisionStatistics()
    {
        Debug.Log($"Collision Stats - Broad Phase: {broadPhaseChecks}, Narrow Phase: {narrowPhaseChecks}, Actual Collisions: {actualCollisions}");
    }

    void OnDrawGizmos()
    {
        if (showBroadPhaseDebug && broadPhase != null)
        {
            // Draw broad phase visualization if your broad phase supports it
            // This would be implementation-specific
        }
    }

    // Public methods for external control
    public void SetBroadPhaseMethod(BroadPhaseMethod method)
    {
        broadPhaseMethod = method;
        InitializeBroadPhase();
    }

    public void EnableCollisionCaching(bool enable)
    {
        enableCollisionCaching = enable;
        if (enable && collisionCache == null)
        {
            collisionCache = new Dictionary<(int, int), float>();
            lastCollisionTime = new Dictionary<(int, int), float>();
        }
    }
 
}

[System.Serializable]
public struct CollisionStatistics
{
    public int broadPhaseChecks;
    public int narrowPhaseChecks;
    public int actualCollisions;
    public int cachedCollisions;
}