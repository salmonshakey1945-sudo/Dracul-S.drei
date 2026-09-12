using UnityEngine;

namespace Dracul.Environment
{
    /// <summary>
    /// 日光を遮るオブジェクトにアタッチするコンポーネント。
    /// 遮蔽度（日光ダメージやブラッドゲージ減少のペナルティ倍率）を設定できます。
    /// </summary>
    public class ShadeObject : MonoBehaviour
    {
        [Header("Shade Settings")]
        [Tooltip("このオブジェクトの影に入った時のペナルティ倍率（0 = 完全遮蔽/ダメージなし, 0.3 = 70%カット, 1 = 遮蔽効果なし）")]
        [Range(0f, 1f)]
        public float penaltyMultiplier = 0.3f;

        [Tooltip("遮蔽物の説明・種類（例: Tree Leaves, Cloth, Roof など）")]
        public string shadeDescription = "Tree Leaves";
    }
}
