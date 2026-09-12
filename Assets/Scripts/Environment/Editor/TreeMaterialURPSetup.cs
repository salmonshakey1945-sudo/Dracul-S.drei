using UnityEditor;
using UnityEngine;

namespace Dracul.Environment.Editor
{
    /// <summary>
    /// Tree9 のマテリアルおよびプレハブを URP (Universal Render Pipeline) Lit に変換・再設定し、
    /// リアルタイムの葉と幹の影が地面に確実に投影されるようにするユーティリティ。
    /// </summary>
    public static class TreeMaterialURPSetup
    {
        [MenuItem("Tools/Fix Tree9 Materials for URP (影の有効化)")]
        public static void FixTreeMaterials()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[TreeMaterialURPSetup] Universal Render Pipeline/Lit シェーダーが見つかりません。");
                return;
            }

            Material barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Tree9/bark14.mat");
            Material leafMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Tree9/Tree9_leafs.mat");

            if (barkMat != null)
            {
                Undo.RecordObject(barkMat, "Fix Bark Material");
                barkMat.shader = urpLitShader;
                barkMat.SetFloat("_Surface", 0);
                barkMat.SetFloat("_AlphaClip", 0);
                barkMat.SetFloat("_Cull", 2);
                barkMat.SetFloat("_Smoothness", 0.1f);
                EditorUtility.SetDirty(barkMat);
            }

            if (leafMat != null)
            {
                Undo.RecordObject(leafMat, "Fix Leaf Material");
                leafMat.shader = urpLitShader;
                leafMat.SetFloat("_Surface", 0);
                leafMat.SetFloat("_AlphaClip", 1);
                leafMat.SetFloat("_Cutoff", 0.35f);
                leafMat.SetFloat("_Cull", 0); // Double sided
                leafMat.SetFloat("_Smoothness", 0.15f);
                leafMat.EnableKeyword("_ALPHATEST_ON");
                leafMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                EditorUtility.SetDirty(leafMat);
            }

            // プレハブの MeshRenderer にマテリアルを直接再割り当て
            string[] prefabPaths = new string[]
            {
                "Assets/Tree9/Tree9_2.prefab",
                "Assets/Tree9/Tree9_3.prefab",
                "Assets/Tree9/Tree9_4.prefab",
                "Assets/Tree9/Tree9_5.prefab"
            };

            foreach (var pPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                if (prefab != null)
                {
                    MeshRenderer mr = prefab.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Undo.RecordObject(mr, "Assign URP Materials to Tree Prefab");
                        mr.sharedMaterials = new Material[] { barkMat, leafMat };
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            // シーン内の生成済みの木（TreeContainer配下）にも即座に適用
            var spawners = Object.FindObjectsByType<ProceduralTreeSpawner>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                Transform container = spawner.transform.Find(spawner.containerName);
                if (container != null)
                {
                    MeshRenderer[] renderers = container.GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in renderers)
                    {
                        Undo.RecordObject(r, "Assign URP Materials to Scene Trees");
                        r.sharedMaterials = new Material[] { barkMat, leafMat };
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TreeMaterialURPSetup] 木のプレハブおよびシーン内の木に URP マテリアルを設定し、影を有効化しました！");
            EditorUtility.DisplayDialog("マテリアル更新完了", "Tree9 のプレハブおよびシーン内の木に URP Lit マテリアルを適用しました！\n木が正常なテクスチャで表示され、リアルタイムの影が出るかご確認ください。", "OK");
        }
    }
}
