using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class TMP_TypingEffect : TMP_BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;

        public int visibleQuadCount = 0;

        public override void Modify(ref List<Vector3> vertices, ref List<Color> colors)
        {
            var quadCount = colors.Count / VertexCountPerQuad;

            for (int i = 0; i < quadCount; ++i)
            {
                var visible = i < visibleQuadCount;
                if (!visible)
                {
                    for(int j=0; j<VertexCountPerQuad; ++j)
                    {
                        var index = i * VertexCountPerQuad + j;
                        colors[index] = Color.clear;
                    }
                }
            }
        }
    }
}
