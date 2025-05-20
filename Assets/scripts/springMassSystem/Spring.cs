using UnityEngine;

public class Spring
{
    public MassPoint pointA;
    public MassPoint pointB;
    public float restLength;
    public float stiffness;
    public float damping;
    public LineRenderer lineRenderer; // visual representation

    public Spring(MassPoint a, MassPoint b, float stiffness, float damping, Transform lineParent, Material lineMaterial)
    {
        pointA = a;
        pointB = b;
        restLength = Vector3.Distance(a.position, b.position);
        this.stiffness = stiffness;
        this.damping = damping;

        // Create the visual line
        GameObject lineObj = new GameObject("SpringLine");
        lineObj.transform.parent = lineParent;
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    public void ApplyForce(float deltaTime)
    {
        Vector3 delta = pointB.position - pointA.position;
        float currentLength = delta.magnitude;
        if (currentLength == 0) return;

        Vector3 direction = delta / currentLength;
        float displacement = currentLength - restLength;

        Vector3 force = stiffness * displacement * direction;

        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        Vector3 dampingForce = damping * Vector3.Dot(relativeVelocity, direction) * direction;

        Vector3 totalForce = force + dampingForce;

        pointA.ApplyForce(totalForce, deltaTime);
        pointB.ApplyForce(-totalForce, deltaTime);
    }

    public void UpdateLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, pointA.position);
            lineRenderer.SetPosition(1, pointB.position);
        }
    }



}
