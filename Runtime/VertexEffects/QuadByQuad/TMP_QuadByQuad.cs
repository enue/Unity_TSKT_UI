using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace TSKT
{
    public abstract class TMP_QuadByQuad : TMP_BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;

        public float delayPerQuad = 0.1f;
        public float durationPerQuad = 0.4f;
        public bool rightToLeft = false;

        public bool autoPlay = false;
        public bool loop = false;

        public float elapsedTime = 0f;

        float? startedTime;

        void Update()
        {
            if (autoPlay && Application.isPlaying)
            {
                if (!startedTime.HasValue)
                {
                    startedTime = Time.time;
                }
                elapsedTime = Time.time - startedTime.Value;
            }
        }

        public float GetDuration(int quadCount)
        {
            return QuadByQuad.GetDuration(quadCount, delayPerQuad, durationPerQuad);
        }

        public override void Modify(ref List<Vector3> vertices, ref List<Color> colors)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            var quadCount = vertices.Count / VertexCountPerQuad;

            var time = elapsedTime;
            if (loop)
            {
                time %= GetDuration(quadCount);
            }

            for (int quadIndex = 0; quadIndex < quadCount; ++quadIndex)
            {
                var normalizedTime = QuadByQuad.GetNormalizedTime(
                    quadIndex: quadIndex,
                    quadCount: quadCount,
                    delayPerQuad: delayPerQuad,
                    elapsedTime: time,
                    durationPerQuad: durationPerQuad,
                    rightToLeft: rightToLeft);

                ModifyQuad(ref vertices, ref colors, quadIndex * VertexCountPerQuad, VertexCountPerQuad, normalizedTime);
            }
        }

        protected abstract void ModifyQuad(ref List<Vector3> vertices, ref List<Color> colors, int startIndex, int count, float normalizedTime);
    }
}
