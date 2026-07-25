using UnityEngine;
using UnityEngine.InputSystem;
using Dracul.Player;

namespace Dracul.UI
{
    /// <summary>
    /// インベントリUIの開閉を管理するマネージャー。
    /// Bキーでインベントリパネルを表示/非表示する。
    /// インベントリ表示中はマウスカーソルを表示し、カメラ操作を無効化する。
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("インベントリUIのルートパネル（Canvas直下のPanel）")]
        public GameObject inventoryPanel;

        [Tooltip("プレイヤーの PlayerInventory コンポーネント")]
        public PlayerInventory playerInventory;

        [Tooltip("StarterAssetsInputs コンポーネント（カーソル制御用）")]
        public StarterAssets.StarterAssetsInputs starterAssetsInputs;

        [Header("Slot UI")]
        [Tooltip("各スロットのUI（順番にスロット0〜7に対応）")]
        public InventorySlotUI[] slotUIs;

        private bool _isOpen = false;

        void Start()
        {
            // 起動時はインベントリを閉じた状態にする
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
            _isOpen = false;

            // PlayerInventory のイベントに登録する
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += RefreshUI;
            }
        }

        void OnDestroy()
        {
            // イベントの解除
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshUI;
            }
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            // Bキーが押されたらトグル
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (_isOpen)
                {
                    CloseInventory();
                }
                else
                {
                    OpenInventory();
                }
            }

            // ESCキーでも閉じられるようにする
            if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
        }

        /// <summary>
        /// インベントリを開く。
        /// </summary>
        public void OpenInventory()
        {
            _isOpen = true;

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
            }

            // マウスカーソルを表示し、カメラ操作を無効化
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorInputForLook = false;
                starterAssetsInputs.cursorLocked = false;
            }

            RefreshUI();
        }

        /// <summary>
        /// インベントリを閉じる。
        /// </summary>
        public void CloseInventory()
        {
            _isOpen = false;

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            // マウスカーソルを非表示に戻し、カメラ操作を再有効化
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorInputForLook = true;
                starterAssetsInputs.cursorLocked = true;
            }
        }

        /// <summary>
        /// インベントリが開いているかどうか。
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// 全スロットのUI表示を最新のインベントリ内容に更新する。
        /// </summary>
        private void RefreshUI()
        {
            if (playerInventory == null || slotUIs == null) return;

            var slots = playerInventory.Slots;
            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] == null) continue;

                if (i < slots.Count)
                {
                    slotUIs[i].UpdateSlot(slots[i]);
                }
                else
                {
                    slotUIs[i].UpdateSlot(null);
                }
            }
        }
    }
}
