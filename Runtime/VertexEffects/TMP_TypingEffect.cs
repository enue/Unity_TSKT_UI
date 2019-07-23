using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class TMP_TypingEffect : MonoBehaviour
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
                Refresh();
            }
        }

        TMP_Text text;
        TMP_Text Text => text ?? (text = GetComponent<TMP_Text>());

        readonly List<Color> colors = new List<Color>();

        void Refresh()
        {
            Text.ForceMeshUpdate();
            var mesh = Text.mesh;
            mesh.GetColors(colors);

            var quadCount = colors.Count / VertexCountPerQuad;

            for (int i = 0; i < quadCount; ++i)
            {
                var visible = i < visibleQuadCount;
                if (!visible)
                {
                    for(int j=0; j<VertexCountPerQuad; ++j)
                    {
                        var index = i * VertexCountPerQuad + j;
                        colors[index] = Color.clear;
                    }
                }
            }
            mesh.SetColors(colors);

            Text.UpdateGeometry(mesh, 0);
        }
    }
}
