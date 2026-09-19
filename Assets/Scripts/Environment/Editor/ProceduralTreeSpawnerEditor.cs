using UnityEditor;
using UnityEngine;

namespace Dracul.Environment.Editor
{
    [CustomEditor(typeof(ProceduralTreeSpawner))]
    public class ProceduralTreeSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ProceduralTreeSpawner spawner = target as ProceduralTreeSpawner;
            if (spawner == null) return;

            // デフォルトのインスペクター描画
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Procedural Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Generate (生成・配置)", GUILayout.Height(35)))
            {
                spawner.GenerateTrees();
                EditorUtility.SetDirty(spawner);
            }

            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear (全削除)", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Objects", "生成されたオブジェクトをすべて削除しますか？", "削除", "キャンセル"))
                {
                    spawner.ClearTrees();
                    EditorUtility.SetDirty(spawner);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Environment Utilities", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            if (GUILayout.Button("Fix Tree Materials for URP (影の有効化)", GUILayout.Height(28)))
            {
                TreeMaterialURPSetup.FixTreeMaterials();
            }

            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("Generate Grass Prefabs (草プレハブ自動生成)", GUILayout.Height(28)))
            {
                GrassPrefabGenerator.GenerateAllGrassPrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "【使い分けと設定ガイド】\n" +
                "・「密度の濃淡」: 'Distribution Mode' を 'Perlin Noise'（自然なまだら模様）または 'Clusters'（島状の群生）に設定すると、草が固まって生える場所と地面が露出する場所のメリハリが作れます。\n" +
                "・「木」を生やす場合: 'Align To Ground Normal' を OFF（常に真上向き）。\n" +
                "・「草」を生やす場合: 'Align To Ground Normal' を ON（地面の傾斜に垂直）、'Add Shade System' を OFF、'Remove Colliders' を ON に設定。",
                MessageType.Info
            );
        }
    }
}
