using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TSKT
{
    public class TMP_VertexHelper
    {
        readonly TMP_Text text;

        Mesh mesh;
        Mesh Mesh
        {
            get
            {
                if (!mesh)
                {
                    text.ForceMeshUpdate();
                    mesh = text.mesh;
                }
                return mesh;
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

        readonly List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> Vertices
        {
            get
            {
                if (!verticesModified)
                {
                    Mesh.GetVertices(vertices);
                    verticesModified = true;
                }
                return vertices;
            }
        }

        bool verticesModified = false;
        bool colorsModified = false;

        public TMP_VertexHelper(TMP_Text text)
        {
            this.text = text;
        }

        public void Consume()
        {
            if (verticesModified)
            {
                Mesh.SetVertices(vertices);
            }
            if (colorsModified)
            {
                Mesh.SetColors(colors);
            }
            if (verticesModified || colorsModified)
            {
                text.UpdateGeometry(Mesh, 0);
            }

            verticesModified = false;
            colorsModified = false;
            mesh = null;
        }
    }
}
