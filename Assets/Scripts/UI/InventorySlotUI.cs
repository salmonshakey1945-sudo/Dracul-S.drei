using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Dracul.Player;

namespace Dracul.UI
{
    /// <summary>
    /// インベントリの1マス（スロット）のUI表示を管理する。
    /// アイコン画像と所持数テキストを表示し、クリックイベントを受け取る。
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("アイテムのアイコンを表示するImage")]
        public Image iconImage;

        [Tooltip("所持数を表示するテキスト（例: 15/30）")]
        public TextMeshProUGUI countText;

        [Tooltip("アイテム名を表示するテキスト（省略可）")]
        public TextMeshProUGUI nameText;

        [Tooltip("空きスロットの背景色")]
        public Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

        [Tooltip("アイテムが入っているスロットの背景色")]
        public Color filledColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);

        /// <summary>このスロットのインデックス番号</summary>
        public int SlotIndex { get; set; }

        /// <summary>スロットがクリックされた時のコールバック</summary>
        public Action<int> OnSlotClicked;

        private Image _backgroundImage;
        private Button _button;

        void Awake()
        {
            _backgroundImage = GetComponent<Image>();
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(() => OnSlotClicked?.Invoke(SlotIndex));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Buttonコンポーネントがない場合でもクリックを検知
            if (_button == null)
            {
                OnSlotClicked?.Invoke(SlotIndex);
            }
        }

        /// <summary>
        /// スロットの表示を更新する。
        /// </summary>
        /// <param name="slot">表示するインベントリスロットのデータ</param>
        public void UpdateSlot(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                // 空のスロット
                SetEmpty();
                return;
            }

            // アイテムが入っているスロット
            if (iconImage != null)
            {
                if (slot.itemData.icon != null)
                {
                    iconImage.sprite = slot.itemData.icon;
                    iconImage.color = Color.white;
                    iconImage.enabled = true;
                }
                else
                {
                    // アイコンが未設定の場合、名前の頭文字を表示する代わりにアイコンを非表示にする
                    iconImage.enabled = false;
                }
            }

            if (countText != null)
            {
                if (slot.itemData.maxStackCount > 1)
                {
                    // スタック可能なアイテムは「個数 / 上限」を表示
                    countText.text = $"{slot.count}/{slot.itemData.maxStackCount}";
                    countText.enabled = true;
                }
                else
                {
                    // スタック上限1（情報アイテムなど）は個数を表示しない
                    countText.enabled = false;
                }
            }

            if (nameText != null)
            {
                nameText.text = slot.itemData.itemName;
                nameText.enabled = true;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = filledColor;
            }
        }

        /// <summary>
        /// スロットを空表示にする。
        /// </summary>
        private void SetEmpty()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.clear;
                iconImage.enabled = false;
            }

            if (countText != null)
            {
                countText.text = "";
                countText.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = "";
                nameText.enabled = false;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = emptyColor;
            }
        }
    }
}
