using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    [System.Obsolete("use TMP_Text.maxVisibleCharacters")]
    public class TMP_TypingEffect : TMP_BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;

        public int visibleQuadCount = 0;

        public override void Modify(TMP_VertexHelper vertexHelper)
        {
            for (int i = 0; i < vertexHelper.CharacterCount; ++i)
            {
                var visible = i < visibleQuadCount;
                if (!visible)
                {
                    var colors = vertexHelper.GetColor(i);
                    for (int j = 0; j < colors.Length; ++j)
                    {
                        colors[j] = Color.clear;
                    }
                }
            }
        }
    }
}
