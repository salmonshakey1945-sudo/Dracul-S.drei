using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dracul.Environment.Editor
{
    /// <summary>
    /// 草の2Dテクスチャ（grass01.tga 等）から、3D空間に配置可能な「立体草プレハブ（クロス型メッシュ＋URPマテリアル）」を自動生成するユーティリティ。
    /// </summary>
    public static class GrassPrefabGenerator
    {
        [MenuItem("Tools/Generate Grass Prefabs from Textures (草プレハブを自動生成)")]
        public static void GenerateAllGrassPrefabs()
        {
            CreateGrassPrefabFromTexture("Assets/NatureStarterKit2/Textures/grass01.tga", "Grass01_CrossPrefab");
            CreateGrassPrefabFromTexture("Assets/NatureStarterKit2/Textures/grass02.tga", "Grass02_CrossPrefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "草プレハブ生成完了",
                "草プレハブを自動生成しました！\n\n" +
                "生成場所: Assets/NatureStarterKit2/Prefabs/\n" +
                "・Grass01_CrossPrefab.prefab\n" +
                "・Grass02_CrossPrefab.prefab\n\n" +
                "これを ProceduralTreeSpawner の 'Tree Prefabs' に登録して配置してください。",
                "OK"
            );
        }

        /// <summary>
        /// 指定テクスチャからクロス型草プレハブを生成
        /// </summary>
        public static GameObject CreateGrassPrefabFromTexture(string texturePath, string prefabName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[GrassPrefabGenerator] テクスチャが見つかりません: {texturePath}");
                return null;
            }

            // 保存先ディレクトリ
            string folderPath = "Assets/NatureStarterKit2/Prefabs";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 1. マテリアル作成
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }

            string matPath = $"{folderPath}/{prefabName}_Mat.mat";
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (grassMat == null)
            {
                grassMat = new Material(urpShader);
                AssetDatabase.CreateAsset(grassMat, matPath);
            }

            grassMat.shader = urpShader;
            grassMat.SetTexture("_BaseMap", texture);
            if (grassMat.HasProperty("_MainTex"))
            {
                grassMat.SetTexture("_MainTex", texture);
            }

            // 草の緑色ティント（白黒テクスチャを自然な緑色に着色）
            Color grassGreen = new Color(0.38f, 0.68f, 0.20f, 1.0f);
            grassMat.SetColor("_BaseColor", grassGreen);
            if (grassMat.HasProperty("_Color"))
            {
                grassMat.SetColor("_Color", grassGreen);
            }

            // アルファクリップ / 両面描画設定 (URP Lit)
            grassMat.SetFloat("_Surface", 0); // Opaque (Alpha Test)
            grassMat.SetFloat("_AlphaClip", 1);
            grassMat.SetFloat("_Cutoff", 0.35f);
            grassMat.SetFloat("_Cull", 0); // Double Sided (両面描画)
            grassMat.SetFloat("_Smoothness", 0.05f);
            grassMat.EnableKeyword("_ALPHATEST_ON");
            grassMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            EditorUtility.SetDirty(grassMat);

            // 2. クロス型草メッシュ作成 (3面クロス: よりどの角度から見ても自然に見える形状)
            string meshPath = $"{folderPath}/{prefabName}_Mesh.asset";
            Mesh grassMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (grassMesh == null)
            {
                grassMesh = CreateCrossGrassMesh(width: 1.0f, height: 1.0f);
                AssetDatabase.CreateAsset(grassMesh, meshPath);
            }

            // 3. GameObject & Prefab の構築
            string prefabPath = $"{folderPath}/{prefabName}.prefab";
            GameObject tempGo = new GameObject(prefabName);

            MeshFilter mf = tempGo.AddComponent<MeshFilter>();
            mf.sharedMesh = grassMesh;

            MeshRenderer mr = tempGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = grassMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 草は影を落とさず軽量化
            mr.receiveShadows = true; // 地面の影や木の影は受ける

            // プレハブとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);

            Debug.Log($"[GrassPrefabGenerator] 草プレハブを作成しました: {prefabPath}");
            return prefab;
        }

        /// <summary>
        /// 3面クロス型（Y軸まわりに60度ずつ回転した3枚の板）の草メッシュを生成
        /// </summary>
        private static Mesh CreateCrossGrassMesh(float width, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "CrossGrassMesh";

            int planeCount = 3; // 3枚の板で立体感を出す
            int vertexCount = planeCount * 4;
            int triangleCount = planeCount * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            int[] triangles = new int[triangleCount];

            float halfW = width * 0.5f;

            for (int i = 0; i < planeCount; i++)
            {
                float angle = i * (180f / planeCount);
                Quaternion rot = Quaternion.Euler(0f, angle, 0f);

                int vIndex = i * 4;
                int tIndex = i * 6;

                // 4頂点 (底辺中央が (0,0,0))
                vertices[vIndex + 0] = rot * new Vector3(-halfW, 0f, 0f);
                vertices[vIndex + 1] = rot * new Vector3(halfW, 0f, 0f);
                vertices[vIndex + 2] = rot * new Vector3(-halfW, height, 0f);
                vertices[vIndex + 3] = rot * new Vector3(halfW, height, 0f);

                uvs[vIndex + 0] = new Vector2(0f, 0f);
                uvs[vIndex + 1] = new Vector2(1f, 0f);
                uvs[vIndex + 2] = new Vector2(0f, 1f);
                uvs[vIndex + 3] = new Vector2(1f, 1f);

                // 上向き法線（ライティングを柔らかく均一にする）
                Vector3 normal = Vector3.up;
                normals[vIndex + 0] = normal;
                normals[vIndex + 1] = normal;
                normals[vIndex + 2] = normal;
                normals[vIndex + 3] = normal;

                // 表裏両面のポリゴン
                triangles[tIndex + 0] = vIndex + 0;
                triangles[tIndex + 1] = vIndex + 2;
                triangles[tIndex + 2] = vIndex + 1;

                triangles[tIndex + 3] = vIndex + 2;
                triangles[tIndex + 4] = vIndex + 3;
                triangles[tIndex + 5] = vIndex + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
