using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#nullable enable


namespace TSKT
{
    public class CircleOutline : ShadowVertexModifier
    {
        [SerializeField]
        int m_circleCount = 2;
        [SerializeField]
        int m_firstSample = 4;
        [SerializeField]
        int m_sampleIncrement = 2;

        [SerializeField]
        Vector2 offset = Vector2.zero;

        [SerializeField]
        bool constantPixelSize = false;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            circleCount = m_circleCount;
            firstSample = m_firstSample;
            sampleIncrement = m_sampleIncrement;
        }
#endif

        public int circleCount
        {
            get
            {
                return m_circleCount;
            }

            set
            {
                m_circleCount = Mathf.Max(value, 1);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public int firstSample
        {
            get
            {
                return m_firstSample;
            }

            set
            {
                m_firstSample = Mathf.Max(value, 2);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public int sampleIncrement
        {
            get
            {
                return m_sampleIncrement;
            }

            set
            {
                m_sampleIncrement = Mathf.Max(value, 1);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        override protected void ModifyMesh(List<UIVertex> verts)
        {
            var original = verts.Count;
            var count = 0;
            var sampleCount = m_firstSample;
            var dx = effectDistance.x / circleCount;
            var dy = effectDistance.y / circleCount;
            for (int i = 1; i <= m_circleCount; i++)
            {
                var rx = dx * i;
                var ry = dy * i;
                var radStep = 2 * Mathf.PI / sampleCount;
                var rad = (i % 2) * radStep * 0.5f;
                for (int j = 0; j < sampleCount; j++)
                {
                    var next = count + original;

                    var p = rx * Mathf.Cos(rad) + offset.x;
                    var q = ry * Mathf.Sin(rad) + offset.y;

                    if (constantPixelSize)
                    {
                        var scale = transform.lossyScale;
                        p /= scale.x;
                        q /= scale.y;
                    }

                    ApplyShadow(verts, effectColor, count, next,
                        p, q);
                    count = next;
                    rad += radStep;
                }
                sampleCount += m_sampleIncrement;
            }
        }
    }
}
