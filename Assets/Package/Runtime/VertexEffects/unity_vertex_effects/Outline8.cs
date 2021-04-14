using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TSKT
{
    public class Outline8 : ShadowVertexModifier
    {
        [SerializeField]
        Vector2 offset = Vector2.zero;

        [SerializeField]
        bool constantPixelSize = false;

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

                        var p = effectDistance.x * x + offset.x;
                        var q = effectDistance.y * y + offset.y;
                        if (constantPixelSize)
                        {
                            var scale = transform.lossyScale;
                            p /= scale.x;
                            q /= scale.y;
                        }

                        ApplyShadow(verts, effectColor, count, next,
                            p, q);
                        count = next;
                    }
                }
            }
        }
    }
}
