#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class ClearFontAssetSample : MonoBehaviour
    {
        [SerializeField]
        TMPro.TMP_Text[] labels = default!;

        [SerializeField]
        TMPro.TMP_Text[] fixedLabels = default!;

        IEnumerator Start()
        {
            for (int i = 0; i < 256; ++i)
            {
                var s = new string(new char[] { (char)('あ' + i) });
                labels[i % labels.Length].SetText(s);
                TextMeshProUtil.SetText(fixedLabels[i % fixedLabels.Length], s);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}


