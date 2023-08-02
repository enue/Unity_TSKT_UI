#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;

namespace TSKT
{
    public class TextMeshProUtil
    {
        [Obsolete]
        public static void SetText(TMPro.TMP_Text target, string value)
        {
            target.text = value;
        }

        [Obsolete("user multi atlas textures")]
        public static void RefreshFontAssetData(params TMPro.TMP_FontAsset[] fonts)
        {
        }
    }
}
