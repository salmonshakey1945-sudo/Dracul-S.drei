using UnityEngine;
using System.Collections.Generic;
using Dracul.Item;

namespace Dracul.Player
{
    /// <summary>
    /// プレイヤーのアイテム所持リストを管理する。
    /// アイテムの実際の効果は将来のアップデートで実装する。
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory")]
        [Tooltip("現在所持しているアイテムの一覧（読み取り用、インスペクターで確認可）")]
        [SerializeField]
        private List<ItemData> _items = new List<ItemData>();

        /// <summary>アイテムリストへの読み取り専用アクセス</summary>
        public IReadOnlyList<ItemData> Items => _items;

        /// <summary>
        /// アイテムをインベントリに追加する。
        /// </summary>
        /// <param name="data">追加するアイテムデータ</param>
        public void AddItem(ItemData data)
        {
            if (data == null) return;

            _items.Add(data);
            Debug.Log($"[Inventory] 「{data.itemName}」を取得！ (所持数: {_items.Count}件)");

            // TODO: UIにアイテム取得通知を表示する
        }

        /// <summary>
        /// アイテムをインベントリから削除する（将来の使用・消費処理用）。
        /// </summary>
        /// <param name="data">削除するアイテムデータ</param>
        /// <returns>削除に成功した場合 true</returns>
        public bool RemoveItem(ItemData data)
        {
            return _items.Remove(data);
        }
    }
}
