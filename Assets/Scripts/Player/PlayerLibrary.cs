using System;
using System.Collections.Generic;
using UnityEngine;
using Dracul.Item;
using Dracul.UI;

namespace Dracul.Player
{
    /// <summary>
    /// プレイヤーが解放（読了）した情報アイテム（Information）のライブラリを管理するコンポーネント。
    /// </summary>
    public class PlayerLibrary : MonoBehaviour
    {
        public static PlayerLibrary Instance { get; private set; }

        [Header("Library Data")]
        [Tooltip("現在解放済みの情報アイテム一覧")]
        [SerializeField]
        private List<ItemData> _unlockedItems = new List<ItemData>();

        /// <summary>解放済みアイテム一覧（読み取り専用）</summary>
        public IReadOnlyList<ItemData> UnlockedItems => _unlockedItems;

        /// <summary>ライブラリ内容が更新されたときのイベント</summary>
        public event Action OnLibraryUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 情報アイテムをライブラリに解放・登録する。
        /// </summary>
        /// <param name="data">解放するアイテムデータ</param>
        /// <returns>新規に解放された場合は true、既に解放済みの場合は false</returns>
        public bool UnlockInformation(ItemData data)
        {
            if (data == null) return false;

            if (_unlockedItems.Contains(data))
            {
                MessageLogUI.AddLog($"「{data.itemName}」はすでにライブラリに登録されています。（Vキーで閲覧可能）");
                return false;
            }

            _unlockedItems.Add(data);
            MessageLogUI.AddLog($"<color=#70c5ff>「{data.itemName}」を読み、ライブラリに登録しました。（Vキーで確認可能）</color>");

            OnLibraryUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// 指定したアイテムが既に解放されているか調べる。
        /// </summary>
        public bool IsUnlocked(ItemData data)
        {
            return data != null && _unlockedItems.Contains(data);
        }
    }
}