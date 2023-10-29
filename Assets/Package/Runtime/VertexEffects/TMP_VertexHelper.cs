using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
#nullable enable

namespace TSKT
{
    public class TMP_VertexHelper
    {
        const int VertexCountPerQuad = 4;
        readonly TextMeshProUGUI text;

        public Span<Vector3> GetVertex(int characterIndex)
        {
            var mat = text.textInfo.characterInfo[characterIndex].materialReferenceIndex;
            var vertexIndex = text.textInfo.characterInfo[characterIndex].vertexIndex;
            return text.textInfo.meshInfo[mat].vertices.AsSpan().Slice(vertexIndex, VertexCountPerQuad);
        }

        public Span<Color32> GetColor(int characterIndex)
        {
            var mat = text.textInfo.characterInfo[characterIndex].materialReferenceIndex;
            var vertexIndex = text.textInfo.characterInfo[characterIndex].vertexIndex;
            return text.textInfo.meshInfo[mat].colors32.AsSpan().Slice(vertexIndex, VertexCountPerQuad);
        }
        public int CharacterCount => text.textInfo.characterCount;

        public TMP_VertexHelper(TextMeshProUGUI text)
        {
            this.text = text;
        }
        public Bounds GetCharacterBounds(int index)
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach(var it in GetVertex(index))
            {
                minX = Mathf.Min(minX, it.x);
                maxX = Mathf.Max(maxX, it.x);
                minY = Mathf.Min(minY, it.y);
                maxY = Mathf.Max(maxY, it.y);
                minZ = Mathf.Min(minZ, it.z);
                maxZ = Mathf.Max(maxZ, it.z);
            }

            return new Bounds(
                new Vector3(
                    (maxX + minX) / 2f,
                    (maxY + minY) / 2f,
                    (maxZ + minZ) / 2f),
                new Vector3(
                    maxX - minX,
                    maxY - minY,
                    maxZ - minZ));
        }

        public void Consume()
        {
            int index = 0;
            foreach (var it in text.textInfo.meshInfo)
            {
                it.mesh.SetVertices(it.vertices);
                it.mesh.SetColors(it.colors32);
                text.UpdateGeometry(it.mesh, index);
                ++index;
            }
        }
    }
}
