using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class DescentQuadByQuad : QuadByQuad
    {
        [SerializeField]
        float height = 8f;

        protected override void ModifyQuad(VertexHelper vh, int startIndex, int count, float normalizedTime)
        {
            if (normalizedTime >= 1f)
            {
                return;
            }
            if (normalizedTime <= 0f)
            {
                return;
            }
            var h = (1f - normalizedTime) * height;

            UIVertex vertex = default;
            for (int i = 0; i < count; ++i)
            {
                var index = startIndex + i;
                vh.PopulateUIVertex(ref vertex, index);
                vertex.position += new Vector3(0f, h, 0f);
                vh.SetUIVertex(vertex, index);
            }
        }
    }
}
