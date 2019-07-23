using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class PopQuadByQuad : QuadByQuad
    {
        [SerializeField]
        float height = 8f;

        protected override void ModifyQuad(VertexHelper vh, int startIndex, int count, float normalizedTime)
        {
            var h = normalizedTime * (1f - normalizedTime) * 4f * height;

            UIVertex vertex = default;
            for (int i = 0; i < count; ++i)
            {
                var index = startIndex + i;
                vh.PopulateUIVertex(ref vertex, index);

                if (normalizedTime <= 0f)
                {
                    vertex.color = Color.clear;
                }
                else
                {
                    vertex.position += new Vector3(0f, h, 0f);
                }
                vh.SetUIVertex(vertex, index);
            }
        }
    }
}
