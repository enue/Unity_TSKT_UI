using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TSKT
{
    public class BoxOutline : ShadowVertexModifier
    {
        const int maxHalfSampleCount = 20;

        [SerializeField]
        [Range(1, maxHalfSampleCount)]
        int m_halfSampleCountX = 1;
        [SerializeField]
        [Range(1, maxHalfSampleCount)]
        int m_halfSampleCountY = 1;

        public int halfSampleCountX
        {
            get
            {
                return m_halfSampleCountX;
            }

            set
            {
                m_halfSampleCountX = Mathf.Clamp(value, 1, maxHalfSampleCount);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public int halfSampleCountY
        {
            get
            {
                return m_halfSampleCountY;
            }

            set
            {
                m_halfSampleCountY = Mathf.Clamp(value, 1, maxHalfSampleCount);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        override protected void ModifyMesh(List<UIVertex> vertices)
        {
            var original = vertices.Count;
            var count = 0;
            var dx = effectDistance.x / m_halfSampleCountX;
            var dy = effectDistance.y / m_halfSampleCountY;
            for (int x = -m_halfSampleCountX; x <= m_halfSampleCountX; x++)
            {
                for (int y = -m_halfSampleCountY; y <= m_halfSampleCountY; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        var next = count + original;
                        ApplyShadow(vertices, effectColor, count, next, dx * x, dy * y);
                        count = next;
                    }
                }
            }
        }
    }
}
