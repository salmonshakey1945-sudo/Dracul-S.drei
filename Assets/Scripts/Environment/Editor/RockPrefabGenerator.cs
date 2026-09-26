using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dracul.Environment.Editor
{
    /// <summary>
    /// 自然でゴツゴツしたローポリ岩の3Dメッシュとプレハブ（URP対応・コライダー付き）を自動生成するエディタ拡張。
    /// </summary>
    public static class RockPrefabGenerator
    {
        [MenuItem("Tools/Generate Rock Prefabs (岩プレハブを自動生成)")]
        public static void GenerateAllRockPrefabs()
        {
            string folderPath = "Assets/Prefab/Rocks";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // マテリアル取得または作成 (DesertCliffRock_URPShader を優先使用)
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/WorldMaterialsFree/URPMaterials/DesertCliffRock_URPShader.mat");
            if (rockMat == null)
            {
                rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/WorldMaterialsFree/URPMaterials/CoarseConcrete_URPShader.mat");
            }
            if (rockMat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                rockMat = new Material(urpShader);
                rockMat.SetColor("_BaseColor", new Color(0.48f, 0.46f, 0.44f, 1f));
                rockMat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(rockMat, $"{folderPath}/Rock_Default_Mat.mat");
            }

            // 異なる3種類の岩メッシュとプレハブを生成
            CreateRockPrefab("Rock_01_Boulder", folderPath, rockMat, seed: 101, flatness: 0.75f, noiseStrength: 0.35f);
            CreateRockPrefab("Rock_02_Flat", folderPath, rockMat, seed: 202, flatness: 0.55f, noiseStrength: 0.30f);
            CreateRockPrefab("Rock_03_Angular", folderPath, rockMat, seed: 303, flatness: 0.85f, noiseStrength: 0.42f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "岩プレハブ生成完了",
                "岩プレハブを3種類自動生成しました！\n\n" +
                "保存先: Assets/Prefab/Rocks/\n" +
                "・Rock_01_Boulder.prefab (ドッシリした大岩)\n" +
                "・Rock_02_Flat.prefab (平たい岩)\n" +
                "・Rock_03_Angular.prefab (角ばった岩)\n\n" +
                "ProceduralTreeSpawner の 'Tree Prefabs' に登録して配置してください。",
                "OK"
            );
        }

        private static GameObject CreateRockPrefab(string name, string folderPath, Material material, int seed, float flatness, float noiseStrength)
        {
            string meshPath = $"{folderPath}/{name}_Mesh.asset";
            Mesh rockMesh = CreateRockMesh(seed, flatness, noiseStrength);
            AssetDatabase.CreateAsset(rockMesh, meshPath);

            string prefabPath = $"{folderPath}/{name}.prefab";
            GameObject go = new GameObject(name);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = rockMesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            // プレイヤーや弾丸が当たるよう MeshCollider (Convex) を付与
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = rockMesh;
            mc.convex = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[RockPrefabGenerator] 作成完了: {prefabPath}");
            return prefab;
        }

        /// <summary>
        /// キューブを細分化し、ノイズでゴツゴツと有機的に変形させた岩メッシュを生成
        /// </summary>
        private static Mesh CreateRockMesh(int seed, float flatness, float noiseStrength)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RockMesh";

            Random.State oldState = Random.state;
            Random.InitState(seed);

            // 基準の多面体（正二十面体/キューブ細分化風の26頂点ジオメトリ）
            int segments = 4;
            var verticesList = new System.Collections.Generic.List<Vector3>();
            var trianglesList = new System.Collections.Generic.List<int>();

            // 球面座標からサンプリングして変形
            int latLines = 8;
            int lonLines = 10;

            for (int lat = 0; lat <= latLines; lat++)
            {
                float theta = lat * Mathf.PI / latLines; // 0 to PI
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonLines; lon++)
                {
                    float phi = lon * 2f * Mathf.PI / lonLines; // 0 to 2PI
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    Vector3 normal = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);

                    // 3次元パーリンノイズ風の有機的歪み
                    float noise1 = Mathf.PerlinNoise(normal.x * 2.5f + seed, normal.z * 2.5f + seed);
                    float noise2 = Mathf.PerlinNoise(normal.y * 3.0f + seed * 2, normal.x * 3.0f + seed * 2);
                    float displacement = 1.0f + (noise1 * 0.6f + noise2 * 0.4f - 0.5f) * noiseStrength;

                    // ランダムな細かな凹凸
                    displacement += (Random.value - 0.5f) * 0.1f * noiseStrength;

                    Vector3 pos = normal * displacement;
                    // Y軸（高さ）を圧縮して平たい岩のドッシリ感を出す
                    pos.y *= flatness;

                    // 底辺（地面側）を少し平らに近づける（接地しやすくする）
                    if (pos.y < -0.1f)
                    {
                        pos.y *= 0.8f;
                    }

                    verticesList.Add(pos);
                }
            }

            for (int lat = 0; lat < latLines; lat++)
            {
                for (int lon = 0; lon < lonLines; lon++)
                {
                    int current = lat * (lonLines + 1) + lon;
                    int next = current + lonLines + 1;

                    trianglesList.Add(current);
                    trianglesList.Add(current + 1);
                    trianglesList.Add(next);

                    trianglesList.Add(current + 1);
                    trianglesList.Add(next + 1);
                    trianglesList.Add(next);
                }
            }

            // フラットシェーディング化（ローポリ岩の角ばった質感を強調）
            Vector3[] origVerts = verticesList.ToArray();
            int[] origTriangles = trianglesList.ToArray();

            Vector3[] flatVerts = new Vector3[origTriangles.Length];
            Vector2[] flatUVs = new Vector2[origTriangles.Length];
            Vector3[] flatNormals = new Vector3[origTriangles.Length];
            int[] flatTriangles = new int[origTriangles.Length];

            for (int i = 0; i < origTriangles.Length; i += 3)
            {
                Vector3 v0 = origVerts[origTriangles[i]];
                Vector3 v1 = origVerts[origTriangles[i + 1]];
                Vector3 v2 = origVerts[origTriangles[i + 2]];

                Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                flatVerts[i] = v0;
                flatVerts[i + 1] = v1;
                flatVerts[i + 2] = v2;

                flatNormals[i] = faceNormal;
                flatNormals[i + 1] = faceNormal;
                flatNormals[i + 2] = faceNormal;

                flatUVs[i] = new Vector2(v0.x + v0.z, v0.y);
                flatUVs[i + 1] = new Vector2(v1.x + v1.z, v1.y);
                flatUVs[i + 2] = new Vector2(v2.x + v2.z, v2.y);

                flatTriangles[i] = i;
                flatTriangles[i + 1] = i + 1;
                flatTriangles[i + 2] = i + 2;
            }

            mesh.vertices = flatVerts;
            mesh.normals = flatNormals;
            mesh.uv = flatUVs;
            mesh.triangles = flatTriangles;
            mesh.RecalculateBounds();

            Random.state = oldState;
            return mesh;
        }
    }
}
