using UnityEngine;

namespace Dracul.Player
{
    /// <summary>
    /// 左手IKと上半身エイム（Spineボーン回転）を制御する。
    /// WeaponManager の装備状態に連動してON/OFFする。
    /// </summary>
    public class GunIKController : MonoBehaviour
    {
        [Header("IK Settings")]
        [Tooltip("LeftHandGrip 空オブジェクト（Pistol 92 の子）")]
        public Transform leftHandIKTarget;
        [Range(0f, 1f)] public float ikWeight = 1f;

        [Header("Aim Settings")]
        [Tooltip("上半身エイム対象のボーン (mixamorig:Spine)")]
        public Transform spineBone;
        [Tooltip("メインカメラ（自動取得も可）")]
        public Transform cameraTransform;
        [Tooltip("上半身の向き変更速度")]
        public float aimSmoothSpeed = 15f;

        private Animator _animator;
        private WeaponManager _weaponManager;
        private bool _isEquipped = false;

        void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _weaponManager = GetComponent<WeaponManager>();

            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped += OnWeaponEquippedChanged;
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        void OnDestroy()
        {
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped -= OnWeaponEquippedChanged;
            }
        }

        private void OnWeaponEquippedChanged(bool equipped)
        {
            _isEquipped = equipped;
        }

        // 左手IKの制御
        void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || leftHandIKTarget == null) return;

            float targetWeight = _isEquipped ? ikWeight : 0f;

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, targetWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, targetWeight);

            if (_isEquipped)
            {
                // IKターゲット（Spineの子など、体の前方に配置したオブジェクト）に左手を固定する
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
            }
        }

        // 上半身エイムの制御
        void LateUpdate()
        {
            if (!_isEquipped || spineBone == null || cameraTransform == null) return;

            // キャラクター全体の回転（足の向き）は変更せず、上半身（Spine）だけをカメラに向ける
            Vector3 aimDirection = cameraTransform.forward;
            
            // SpineのZ軸（正面）をカメラの向いている方向（aimDirection）に合わせる
            // Y軸（上方向）はワールドのUpを基準にする
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            
            // アニメーションの回転を上書きしてSpineをねじる
            spineBone.rotation = Quaternion.Slerp(spineBone.rotation, targetRotation, Time.deltaTime * aimSmoothSpeed);
        }
    }
}
