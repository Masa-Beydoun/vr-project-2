using UnityEngine;

[CreateAssetMenu(fileName = "NewPhysicalMaterial", menuName = "Physics/Physical Material")]
public class PhysicalMaterial : ScriptableObject
{
    public string materialName;
    public float Density = 1f;
    public float dragCoefficient = 1f;
    public float bounciness = 0.3f;
    public float stiffness = 1000f;
    public float fractureThreshold = 10000f;
    
    public float youngsModulus = 2e5f;        
    public float poissonRatio = 0.3f;          
}
