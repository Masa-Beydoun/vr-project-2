using System.Collections.Generic;
using UnityEngine;

public class SpringMassSphere : MonoBehaviour
{
    public int resolution = 5;
    public float springStiffness = 500f;
    public float springDamping = 2f;
    public float connectionRadius = 0.6f;
    public GameObject pointPrefab;
    public Material springLineMaterial;

    private List<MassPoint> massPoints = new List<MassPoint>();
    private List<Spring> springs = new List<Spring>();

    void start()
    {
        float radius = transform.localScale.x / 2f;
        float step = (2f * radius) / (resolution - 1);
        connectionRadius = step * Mathf.Sqrt(3); 
        GenerateMassPoints();
        ConnectSprings();
    }



    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();

        foreach (var p in massPoints)
            p.ApplyForce(gravity * p.Mass, dt);

        foreach (var s in springs)
            s.ApplyForce(dt);

        foreach (var p in massPoints)
        {
            p.Integrate(dt);
            p.physicalObject.transform.position = p.position;
        }

        foreach (var s in springs)
            s.UpdateLine();
    }

    void GenerateMassPoints()
    {
        float radius = transform.localScale.x / 2f;
        float step = (2f * radius) / (resolution - 1);
        Vector3 center = transform.position;

        for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 offset = new Vector3(x * step - radius, y * step - radius, z * step - radius);
                    if (offset.magnitude <= radius)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                        var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                        go.AddComponent<CollisionBody>();
                        MassPoint mp = new MassPoint(worldPos, po);
                        massPoints.Add(mp);
                    }
                }
    }

    void ConnectSprings()
    {
        for (int i = 0; i < massPoints.Count; i++)
            for (int j = i + 1; j < massPoints.Count; j++)
            {
                float dist = Vector3.Distance(massPoints[i].position, massPoints[j].position);
                if (dist <= connectionRadius)
                {
                    springs.Add(new Spring(massPoints[i], massPoints[j], springStiffness, springDamping, transform, springLineMaterial));
                }
            }
    }
}
