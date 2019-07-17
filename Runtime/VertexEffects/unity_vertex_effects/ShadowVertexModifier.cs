using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
            var vertices = UIVerticesPool.Get();
            vh.GetUIVertexStream(vertices);
            ModifyMesh(vertices);
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
            UIVerticesPool.Release(vertices);
        }

        protected abstract void ModifyMesh(List<UIVertex> verts);
    }
}
