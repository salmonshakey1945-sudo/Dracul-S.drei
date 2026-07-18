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

    private Dracul.Player.WeaponManager _weaponManager;

    void Start()
    {
        _weaponManager = GetComponent<Dracul.Player.WeaponManager>();
        if (attackCamera == null)
        {
            attackCamera = Camera.main;
        }
    }

    private bool _wantsToShoot = false;

    void Update()
    {
        if (Mouse.current == null) return;

        // 武器が装備されていない場合は射撃不可
        if (_weaponManager != null && !_weaponManager.IsWeaponEquipped) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _wantsToShoot = true;
        }
    }

    void LateUpdate()
    {
        if (_wantsToShoot)
        {
            _wantsToShoot = false;
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
            GameObject bullet = Instantiate(projectilePrefab, spawnPos, rotation);

            // 弾がプレイヤー自身のコライダーに干渉しないようにする
            Collider bulletCollider = bullet.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                // CharacterController も Collider の一種
                Collider[] playerColliders = GetComponentsInChildren<Collider>();
                foreach (Collider playerCol in playerColliders)
                {
                    Physics.IgnoreCollision(bulletCollider, playerCol);
                }
            }
        }
    }
}
