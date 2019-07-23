using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace TSKT
{
    [RequireComponent(typeof(TMP_Text))]
    public abstract class TMP_QuadByQuad : MonoBehaviour
    {
        const int VertexCountPerQuad = 4;

        public float delayPerQuad = 0.1f;
        public float durationPerQuad = 0.4f;
        public bool rightToLeft = false;
        public bool autoPlay = false;

        [SerializeField]
        float elapsedTime = 0f;
        public float ElapsedTime
        {
            get => elapsedTime;
            set
            {
                elapsedTime = value;
                Refresh();
            }
        }

        TMP_Text text;
        TMP_Text Text => text ?? (text = GetComponent<TMP_Text>());

        float? startedTime;

        List<Color> colors = new List<Color>();
        List<Vector3> vertices = new List<Vector3>();

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

        protected abstract void ModifyQuad(ref List<Vector3> vertices, ref List<Color> colors, int startIndex, int count, float normalizedTime);

        public float GetDuration(int quadCount)
        {
            return QuadByQuad.GetDuration(quadCount, delayPerQuad, durationPerQuad);
        }

        void Refresh()
        {
            Text.ForceMeshUpdate();
            var mesh = Text.mesh;
            mesh.GetVertices(vertices);
            mesh.GetColors(colors);

            var quadCount = vertices.Count / VertexCountPerQuad;
            if (ElapsedTime > GetDuration(quadCount))
            {
                return;
            }

            for (int i = 0; i < quadCount; ++i)
            {
                var normalizedTime = QuadByQuad.GetNormalizedTime(quadIndex: i,
                    quadCount: quadCount,
                    delayPerQuad: delayPerQuad,
                    elapsedTime: elapsedTime,
                    durationPerQuad: durationPerQuad,
                    rightToLeft: rightToLeft);
                if (normalizedTime < 1f)
                {
                    ModifyQuad(ref vertices, ref colors, i * VertexCountPerQuad, VertexCountPerQuad, normalizedTime);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);

            Text.UpdateGeometry(mesh, 0);
        }
    }
}
