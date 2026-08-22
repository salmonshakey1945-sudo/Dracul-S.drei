using UnityEngine;
using Dracul.Item;

namespace Dracul.Item
{
    /// <summary>
    /// ワールドに配置・ドロップされるアイテムオブジェクト。
    /// PlayerInteract から Pickup() を呼ぶことで PlayerInventory に追加される。
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        [Tooltip("このオブジェクトが表すアイテムデータ")]
        public ItemData itemData;

        [Tooltip("取得数の上書き設定（0ならItemDataのpickupAmountを使用）")]
        public int overrideAmount = 0;

        /// <summary>
        /// すでに取得済みかどうか。重複取得防止に使用する。
        /// </summary>
        [HideInInspector]
        public bool isPickedUp = false;

        /// <summary>
        /// プレイヤーがFキーを押したときに PlayerInteract から呼ばれる。
        /// アイテムを PlayerInventory に追加し、自身を消滅させる。
        /// インベントリが満杯の場合はアイテムを残す。
        /// </summary>
        /// <param name="inventory">アイテムを受け取るプレイヤーインベントリ</param>
        public void Pickup(Dracul.Player.PlayerInventory inventory)
        {
            if (isPickedUp) return;
            if (itemData == null)
            {
                Debug.LogWarning($"[ItemPickup] {gameObject.name} に ItemData がアサインされていません。");
                return;
            }

            // インベントリに空きがあるか確認してから追加する
            int amountToAdd = overrideAmount > 0 ? overrideAmount : itemData.pickupAmount;
            if (!inventory.CanAddItem(itemData, amountToAdd))
            {
                Debug.Log($"[ItemPickup] インベントリが満杯のため「{itemData.itemName}」を拾えません。");
                return;
            }

            int added = inventory.AddItem(itemData, amountToAdd);
            if (added > 0)
            {
                isPickedUp = true;
                Destroy(gameObject);
            }
        }
    }
}
