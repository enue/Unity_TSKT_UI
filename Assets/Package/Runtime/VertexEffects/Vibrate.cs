using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(Text))]
    public class Vibrate : BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;

        public float interval = 4f / 60f;
        public float distance = 4f;

        float lastModifiedTime = 0f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            UIVertex vertex = default;
            var vertexCount = vh.currentVertCount;
            for (int i = 0; i < vertexCount; i += VertexCountPerQuad)
            {
                var distance = Random.insideUnitSphere * this.distance;
                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    vh.PopulateUIVertex(ref vertex, i + j);
                    vertex.position += distance;
                    vh.SetUIVertex(vertex, i + j);
                }
            }
        }

        Text? text;
        Text Text
        {
            get
            {
                return text ? text! : (text = GetComponent<Text>());
            }
        }

        void Update()
        {
            if (Time.time - lastModifiedTime > interval)
            {
                lastModifiedTime = Time.time;
                Text.SetAllDirty();
            }
        }
    }
}
