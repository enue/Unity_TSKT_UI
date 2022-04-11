#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if TSKT_UI_SUPPORT_TEXTMESHPRO
namespace TSKT
{
    public class TextMeshProUtil
    {
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
                RefreshFontAssetData(target.font);
            }
        }
        public static void RefreshFontAssetData(TMPro.TMP_FontAsset font)
        {
            using (UnityEngine.Pool.ListPool<TMPro.TMP_FontAsset>.Get(out var clearedFonts))
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
                if (clearedFonts.Count > 0)
                {
                    var targetTexts = Object.FindObjectsOfType<TMPro.TMP_Text>(includeInactive: true);
                    foreach (var it in targetTexts)
                    {
                        if (it.font)
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
