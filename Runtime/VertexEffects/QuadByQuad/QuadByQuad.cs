using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TSKT
{
    [RequireComponent(typeof(Text))]
    public abstract class QuadByQuad : BaseMeshEffect
    {
        const int VertexCountPerQuad = 4;
        public float delayPerQuad = 0.1f;
        public float durationPerQuad = 0.4f;
        public bool rightToLeft = false;
        public bool autoPlay = false;
        public bool loop = false;

        [SerializeField]
        float elapsedTime = 0f;
        public float ElapsedTime
        {
            get => elapsedTime;
            set
            {
                elapsedTime = value;
                Text.SetAllDirty();
            }
        }

        Text text;
        Text Text => text ?? (text = GetComponent<Text>());

        float? startedTime;

        void Update()
        {
            if (autoPlay && Application.isPlaying)
            {
                if (!startedTime.HasValue)
                {
                    startedTime = Time.time;
                }
                ElapsedTime = Time.time - startedTime.Value;
            }
        }

        protected abstract void ModifyQuad(VertexHelper vh, int startIndex, int count, float normalizedTime);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            var quadCount = vh.currentVertCount / VertexCountPerQuad;

            var time = elapsedTime;
            if (loop)
            {
                time %= GetDuration(quadCount);
            }

            for (int i = 0; i < quadCount; ++i)
            {
                var normalizedTime = GetNormalizedTime(quadIndex: i,
                    quadCount: quadCount,
                    delayPerQuad: delayPerQuad,
                    elapsedTime: time,
                    durationPerQuad: durationPerQuad,
                    rightToLeft: rightToLeft);
                ModifyQuad(vh, i * VertexCountPerQuad, VertexCountPerQuad, normalizedTime);
            }
        }

        public static float GetDuration(int quadCount, float delayPerQuad, float durationPerQuad)
        {
            return durationPerQuad + (quadCount - 1) * delayPerQuad;
        }

        public float GetDuration(int quadCount)
        {
            return GetDuration(quadCount, delayPerQuad, durationPerQuad);
        }

        public static float GetNormalizedTime(
            int quadIndex,
            int quadCount,
            float delayPerQuad,
            float elapsedTime,
            float durationPerQuad,
            bool rightToLeft)
        {
            float timeOffset;
            if (rightToLeft)
            {
                timeOffset = -delayPerQuad * (quadCount - 1 - quadIndex);
            }
            else
            {
                timeOffset = -delayPerQuad * quadIndex;
            }
            var quadTime = elapsedTime + timeOffset;
            var normalizedTime = quadTime / durationPerQuad;
            return normalizedTime;
        }
    }
}
