using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public abstract class ShadowVertexModifier : Shadow
    {
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }
            using (UnityEngine.Pool.ListPool<UIVertex>.Get(out var vertices))
            {
                vertices.Clear();
                vh.GetUIVertexStream(vertices);
                ModifyMesh(vertices);
                vh.Clear();
                vh.AddUIVertexTriangleStream(vertices);
            }
        }

        protected abstract void ModifyMesh(List<UIVertex> verts);
    }
}
