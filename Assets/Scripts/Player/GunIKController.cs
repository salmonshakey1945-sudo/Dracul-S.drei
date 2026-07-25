using UnityEngine;

namespace Dracul.Player
{
    /// <summary>
    /// 左手IKと上半身エイムを制御する。
    /// WeaponManager の装備状態に連動してON/OFFする。
    /// LeftHandAimTarget を Center_Rotation を回転軸として
    /// Z軸（forward）がカメラの向きに合うよう回転させる。
    /// 下半身の前方を基準に左右±maxYawAngle度の回転制限を行う。
    /// </summary>
    public class GunIKController : MonoBehaviour
    {
        [Header("IK Settings")]
        [Tooltip("左手IKターゲット（Center_Rotationの子オブジェクト）")]
        public Transform leftHandIKTarget;
        [Range(0f, 1f)] public float ikWeight = 1f;

        [Header("Aim Pivot")]
        [Tooltip("回転の中心点（Center_Rotation オブジェクト）")]
        public Transform centerRotation;

        [Header("Aim Settings")]
        [Tooltip("メインカメラ（自動取得も可）")]
        public Transform cameraTransform;
        [Tooltip("回転の追従速度")]
        public float aimSmoothSpeed = 15f;

        [Header("Rotation Limits")]
        [Tooltip("下半身の前方を基準にした左右の回転制限（度）")]
        [Range(0f, 180f)] public float maxYawAngle = 60f;

        private Animator _animator;
        private WeaponManager _weaponManager;
        private bool _isEquipped = false;

        // Center_Rotation から LeftHandAimTarget へのローカル空間でのオフセット（初期配置を記録）
        private Vector3 _localOffset;

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

            // 初期配置時の Center_Rotation → LeftHandAimTarget のオフセットを記録
            if (centerRotation != null && leftHandIKTarget != null)
            {
                _localOffset = centerRotation.InverseTransformPoint(leftHandIKTarget.position);
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

        private Vector3 _smoothedAimDirection;

        // 左手IKの制御とエイム計算
        void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || leftHandIKTarget == null) return;

            float targetWeight = _isEquipped ? ikWeight : 0f;

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, targetWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, targetWeight);

            if (_isEquipped && centerRotation != null && cameraTransform != null)
            {
                // --- Yaw（水平回転）の計算とクランプ ---
                Vector3 cameraForwardFlat = cameraTransform.forward;
                cameraForwardFlat.y = 0f;
                if (cameraForwardFlat.sqrMagnitude < 0.001f)
                {
                    cameraForwardFlat = transform.forward;
                }
                cameraForwardFlat.Normalize();

                Vector3 bodyForward = transform.forward;
                bodyForward.y = 0f;
                bodyForward.Normalize();

                float signedYaw = Vector3.SignedAngle(bodyForward, cameraForwardFlat, Vector3.up);
                float clampedYaw = Mathf.Clamp(signedYaw, -maxYawAngle, maxYawAngle);
                Vector3 clampedForwardFlat = Quaternion.Euler(0f, clampedYaw, 0f) * bodyForward;

                // --- Pitch（上下回転）の計算 ---
                float pitch = cameraTransform.eulerAngles.x;
                // UnityのeulerAnglesは0〜360の値を取るため、180度より大きい場合はマイナス（上向き）に変換
                if (pitch > 180f) pitch -= 360f;

                // キャラクターの向いている方向を基準にしたローカルの右軸（Right）を計算
                Vector3 rightAxis = Vector3.Cross(Vector3.up, clampedForwardFlat).normalized;
                
                // 右軸を中心に上下（Pitch）回転させる
                Vector3 targetAimDirection = Quaternion.AngleAxis(pitch, rightAxis) * clampedForwardFlat;

                // --- 補間処理 ---
                // World PositionをLerpするとアニメーション中のボーンとの遅延により激しく震えるため、
                // 目標「方向」だけをSlerpで滑らかにする
                if (_smoothedAimDirection == Vector3.zero) _smoothedAimDirection = targetAimDirection;
                _smoothedAimDirection = Vector3.Slerp(_smoothedAimDirection, targetAimDirection, Time.deltaTime * aimSmoothSpeed);

                // 滑らかになった方向から最終的な回転と位置を算出
                Quaternion finalRotation = Quaternion.LookRotation(_smoothedAimDirection, Vector3.up);
                
                // Center_Rotationの位置は毎フレーム最新のアニメーション結果を使うため震えない
                Vector3 rotatedOffset = finalRotation * _localOffset;
                Vector3 finalPosition = centerRotation.position + rotatedOffset;

                // アニメーターのIKに直接適用
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, finalPosition);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, finalRotation);

                // デバッグや他のスクリプトから参照できるように実際のTransformも更新
                leftHandIKTarget.position = finalPosition;
                leftHandIKTarget.rotation = finalRotation;
            }
        }
    }
}
