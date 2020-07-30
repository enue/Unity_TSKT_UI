using System.Collections;
using System.Collections.Generic;

#if TSKT_UI_SUPPORT_TEXTMESHPRO
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
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
#endif
