using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float maxAimDistance = 1000f; // Max distance for raycast
    public LayerMask aimLayerMask = ~0;   // Default to everything, can be adjusted in Inspector

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null) return;
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        
        // Get mouse position
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

        Vector3 targetPoint;
        RaycastHit hit;

        // Perform Raycast into the world
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            // If we hit nothing (sky), aim at a point far away along the ray
            targetPoint = ray.GetPoint(maxAimDistance);
        }

        Vector3 direction = (targetPoint - spawnPos).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            Instantiate(projectilePrefab, spawnPos, rotation);
        }
    }
}
