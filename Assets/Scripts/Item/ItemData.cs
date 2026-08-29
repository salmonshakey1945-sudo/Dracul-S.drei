using UnityEngine;

namespace Dracul.Item
{
    /// <summary>
    /// アイテムの種別。
    /// </summary>
    public enum ItemType
    {
        Consumable,   // 消耗品（将来: 使用するとステータスが変化）
        KeyItem,      // キーアイテム（将来: ストーリーで使用）
        Equipment,    // 装備品（ギアなど）
        Ammo,         // 弾薬
        Information   // 情報アイテム（特定の場所で消費）
    }

    /// <summary>
    /// アイテムのデータ定義。ScriptableObject として保存し、Prefab にアサインして使用する。
    /// Project ウィンドウで右クリック > Create > Dracul > ItemData から作成できる。
    /// </summary>
    [CreateAssetMenu(menuName = "Dracul/ItemData", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("アイテム名（UIや取得ログに表示される）")]
        public string itemName = "Unknown Item";

        [Tooltip("アイテムの説明文（将来のUI用）")]
        [TextArea(2, 4)]
        public string description = "";

        [Tooltip("取得時にメッセージウィンドウに表示するテキスト（空なら「○○を手に入れた。」）")]
        [TextArea(1, 3)]
        public string pickupMessage = "";

        [Tooltip("ライブラリ（Vキー）で表示する詳細文章・テキスト（空ならdescriptionを表示）")]
        [TextArea(4, 12)]
        public string libraryContent = "";

        [Tooltip("アイテムのアイコン画像（インベントリUIに表示される）")]
        public Sprite icon;

        [Tooltip("アイテムの種別")]
        public ItemType itemType = ItemType.Equipment;

        [Tooltip("1マスに何個まで重ねられるか（スタック上限）。情報アイテムは1推奨")]
        [Min(1)]
        public int maxStackCount = 1;

        [Tooltip("1回の取得で増える数")]
        [Min(1)]
        public int pickupAmount = 1;
    }
}
