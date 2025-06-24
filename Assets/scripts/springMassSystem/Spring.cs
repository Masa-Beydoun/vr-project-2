using UnityEngine;

public class Spring
{
    public float maxStretchRatio = 2f; // Break if stretched 2× rest length
    public bool broken = false;

    public MassPoint pointA;
    public MassPoint pointB;
    public float restLength;
    public LineRenderer lineRenderer;
    private float currentForceMagnitude = 0f;
    private static float observedMaxForce = 1f;

    [HideInInspector] public float springStiffness;
    [HideInInspector] public float springDamping;

    public Spring(MassPoint a, MassPoint b, float stiffness, float damping, Transform lineParent, Material lineMaterial)
    {
        pointA = a;
        pointB = b;
        restLength = Vector3.Distance(a.position, b.position);
        springStiffness = stiffness;
        springDamping = damping;

        GameObject lineObj = new GameObject("SpringLine");
        lineObj.transform.parent = lineParent;

        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(lineMaterial); // make unique instance
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    public void ApplyForce(float deltaTime)
    {
        if (broken) return;

        Vector3 delta = pointB.position - pointA.position;
        float currentLength = delta.magnitude;
        if (currentLength == 0f) return;

        // Check for overstretch
        if (currentLength > restLength * maxStretchRatio)
        {
            broken = true;
            return;
        }

        Vector3 direction = delta / currentLength;
        float displacement = currentLength - restLength;

        Vector3 springForce = springStiffness * displacement * direction;

        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        Vector3 dampingForce = springDamping * Vector3.Dot(relativeVelocity, direction) * direction;

        Vector3 totalForce = springForce + dampingForce;

        pointA.ApplyForce(totalForce, deltaTime);
        pointB.ApplyForce(-totalForce, deltaTime);

        currentForceMagnitude = springForce.magnitude;
        if (currentForceMagnitude > observedMaxForce)
            observedMaxForce = currentForceMagnitude;
    }

    public void UpdateLine()
    {
        //Debug.Log("Updating line between: " + pointA.position + " and " + pointB.position);

        if (broken)
        {
            lineRenderer.material.color = Color.black;
        }
        else
        {
            float t = Mathf.Clamp01(currentForceMagnitude / observedMaxForce);
            float hue = Mathf.Lerp(0f, 0.8f, t);
            Color springColor = Color.HSVToRGB(hue, 1f, 1f);
            lineRenderer.material.color = springColor;
        }

        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, pointB.position);

        // Just for testing
        Debug.DrawLine(pointA.position, pointB.position, Color.red, 2f);

    }

}
