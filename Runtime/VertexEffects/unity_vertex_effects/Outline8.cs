using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TSKT
{
    public class Outline8 : ShadowVertexModifier
    {
        override protected void ModifyMesh(List<UIVertex> verts)
        {
            var original = verts.Count;
            var count = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        var next = count + original;
                        ApplyShadow(verts, effectColor, count, next, effectDistance.x * x, effectDistance.y * y);
                        count = next;
                    }
                }
            }
        }
    }
}
