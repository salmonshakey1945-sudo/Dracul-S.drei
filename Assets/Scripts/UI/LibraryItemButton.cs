using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dracul.Item;

namespace Dracul.UI
{
    /// <summary>
    /// ライブラリウィンドウの左側リストに並ぶ、情報アイテム選択用ボタンのUIコンポーネント。
    /// </summary>
    public class LibraryItemButton : MonoBehaviour
    {
        [Tooltip("アイテム名を表示するテキスト")]
        public TextMeshProUGUI titleText;

        [Tooltip("アイテムのアイコン画像（省略可）")]
        public Image iconImage;

        [Tooltip("選択中に表示するハイライト枠や画像（省略可）")]
        public GameObject selectedHighlight;

        [Tooltip("ボタンコンポーネント（省略時は自動取得）")]
        public Button button;

        public ItemData ItemData { get; private set; }

        private Action<ItemData> _onSelectedCallback;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// ボタンのデータをセットアップする。
        /// </summary>
        public void Setup(ItemData data, Action<ItemData> onSelected, bool isSelected = false)
        {
            ItemData = data;
            _onSelectedCallback = onSelected;

            if (titleText != null)
            {
                titleText.text = data != null ? data.itemName : "---";
            }

            if (iconImage != null)
            {
                if (data != null && data.icon != null)
                {
                    iconImage.sprite = data.icon;
                    iconImage.color = Color.white;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            SetSelected(isSelected);
        }

        /// <summary>
        /// 選択中ハイライトのON/OFF
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(isSelected);
            }
        }

        private void OnClick()
        {
            _onSelectedCallback?.Invoke(ItemData);
        }
    }
}