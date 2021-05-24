using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    public class Gradation : BaseMeshEffect
    {
        [SerializeField]
        Color color = Color.white;

        [SerializeField]
        bool[] targetVertices = {false, false, true, true};

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }
            if (targetVertices == null)
            {
                return;
            }

            if (targetVertices.Length == 0)
            {
                return;
            }

            UIVertex vertex = default;
            var vertexCount = vh.currentVertCount;
            for(int i=0; i<vertexCount; ++i)
            {
                if (targetVertices[i % targetVertices.Length])
                {
                    vh.PopulateUIVertex(ref vertex, i);
                    vertex.color = color;
                    vh.SetUIVertex(vertex, i);
                }
            }
        }
    }
}
