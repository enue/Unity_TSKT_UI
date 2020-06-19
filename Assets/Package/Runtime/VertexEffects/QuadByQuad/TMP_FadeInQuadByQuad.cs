﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
                    vertexHelper.Colors[index] = Color.clear;
                }
                else
                {
                    var color = vertexHelper.Colors[index];
                    color.a = color.a * normalizedTime;
                    vertexHelper.Colors[index] = color;
                }
            }
        }
    }
}