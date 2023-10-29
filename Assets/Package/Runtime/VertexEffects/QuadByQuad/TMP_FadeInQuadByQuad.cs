using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public class TMP_FadeInQuadByQuad : TMP_QuadByQuad
    {
        protected override void ModifyQuad(TMP_VertexHelper vertexHelper, int startIndex, int count, float normalizedTime)
        {
            if (normalizedTime >= 1f)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                var index = i + startIndex;
                if (normalizedTime <= 0f)
                {
                    var colors = vertexHelper.GetColor(index);
                    for(int j =0; j< colors.Length; ++j)
                    {
                        colors[j] =  Color.clear;
                    }
                }
                else
                {
                    var colors = vertexHelper.GetColor(index);
                    for(int j =0; j< colors.Length; ++j)
                    {
                        colors[j].a = (byte)(colors[j].a * normalizedTime);
                    }
                }
            }
        }
    }
}
