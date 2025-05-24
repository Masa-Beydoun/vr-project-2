using UnityEngine;

public class Spring
{
    public MassPoint pointA;
    public MassPoint pointB;
    public float restLength;
    public float stiffness;
    public float damping;
    public LineRenderer lineRenderer;
    private float currentForceMagnitude = 0f;
    private static float observedMaxForce = 1f;



    public Spring(MassPoint a, MassPoint b, float stiffness, float damping, Transform lineParent, Material lineMaterial)
    {
        pointA = a;
        pointB = b;
        restLength = Vector3.Distance(a.position, b.position);
        this.stiffness = stiffness;
        this.damping = damping;

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
        currentForceMagnitude = force.magnitude;

        // Update global max if needed
        if (currentForceMagnitude > observedMaxForce)
            observedMaxForce = currentForceMagnitude;
    }

    public void UpdateLine()
    {
        // Use observedMaxForce instead of fixed maxForce
        float t = Mathf.Clamp01(currentForceMagnitude / observedMaxForce);
        float hue = Mathf.Lerp(0f, 0.8f, t);
        Color springColor = Color.HSVToRGB(hue, 1f, 1f);

        lineRenderer.material.color = springColor;

        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, pointB.position);


    }





}
