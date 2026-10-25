using UnityEngine;
using UnityEditor;
using System.IO;

namespace MedievalVillageFixer
{
    [InitializeOnLoad]
    public static class FixMedievalVillageMaterials
    {
        private const string TargetFolderPath = "Assets/Medieval Village Building Pack/Materials";
        private const string PrefKey = "MedievalVillage_MaterialsFixed_v1";

        static FixMedievalVillageMaterials()
        {
            EditorApplication.delayCall += CheckAndAutoFix;
        }

        private static void CheckAndAutoFix()
        {
            if (!SessionState.GetBool(PrefKey, false))
            {
                SessionState.SetBool(PrefKey, true);
                // 未変換のマテリアルがあるか確認
                if (HasUnconvertedMaterials())
                {
                    Debug.Log("[Medieval Village Fixer] Built-inシェーダーのマテリアルを検出しました。URPへの自動変換を実行します...");
                    FixMaterials();
                }
            }
        }

        private static bool HasUnconvertedMaterials()
        {
            if (!Directory.Exists(TargetFolderPath)) return false;

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolderPath });
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null)
                {
                    if (mat.shader.name.StartsWith("Mikes Emporium/") || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [MenuItem("Tools/Fix Medieval Village Materials (URP)")]
        public static void FixMaterials()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[Medieval Village Fixer] 'Universal Render Pipeline/Lit' シェーダーが見つかりませんでした。プロジェクトがURP構成になっているか確認してください。");
                return;
            }

            if (!Directory.Exists(TargetFolderPath))
            {
                Debug.LogWarning($"[Medieval Village Fixer] フォルダが見つかりません: {TargetFolderPath}");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolderPath });
            int convertedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in matGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string matName = mat.name;

                    // 既存のテクスチャ参照を保持
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    if (mainTex == null && mat.HasProperty("_BaseMap"))
                    {
                        mainTex = mat.GetTexture("_BaseMap");
                    }

                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                    // シェーダーを URP/Lit に切り替え
                    mat.shader = urpLitShader;

                    // テクスチャ設定
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                    }
                    mat.SetColor("_BaseColor", Color.white);

                    if (bumpMap != null)
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                        mat.SetFloat("_BumpScale", 1.0f);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    else
                    {
                        mat.DisableKeyword("_NORMALMAP");
                    }

                    // マテリアルごとの個別設定（透明度やカットアウト）
                    if (matName.Contains("Windows"))
                    {
                        // 窓ガラス: 半透明 (Transparent)
                        mat.SetFloat("_Surface", 1); // 1 = Transparent
                        mat.SetFloat("_Blend", 0);   // 0 = Alpha blend
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.45f));
                        mat.SetFloat("_Smoothness", 0.9f);
                        mat.SetFloat("_Metallic", 0.1f);

                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.DisableKeyword("_ALPHATEST_ON");
                    }
                    else if (matName.Contains("Transparent") || matName.Contains("StrawRoofTransparent"))
                    {
                        // わら屋根: Alpha Cutout
                        mat.SetFloat("_Surface", 0); // Opaque
                        mat.SetFloat("_AlphaClip", 1);
                        mat.SetFloat("_Cutoff", 0.35f);
                        mat.SetFloat("_Smoothness", 0.1f);
                        mat.SetFloat("_Metallic", 0.0f);

                        mat.EnableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    }
                    else
                    {
                        // 通常の不透明 (Opaque)
                        mat.SetFloat("_Surface", 0);
                        mat.SetFloat("_AlphaClip", 0);
                        mat.SetFloat("_Smoothness", 0.2f);
                        mat.SetFloat("_Metallic", matName.Contains("Metal") ? 0.8f : 0.0f);

                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    }

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Medieval Village Fixer] 完了: {convertedCount} 個のマテリアルを URP Lit シェーダーに変換しテクスチャを再設定しました。");
            EditorUtility.DisplayDialog("Medieval Village Fixer", $"{convertedCount} 個のマテリアルを正常に URP/Lit に変換しテクスチャを修復しました。", "OK");
        }
    }
}
