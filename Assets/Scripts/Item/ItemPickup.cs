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

        /// <summary>
        /// すでに取得済みかどうか。重複取得防止に使用する。
        /// </summary>
        [HideInInspector]
        public bool isPickedUp = false;

        /// <summary>
        /// プレイヤーがFキーを押したときに PlayerInteract から呼ばれる。
        /// アイテムを PlayerInventory に追加し、自身を消滅させる。
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

            isPickedUp = true;
            inventory.AddItem(itemData);
            Destroy(gameObject);
        }
    }
}
