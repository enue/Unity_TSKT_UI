using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public class FadeInQuadByQuad : QuadByQuad
    {
        protected override void ModifyQuad(VertexHelper vh, int startIndex, int count, float normalizedTime)
        {
            if (normalizedTime >= 1f)
            {
                return;
            }

            UIVertex vertex = default;
            for (int i = 0; i < count; ++i)
            {
                var index = i + startIndex;
                vh.PopulateUIVertex(ref vertex, index);
                if (normalizedTime <= 0f)
                {
                    vertex.color = Color.clear;
                }
                else
                {
                    var color = vertex.color;
                    color.a = (byte)(color.a * normalizedTime);
                    vertex.color = color;
                }
                vh.SetUIVertex(vertex, index);
            }
        }
    }
}
