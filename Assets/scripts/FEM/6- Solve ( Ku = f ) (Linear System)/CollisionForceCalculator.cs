using UnityEngine;

public static class CollisionForceCalculator
{
    // Collision stiffness coefficient - tune this parameter for stability
    public static float collisionStiffness = 10000f;

    // Compute collision forces for collided nodes
    // collidedNodeIndices: indices of collided nodes
    // penetrationDepths: penetration depths for each collided node
    // penetrationNormals: normals pointing out of collision for each node (must come from your collision detection)
    // totalForceVector: the global force vector to add collision forces into
    public static void AddCollisionForces(int[] collidedNodeIndices, float[] penetrationDepths, Vector3[] penetrationNormals, ref float[] totalForceVector)
    {
        for (int i = 0; i < collidedNodeIndices.Length; i++)
        {
            int nodeIndex = collidedNodeIndices[i];
            float depth = penetrationDepths[i];
            Vector3 normal = penetrationNormals[i];

            Vector3 collisionForce = collisionStiffness * depth * normal;

            // Add force components to totalForceVector (3 DOFs per node)
            totalForceVector[nodeIndex * 3 + 0] += collisionForce.x;
            totalForceVector[nodeIndex * 3 + 1] += collisionForce.y;
            totalForceVector[nodeIndex * 3 + 2] += collisionForce.z;
        }
    }
}
