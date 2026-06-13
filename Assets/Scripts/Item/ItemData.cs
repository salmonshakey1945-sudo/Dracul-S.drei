using UnityEngine;

namespace Dracul.Item
{
    /// <summary>
    /// アイテムの種別。
    /// </summary>
    public enum ItemType
    {
        Consumable, // 消耗品（将来: 使用するとステータスが変化）
        KeyItem,    // キーアイテム（将来: ストーリーで使用）
        Material    // 素材（将来: 合成などに使用）
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

        [Tooltip("アイテムのアイコン画像（将来のUI用）")]
        public Sprite icon;

        [Tooltip("アイテムの種別")]
        public ItemType itemType = ItemType.Material;
    }
}
