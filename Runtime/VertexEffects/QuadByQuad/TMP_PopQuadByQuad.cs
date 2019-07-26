using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class TMP_PopQuadByQuad : TMP_QuadByQuad
    {
        [SerializeField]
        float height = 8f;

        protected override void ModifyQuad(ref List<Vector3> vertices, ref List<Color> colors, int startIndex, int count, float normalizedTime)
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

                vertices[index] += new Vector3(0f, h, 0f);
            }
        }
    }
}
