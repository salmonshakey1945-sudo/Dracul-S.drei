using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float maxAimDistance = 1000f; // Max distance for raycast
    public LayerMask aimLayerMask = ~0;   // Default to everything, can be adjusted in Inspector

    [Header("Camera Settings")]
    [Tooltip("If left empty, Camera.main will be used.")]
    public Camera attackCamera;

    void Start()
    {
        if (attackCamera == null)
        {
            attackCamera = Camera.main;
        }
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
        if (attackCamera == null) attackCamera = Camera.main;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        
        // カメラが向いている正面方向をそのまま弾の飛ぶ方向にする
        Vector3 direction = attackCamera.transform.forward;

        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            Instantiate(projectilePrefab, spawnPos, rotation);
        }
    }
}
