using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public class RadialGradation : BaseMeshEffect
    {
        [SerializeField]
        Color32 color = Color.white;

        [SerializeField]
        Vector3 pivot = default;

        [SerializeField]
        float minRadius = 0f;

        [SerializeField]
        float maxRadius = 100f;

        [SerializeField]
        bool reverse = false;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            UIVertex vertex = default;
            var vertexCount = vh.currentVertCount;
            for (int i = 0; i < vertexCount; ++i)
            {
                vh.PopulateUIVertex(ref vertex, i);

                var t = (Vector3.Distance(vertex.position, pivot) - minRadius) / (maxRadius - minRadius);
                if (reverse)
                {
                    t = 1f - t;
                }
                vertex.color = Color32.Lerp(vertex.color, color, t);

                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
