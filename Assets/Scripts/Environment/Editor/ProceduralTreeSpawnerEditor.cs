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

            GUI.backgroundColor = new Color(0.95f, 0.75f, 0.3f);
            if (GUILayout.Button("Setup Medieval Debris Preset (木多め・瓦礫少なめプリセット)", GUILayout.Height(32)))
            {
                SetupMedievalDebrisPreset(spawner);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "【使い分けと設定ガイド】\n" +
                "・「木」を生やす場合: 'Align To Ground Normal' を OFF（常に真上向き）。\n" +
                "・「草」を生やす場合: 'Tree Proximity' または 'Perlin Noise'。'Align To Ground Normal' を ON、'Add Shade System' を OFF、'Remove Colliders' を ON。\n" +
                "・「石・岩」を生やす場合: 'Tree Proximity'（木の根元集中）。'Align To Ground Normal' を ON、'Ground Sink Ratio' を 0.3〜0.4（地面に埋める）、'Spawn Child Satellites' を ON（大小の親子ペア）、'Remove Colliders' を OFF（遮蔽物化）。\n" +
                "・「廃墟・瓦礫」を散らす場合: 'Building Proximity'（建物周辺）。'Prefab Selection & Ratio' で Wood Ratio を 0.8（木80%:瓦礫20%）に設定、'Random Tumble Angle' で不規則に傾けて散乱表現。",
                MessageType.Info
            );
        }

        private static void SetupMedievalDebrisPreset(ProceduralTreeSpawner spawner)
        {
            Undo.RecordObject(spawner, "Setup Medieval Debris Preset");

            // Medieval Village Building Pack から木材パーツとレンガパーツを検索
            string prefabFolder = "Assets/Medieval Village Building Pack/Prefabs";
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });

            var woodList = new System.Collections.Generic.List<GameObject>();
            var rubbleList = new System.Collections.Generic.List<GameObject>();

            foreach (string guid in allPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                if (fileName.StartsWith("Wood_Plank") || fileName.StartsWith("Wood_Support") || fileName.StartsWith("Wood_Joist"))
                {
                    woodList.Add(prefab);
                }
                else if (fileName.StartsWith("Brick_Corner") || fileName.StartsWith("Brick_Row"))
                {
                    rubbleList.Add(prefab);
                }
            }

            spawner.useRatioCategories = true;
            spawner.woodPrefabs = woodList.ToArray();
            spawner.rubblePrefabs = rubbleList.ToArray();
            spawner.woodRatio = 0.8f; // 木80% : 瓦礫20%

            spawner.distributionMode = ProceduralTreeSpawner.DistributionMode.BuildingProximity;
            spawner.preventBuildingRoofSpawn = true; // 屋根上・屋内への配置を防止
            spawner.buildingProximityFocus = 0.95f;

            // ユーザーがまだカスタムしていない初期値（100x100）の場合のみ狭めの初期値をセット。変更済みの場合はユーザーの設定を維持！
            if (spawner.spawnAreaSize == new Vector2(100f, 100f) || spawner.spawnAreaSize == new Vector2(25f, 25f))
            {
                spawner.spawnAreaSize = new Vector2(16f, 16f); // 家の足元に合わせたコンパクトサイズ
            }
            if (spawner.buildingOuterRadius >= 10f)
            {
                spawner.buildingInnerRadius = 3.8f; // 外壁のすぐ外側
                spawner.buildingOuterRadius = 7.0f; // 外壁から約2〜3mの範囲に限定
            }
            spawner.buildingFalloff = 2.0f; // 壁際にギュッと集中

            if (spawner.treeCount == 50)
            {
                spawner.treeCount = 25; // 狭い範囲に合わせて数を適正化
            }
            spawner.minDistance = 0.4f;

            spawner.baseScale = new Vector3(0.85f, 0.85f, 0.85f);
            spawner.scaleVariation = 0.3f;
            spawner.randomYRotation = true;
            spawner.randomTumbleAngle = 20f; // リアルな転がり傾き

            spawner.alignToGroundNormal = true;
            spawner.normalAlignmentStrength = 0.9f;
            spawner.groundSinkRatio = 0.15f; // 地面に少し埋める

            spawner.addShadeSystem = false; // 木の影コライダーは不要
            spawner.removeColliders = false;
            spawner.containerName = "DebrisContainer";

            EditorUtility.SetDirty(spawner);
            EditorUtility.DisplayDialog(
                "Medieval Debris Preset",
                $"【瓦礫プリセットを適用しました】\n\n" +
                $"・木材プレハブ: {woodList.Count}個\n" +
                $"・瓦礫プレハブ: {rubbleList.Count}個\n" +
                $"・木材比率: 80% (木多め・瓦礫少なめ)\n" +
                $"・散布範囲: 建物の外壁際 (半径 3.8m 〜 7.0m)\n\n" +
                $"※パラメータを変更した後は、プリセットボタンを押さずに【Generate (生成・配置)】ボタンを押せば設定サイズで配置されます。",
                "OK"
            );
        }
    }
}
