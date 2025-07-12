using System.Collections.Generic;
using UnityEngine;

public class EnhancedCollisionManager : MonoBehaviour
{
    [Header("Broad Phase Configuration")]
    public BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.SweepAndPrune;
    [SerializeField] private float uniformGridCellSize = 1.0f;
    [SerializeField] private Vector3 octreeBounds = Vector3.one * 10000;

    [Header("Collision Handling")]
    [SerializeField] private EnhancedCollisionHandler collisionHandler;

    [Header("Performance Settings")]
    [SerializeField] private int maxCollisionsPerFrame = 50; // Reduced from 100
    [SerializeField] private bool enableCollisionCaching = true;
    [SerializeField] private float cacheValidityTime = 0.1f; // Increased from 0.016f
    [SerializeField] private float minCollisionInterval = 0.05f; // NEW: Minimum time between same-pair collisions

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

        // Broad phase collision detection
        var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        broadPhaseChecks = candidatePairs.Count;

        if (showBroadPhaseDebug)
        {
            Debug.Log($"Broad Phase: {broadPhaseChecks} candidate pairs found");
        }

        // Process collision pairs
        ProcessCollisionPairs(candidatePairs);

        // Clean up old cache entries
        if (enableCollisionCaching)
        {
            CleanupCollisionCache();
        }

        // Log statistics
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
        if (collisionHandler == null)
        {
            collisionHandler = GetComponent<EnhancedCollisionHandler>();
            if (collisionHandler == null)
            {
                Debug.LogError("EnhancedCollisionHandler not found! Please attach it to the same GameObject.");
            }
        }
    }

    private void ProcessCollisionPairs(List<(PhysicalObject, PhysicalObject)> candidatePairs)
    {
        collisionCount = 0;

        foreach (var (objA, objB) in candidatePairs)
        {
            if (collisionCount >= maxCollisionsPerFrame)
            {
                if (showNarrowPhaseDebug)
                {
                    Debug.LogWarning($"Maximum collisions per frame ({maxCollisionsPerFrame}) reached. Some collisions may be skipped.");
                }
                break;
            }

            if (objA == null || objB == null) continue;

            // Check if we should skip this collision due to timing
            if (ShouldSkipCollision(objA, objB))
            {
                continue;
            }

            narrowPhaseChecks++;

            // Determine collision type and handle accordingly
            HandleCollisionPair(objA, objB);
        }
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

    private void HandleCollisionPair(PhysicalObject objA, PhysicalObject objB)
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
            HandleSpringMassToSpringMass(springSystemA, springSystemB);
        }
        else if (femControllerA != null && femControllerB != null)
        {
            HandleFEMToFEM(femControllerA, femControllerB);
        }
        else if (springSystemA != null && femControllerB != null)
        {
            HandleSpringMassToFEM(springSystemA, femControllerB);
        }
        else if (femControllerA != null && springSystemB != null)
        {
            HandleFEMToSpringMass(femControllerA, springSystemB);
        }
        else
        {
            // Handle static object collisions or other cases
            HandleStaticCollision(objA, objB);
        }
    }

    private void HandleSpringMassToSpringMass(SpringMassSystem systemA, SpringMassSystem systemB)
    {
        if (systemA.allPoints.Count == 0 || systemB.allPoints.Count == 0)
            return;

        List<CollisionResultEnhanced> collisions = CollisionDetector.CheckCollision(systemA, systemB);

        // Limit collisions per pair to prevent spam
        int processedCollisions = 0;
        const int maxCollisionsPerPair = 5;

        foreach (var collision in collisions)
        {
            if (collision.collided && processedCollisions < maxCollisionsPerPair)
            {
                collisionHandler.HandleCollision(collision);
                actualCollisions++;
                collisionCount++;
                processedCollisions++;

                if (showNarrowPhaseDebug)
                {
                    Debug.Log($"SpringMass Collision: {collision.pointA.sourceName}[{collision.pointA.id}] <-> {collision.pointB.sourceName}[{collision.pointB.id}]");
                    Debug.DrawLine(collision.pointA.position, collision.pointB.position, Color.red, 0.1f);
                }
            }
        }
    }

    private void HandleFEMToFEM(FEMController controllerA, FEMController controllerB)
    {
        if (controllerA.GetAllNodes().Length == 0 || controllerB.GetAllNodes().Length == 0)
            return;

        List<CollisionResultEnhanced_FEM> collisions = CollisionDetector_FEM.CheckCollision(controllerA, controllerB);

        foreach (var collision in collisions)
        {
            if (collision.collided)
            {
                collisionHandler.HandleFEMCollision(collision);
                actualCollisions++;
                collisionCount++;

                if (showNarrowPhaseDebug)
                {
                    Debug.Log($"FEM Collision: {controllerA.gameObject.name}[{collision.pointA.ID}] <-> {controllerB.gameObject.name}[{collision.pointB.ID}]");
                    Debug.DrawLine(collision.pointA.Position, collision.pointB.Position, Color.blue, 0.1f);
                }
            }
        }
    }

    private void HandleSpringMassToFEM(SpringMassSystem springSystem, FEMController femController)
    {
        if (springSystem.allPoints.Count == 0 || femController.GetAllNodes().Length == 0)
            return;

        // Check collisions between spring-mass points and FEM nodes
        var massPoints = springSystem.allPoints;
        var femNodes = femController.GetAllNodes();

        foreach (var massPoint in massPoints)
        {
            foreach (var femNode in femNodes)
            {
                if (CheckPointToPointCollision(massPoint.position, femNode.Position, out Vector3 normal, out float penetration, out Vector3 contactPoint))
                {
                    collisionHandler.HandleCollision(massPoint, femNode, normal, penetration, contactPoint);
                    actualCollisions++;
                    collisionCount++;

                    if (showNarrowPhaseDebug)
                    {
                        Debug.Log($"Mixed Collision: SpringMass[{massPoint.id}] <-> FEM[{femNode.ID}]");
                        Debug.DrawLine(massPoint.position, femNode.Position, Color.green, 0.1f);
                    }
                }
            }
        }
    }

    private void HandleFEMToSpringMass(FEMController femController, SpringMassSystem springSystem)
    {
        // Just call the reverse
        HandleSpringMassToFEM(springSystem, femController);
    }

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

    public CollisionStatistics GetCollisionStatistics()
    {
        return new CollisionStatistics
        {
            broadPhaseChecks = broadPhaseChecks,
            narrowPhaseChecks = narrowPhaseChecks,
            actualCollisions = actualCollisions,
            cachedCollisions = collisionCache?.Count ?? 0
        };
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