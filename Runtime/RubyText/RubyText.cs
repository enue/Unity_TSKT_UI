using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TSKT
{
    [RequireComponent(typeof(Text))]
    public class RubyText : BaseMeshEffect
    {
        public const int VertexCountPerQuad = 6;

        Text text;
        Text Text
        {
            get
            {
                return text ?? (text = GetComponent<Text>());
            }
        }

        [SerializeField]
        RubyBodyText bodyText;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        bool forceRefresh;

        StringWithRuby stringWithRuby;

        public void Set(StringWithRuby stringWithRuby)
        {
            Text.text = stringWithRuby.joinedRubyText;
            this.stringWithRuby = stringWithRuby;
        }

        void Update()
        {
            if (forceRefresh)
            {
                Text.SetAllDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            var vertices = UIVerticesPool.Get();

            vh.GetUIVertexStream(vertices);
            ModifyMesh(vertices);
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);

            UIVerticesPool.Release(vertices);
        }

        void ModifyMesh(List<UIVertex> list)
        {
            if (stringWithRuby.rubies == null)
            {
                return;
            }
            if (bodyText.OriginalTextVertices == null)
            {
                return;
            }

            foreach (var ruby in stringWithRuby.rubies)
            {
                // 文字送り途中などでベースの文字が表示されていない場合がある。その場合ルビは非表示にする。
                if (ruby.RequiredTargetVerticesCount > bodyText.OriginalTextVertices.Count)
                {
                    for (int j = 0; j < ruby.textLength; ++j)
                    {
                        for (int k = 0; k < VertexCountPerQuad; ++k)
                        {
                            var index = (ruby.textPosition + j) * VertexCountPerQuad + k;
                            if (index < list.Count)
                            {
                                var vertex = list[index];
                                vertex.color = new Color32(vertex.color.r, vertex.color.g, vertex.color.b, 0);
                                list[index] = vertex;
                            }
                        }
                    }
                    continue;
                }
                // 表示処理
                for (int j = 0; j < ruby.textLength; ++j)
                {
                    for (int k = 0; k < VertexCountPerQuad; ++k)
                    {
                        var index = (ruby.textPosition + j) * VertexCountPerQuad + k;
                        if (index < list.Count)
                        {
                            var vertex = list[index];
                            vertex.color = new Color32(vertex.color.r, vertex.color.g, vertex.color.b, byte.MaxValue);
                            list[index] = vertex;
                        }
                    }
                }

                // 改行を挟む場合はルビを分割
                var newLine = new List<int>();
                newLine.Add(0);
                for (int i = 1; i < ruby.bodyQuadCount; ++i)
                {
                    var index = (ruby.bodyQuadIndex + i) * VertexCountPerQuad;
                    var prevIndex = (ruby.bodyQuadIndex + i - 1) * VertexCountPerQuad;
                    if (bodyText.OriginalTextVertices.Count > index && bodyText.OriginalTextVertices.Count > prevIndex)
                    {
                        if (bodyText.OriginalTextVertices[index].position.x < bodyText.OriginalTextVertices[prevIndex].position.x)
                        {
                            newLine.Add(i);
                        }
                    }
                }
                var splitRubyLength = ruby.textLength / newLine.Count;
                for (int i = 0; i < newLine.Count; ++i)
                {
                    var targetQuadIndex = ruby.bodyQuadIndex + newLine[i];
                    var targetQuadCount = (i == newLine.Count - 1)
                        ? (ruby.bodyQuadCount - newLine[i])
                        : newLine[i + 1] - newLine[i];

                    int currentRubyLength;
                    if (i == newLine.Count - 1)
                    {
                        currentRubyLength = splitRubyLength + ruby.textLength % newLine.Count;
                    }
                    else
                    {
                        currentRubyLength = splitRubyLength;
                    }
                    var rubyIndex = ruby.textPosition + splitRubyLength * i;
                    var bodyVertices = bodyText.OriginalTextVertices
                            .Skip(targetQuadIndex * VertexCountPerQuad)
                            .Take(targetQuadCount * VertexCountPerQuad)
                            .ToArray();

                    ModifyRubyPosition(ref list, bodyVertices, rubyIndex, currentRubyLength);
                }
            }
        }

        void ModifyRubyPosition(ref List<UIVertex> targets, UIVertex[] bodyVertices, int rubyIndex, int rubyLength)
        {
            var bodyXMax = bodyVertices[0].position.x;
            var bodyXMin = bodyVertices[0].position.x;
            var bodyYMax = bodyVertices[0].position.y;
            for(int i=1; i<bodyVertices.Length; ++i)
            {
                bodyXMax = Mathf.Max(bodyXMax, bodyVertices[i].position.x);
                bodyXMin = Mathf.Min(bodyXMin, bodyVertices[i].position.x);
                bodyYMax = Mathf.Max(bodyYMax, bodyVertices[i].position.y);
            }

            for (int i = 0; i < rubyLength; ++i)
            {
                var t = Mathf.InverseLerp(
                    -0.4f,
                    rubyLength - 1f + 0.4f,
                    i);
                var toPosition = new Vector2(
                    Mathf.Lerp(bodyXMin, bodyXMax, t),
                    bodyYMax + positionY);

                var quadVertices = targets
                    .Skip((rubyIndex + i) * VertexCountPerQuad)
                    .Take(VertexCountPerQuad);

                if (!quadVertices.Any())
                {
                    continue;
                }

                var fromPosition = new Vector2(
                    quadVertices.Average(_ => _.position.x),
                    quadVertices.Average(_ => _.position.y));

                var move = toPosition - fromPosition;

                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    var v = targets[index];
                    v.position.x += move.x;
                    v.position.y += move.y;
                    targets[index] = v;
                }
            }
        }
    }
}
