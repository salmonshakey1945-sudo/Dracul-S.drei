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

            GUI.backgroundColor = new Color(0.85f, 0.65f, 0.4f);
            if (GUILayout.Button("Generate Rock Prefabs (岩プレハブ自動生成)", GUILayout.Height(28)))
            {
                RockPrefabGenerator.GenerateAllRockPrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "【使い分けと設定ガイド】\n" +
                "・「木」を生やす場合: 'Align To Ground Normal' を OFF（常に真上向き）。\n" +
                "・「草」を生やす場合: 'Tree Proximity' または 'Perlin Noise'。'Align To Ground Normal' を ON、'Add Shade System' を OFF、'Remove Colliders' を ON。\n" +
                "・「石・岩」を生やす場合: 'Tree Proximity'（木の根元集中）。'Align To Ground Normal' を ON、'Ground Sink Ratio' を 0.3〜0.4（地面に埋める）、'Spawn Child Satellites' を ON（大小の親子ペア）、'Remove Colliders' を OFF（遮蔽物化）。",
                MessageType.Info
            );
        }
    }
}
