using UnityEngine;
using Dracul.Item;
using Dracul.UI;

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

        [Tooltip("このオブジェクト固有の取得時メッセージ（設定されていればItemDataの設定より優先）")]
        [TextArea(1, 3)]
        public string overridePickupMessage = "";

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
                string fullMsg = $"インベントリが満杯のため「{itemData.itemName}」を拾えません。";
                Debug.Log($"[ItemPickup] {fullMsg}");
                MessageLogUI.AddLog($"<color=red>{fullMsg}</color>");
                return;
            }

            int added = inventory.AddItem(itemData, amountToAdd);
            if (added > 0)
            {
                isPickedUp = true;

                // 取得メッセージの決定（overridePickupMessage > itemData.pickupMessage > デフォルトメッセージ）
                string message = GetPickupMessage(added);
                MessageLogUI.AddLog(message);

                Destroy(gameObject);
            }
        }

        private string GetPickupMessage(int addedCount)
        {
            string template = !string.IsNullOrEmpty(overridePickupMessage)
                ? overridePickupMessage
                : itemData.pickupMessage;

            if (!string.IsNullOrEmpty(template))
            {
                return template
                    .Replace("{name}", itemData.itemName)
                    .Replace("{count}", addedCount.ToString());
            }

            return addedCount > 1 
                ? $"「{itemData.itemName}」を {addedCount} 個手に入れた。" 
                : $"「{itemData.itemName}」を手に入れた。";
        }
    }
}
