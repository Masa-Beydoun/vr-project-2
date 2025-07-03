using UnityEngine;

[CreateAssetMenu(fileName = "NewPhysicalMaterial2", menuName = "Physics/Physical Material2")]
public class PhysicalMaterial2 : ScriptableObject
{
    public string materialName;
    public float Density = 1f;
    public float dragCoefficient = 1f;
    public float bounciness = 0.3f;
    public float stiffness = 1000f;
    public float fractureThreshold = 10000f;
}
