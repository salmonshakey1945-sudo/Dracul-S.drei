using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second for each axis")]
    public Vector3 rotationSpeed = new Vector3(10, 0, 0);

    void Update()
    {
        // Rotate the object based on the rotationSpeed and time
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
