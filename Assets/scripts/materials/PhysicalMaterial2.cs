using UnityEngine;

<<<<<<< HEAD
[CreateAssetMenu(fileName = "NewPhysicalMaterial2", menuName = "Physics/Physical Material2")]
=======
[CreateAssetMenu(fileName = "NewPhysicalMaterial2", menuName = "Physics/Physical Material")]
>>>>>>> 0e1295122ebe31296514cedd4ab4eea4a889611c
public class PhysicalMaterial2 : ScriptableObject
{
    public string materialName;
    public float Density = 1f;
    public float dragCoefficient = 1f;
    public float bounciness = 0.3f;
    public float stiffness = 1000f;
    public float fractureThreshold = 10000f;
}
