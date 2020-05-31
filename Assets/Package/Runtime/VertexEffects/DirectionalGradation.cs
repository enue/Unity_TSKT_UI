using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace TSKT
{
    public class DirectionalGradation : BaseMeshEffect
    {
        [SerializeField]
        Color32 color = Color.white;

        [SerializeField]
        [Range(0f, 360f)]
        float angle = 0f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            UIVertex vertex = default;
            var vertexCount = vh.currentVertCount;

            float max = float.MinValue;
            float min = float.MaxValue;

            var vector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
            for(int i=0; i<vertexCount; ++i)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var dot = vertex.position.x * vector.x + vertex.position.y * vector.y;
                max = Mathf.Max(dot, max);
                min = Mathf.Min(dot, min);
            }

            if (max == min)
            {
                return;
            }

            for (int i = 0; i < vertexCount; ++i)
            {
                vh.PopulateUIVertex(ref vertex, i);

                var dot = vertex.position.x * vector.x + vertex.position.y * vector.y;
                var t = Mathf.InverseLerp(min, max, dot);
                var c = Color.Lerp(Color.white, color, t);
                vertex.color = c * vertex.color;

                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
