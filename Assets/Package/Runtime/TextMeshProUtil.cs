#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

#if TSKT_UI_SUPPORT_TEXTMESHPRO
namespace TSKT
{
    public class TextMeshProUtil
    {
        readonly static List<TMPro.TMP_FontAsset> fontsToRefresh = new();

        // テクスチャがいっぱいになった時にFontAsset.ClearFontAssetDataを呼ぶ
        public static void SetText(TMPro.TMP_Text target, string value)
        {
            if (target.font.HasCharacters(value, out _, searchFallbacks: true, tryAddCharacter: true))
            {
                target.SetText(value);
            }
            else
            {
                target.SetText(value);
                fontsToRefresh.Add(target.font);

                UniTask.Yield().ToUniTask().ContinueWith(() =>
                {
                    RefreshFontAssetData(fontsToRefresh.Distinct().ToArray());
                    fontsToRefresh.Clear();
                });
            }
        }

        public static void RefreshFontAssetData(params TMPro.TMP_FontAsset[] fonts)
        {
            bool clearedGlobalFallback = false;

            using (UnityEngine.Pool.ListPool<TMPro.TMP_FontAsset>.Get(out var clearedFonts))
            {
                foreach (var font in fonts)
                {
                    if (font.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic
                        && !font.isMultiAtlasTexturesEnabled)
                    {
                        font.ClearFontAssetData();
                        clearedFonts.Add(font);
                    }
                    foreach (var it in font.fallbackFontAssetTable)
                    {
                        if (it.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic
                            && !font.isMultiAtlasTexturesEnabled)
                        {
                            it.ClearFontAssetData();
                            clearedFonts.Add(it);
                        }
                    }
                    foreach (var it in TMPro.TMP_Settings.fallbackFontAssets)
                    {
                        if (it.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic
                            && !font.isMultiAtlasTexturesEnabled)
                        {
                            it.ClearFontAssetData();
                            clearedFonts.Add(it);
                            clearedGlobalFallback = true;
                        }
                    }
                }

                if (clearedGlobalFallback || clearedFonts.Count > 0)
                {
                    var targetTexts = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (var it in targetTexts)
                    {
                        if (clearedGlobalFallback)
                        {
                            it.ForceMeshUpdate();
                        }
                        else if (it.font)
                        {
                            if (clearedFonts.Contains(it.font) || clearedFonts.Intersect(it.font.fallbackFontAssetTable).Any())
                            {
                                it.ForceMeshUpdate();
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
