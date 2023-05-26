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
            var quadCount = vertexHelper.Colors.Count / VertexCountPerQuad;

            for (int i = 0; i < quadCount; ++i)
            {
                var visible = i < visibleQuadCount;
                if (!visible)
                {
                    for(int j=0; j<VertexCountPerQuad; ++j)
                    {
                        var index = i * VertexCountPerQuad + j;
                        vertexHelper.Colors[index] = Color.clear;
                    }
                }
            }
        }
    }
}
