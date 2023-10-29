using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public class TMP_PopQuadByQuad : TMP_QuadByQuad
    {
        [SerializeField]
        float height = 8f;

        protected override void ModifyQuad(TMP_VertexHelper vertexHelper, int startIndex, int count, float normalizedTime)
        {
            if (normalizedTime >= 1f)
            {
                return;
            }
            if (normalizedTime <= 0f)
            {
                return;
            }

            var h = normalizedTime * (1f - normalizedTime) * 4f * height;

            for (int i = 0; i < count; ++i)
            {
                var index = startIndex + i;

                var vertices = vertexHelper.GetVertex(index);
                for (int j = 0; j < vertices.Length; ++j)
                {
                    vertices[j] += new Vector3(0f, h, 0f);
                }
            }
        }
    }
}
