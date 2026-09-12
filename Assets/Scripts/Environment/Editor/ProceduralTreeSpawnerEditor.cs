using UnityEditor;
using UnityEngine;

namespace Dracul.Environment.Editor
{
    [CustomEditor(typeof(ProceduralTreeSpawner))]
    public class ProceduralTreeSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクター描画
            DrawDefaultInspector();

            ProceduralTreeSpawner spawner = (ProceduralTreeSpawner)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Procedural Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Generate Trees (木を生成)", GUILayout.Height(35)))
            {
                spawner.GenerateTrees();
                EditorUtility.SetDirty(spawner);
            }

            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear Trees (木を全削除)", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Trees", "生成された木をすべて削除しますか？", "削除", "キャンセル"))
                {
                    spawner.ClearTrees();
                    EditorUtility.SetDirty(spawner);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("URP Shadow Settings", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            if (GUILayout.Button("Fix Tree Materials for URP (影の有効化)", GUILayout.Height(30)))
            {
                TreeMaterialURPSetup.FixTreeMaterials();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "・'Fix Tree Materials for URP' を押すと、Tree9のマテリアルをURP Litシェーダーに変換し、リアルタイムの葉の影を有効化します。\n" +
                "・'Generate Trees' を押すと設定した範囲に木がランダム配置されます。\n" +
                "・葉の影に入ると日光ダメージとブラッドゲージ減少が自動で軽減されます。",
                MessageType.Info
            );
        }
    }
}
