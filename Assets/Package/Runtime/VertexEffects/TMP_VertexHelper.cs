using System.Collections;
using System.Collections.Generic;

#if TSKT_UI_SUPPORT_TEXTMESHPRO

using TMPro;
using UnityEngine;
#nullable enable
using System;

namespace TSKT
{
    public class TMP_VertexHelper
    {
        const int VertexCountPerQuad = 4;
        readonly TMP_Text text;

        Mesh? mesh;
        Mesh Mesh
        {
            get
            {
                if (!mesh)
                {
                    text.ForceMeshUpdate();
                    mesh = text.mesh;
                }
                return mesh!;
            }
        }

        readonly List<Color> colors = new List<Color>();
        public List<Color> Colors
        {
            get
            {
                if (!colorsModified)
                {
                    Mesh.GetColors(colors);
                    colorsModified = true;
                }
                return colors;
            }
        }

        readonly List<Vector3> vertices = new();
        public List<Vector3> Vertices
        {
            get
            {
                if (vertices.Count == 0)
                {
                    Mesh.GetVertices(vertices);
                }
                return vertices;
            }
        }
        bool colorsModified = false;

        public TMP_VertexHelper(TMP_Text text)
        {
            this.text = text;
        }

        public RangeInt GetVertexRangeOfQuad(int index)
        {
            return new RangeInt(index * VertexCountPerQuad, VertexCountPerQuad);
        }
        public Bounds GetQuadBounds(int index)
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            for (int i = 0; i < VertexCountPerQuad; ++i)
            {
                var v = Vertices[index * VertexCountPerQuad + i];
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minY = Mathf.Min(minY, v.y);
                maxY = Mathf.Max(maxY, v.y);
                minZ = Mathf.Min(minZ, v.z);
                maxZ = Mathf.Max(maxZ, v.z);
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
            if (vertices.Count > 0)
            {
                Mesh.SetVertices(vertices);
            }
            if (colorsModified)
            {
                Mesh.SetColors(colors);
            }
            if (vertices.Count > 0 || colorsModified)
            {
                text.UpdateGeometry(Mesh, 0);
            }

            vertices.Clear();
            colorsModified = false;
            mesh = null;
        }
    }
}
#endif
