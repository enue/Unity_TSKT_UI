using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class TypingEffect : BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;

        [SerializeField]
        int visibleQuadCount = 0;
        public int VisibleQuadCount
        {
            get => visibleQuadCount;
            set
            {
                visibleQuadCount = value;
                Text.SetAllDirty();
            }
        }

        Text text;
        Text Text
        {
            get
            {
                return text ? text : (text = GetComponent<Text>());
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            var quadCount = vh.currentVertCount / VertexCountPerQuad;

            UIVertex vertex = default;
            for (int i = 0; i < quadCount; ++i)
            {
                var visible = i < visibleQuadCount;
                if (!visible)
                {
                    for(int j=0; j<VertexCountPerQuad; ++j)
                    {
                        var index = i * VertexCountPerQuad + j;
                        vh.PopulateUIVertex(ref vertex, index);
                        vertex.color = Color.clear;
                        vh.SetUIVertex(vertex, index);
                    }
                }
            }
        }
    }
}
