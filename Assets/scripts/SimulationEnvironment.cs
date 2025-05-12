// SimulationEnvironment.cs
using UnityEngine;

public class SimulationEnvironment : MonoBehaviour
{
    public static SimulationEnvironment Instance { get; private set; }

    [Header("Environment Settings")]
    public float gravityStrength = 9.81f;
    public float airTemperature = 20f; // Celsius
    public float airPressure = 101325f; // Pa

    [Header("Physical Constants")]
    public float gasConstant = 287.05f; // J/(kg·K)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public float GetAirDensity()
    {
        float tempKelvin = airTemperature + 273.15f;
        return airPressure / (gasConstant * tempKelvin);
    }

    public Vector3 GetGravity()
    {
        return new Vector3(0, -gravityStrength, 0);
    }
}
