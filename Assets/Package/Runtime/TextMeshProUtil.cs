﻿#nullable enable
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
            if (target.font.TryAddCharacters(value))
            {
                target.SetText(value);
            }
            else
            {
                var targetTexts = Object.FindObjectsOfType<TMPro.TMP_Text>(includeInactive: true)
                    .Where(_ => _ != target && _.font == target.font)
                    .Select(_ => (label: _, text: _.text))
                    .ToArray();
                target.font.ClearFontAssetData();
                target.SetText(value);
                foreach (var (label, text) in targetTexts)
                {
                    label.SetText(text);
                }
            }
        }
    }
}
#endif
