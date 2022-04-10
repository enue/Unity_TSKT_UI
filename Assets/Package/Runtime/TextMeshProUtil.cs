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
            if (target.font.atlasPopulationMode != TMPro.AtlasPopulationMode.Dynamic)
            {
                target.SetText(value);
                return;
            }

            if (target.font.HasCharacters(value, out _, searchFallbacks: false, tryAddCharacter: true))
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
            font.ClearFontAssetData();
            var targetTexts = Object.FindObjectsOfType<TMPro.TMP_Text>(includeInactive: true);
            foreach (var it in targetTexts)
            {
                if (it.font == font)
                {
                    it.ForceMeshUpdate();
                }
            }
        }
    }
}
#endif
