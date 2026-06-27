using UnityEngine;
using UnityEngine.InputSystem;

namespace Dracul.Player
{
    /// <summary>
    /// 武器の装備・収納を管理するマネージャー。
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapon Slots")]
        [Tooltip("左手装備スロット（銃など）")]
        public GameObject leftHandWeapon;

        [Header("Input")]
        [Tooltip("武器を出し入れするキー")]
        public Key equipKey = Key.E;

        // 現在の装備状態
        public bool IsWeaponEquipped { get; private set; } = false;

        // 装備状態変化イベント
        public event System.Action<bool> OnWeaponEquipped;

        void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[equipKey].wasPressedThisFrame)
            {
                ToggleWeapon();
            }
        }

        public void ToggleWeapon()
        {
            IsWeaponEquipped = !IsWeaponEquipped;
            ApplyWeaponState();
        }

        private void ApplyWeaponState()
        {
            if (leftHandWeapon != null)
            {
                leftHandWeapon.SetActive(IsWeaponEquipped);
            }

            OnWeaponEquipped?.Invoke(IsWeaponEquipped);
        }
    }
}
