using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
#nullable enable

namespace TSKT
{
    public abstract class TMP_QuadByQuad : TMP_BaseMeshEffect
    {
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

        public override void Modify(TMP_VertexHelper vertexHelper)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var time = elapsedTime;
            if (loop)
            {
                time %= GetDuration(vertexHelper.CharacterCount);
            }

            for (int i = 0; i < vertexHelper.CharacterCount; ++i)
            {
                var normalizedTime = QuadByQuad.GetNormalizedTime(
                    quadIndex: i,
                    quadCount: vertexHelper.CharacterCount,
                    delayPerQuad: delayPerQuad,
                    elapsedTime: time,
                    durationPerQuad: durationPerQuad,
                    rightToLeft: rightToLeft);

                ModifyQuad(vertexHelper, i, 1, normalizedTime);
            }
        }

        protected abstract void ModifyQuad(TMP_VertexHelper vertexHelper, int startIndex, int count, float normalizedTime);
    }
}
