using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TSKT
{
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class TMP_RubyText : MonoBehaviour
    {
        public const int VertexCountPerQuad = 4;

        TMPro.TMP_Text text;
        TMPro.TMP_Text Text
        {
            get
            {
                return text ?? (text = GetComponent<TMPro.TMP_Text>());
            }
        }

        [SerializeField]
        TMPro.TMP_Text bodyText = default;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        bool forceRefresh = default;

        StringWithRuby stringWithRuby;

        List<Vector3> vertexBuffer;

        public void Set(StringWithRuby stringWithRuby)
        {
            Text.text = stringWithRuby.joinedRubyText;
            this.stringWithRuby = stringWithRuby;

            ModifyMesh();
        }

        void Update()
        {
            if (forceRefresh)
            {
                ModifyMesh();
            }
        }

        void ModifyMesh()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            if (stringWithRuby.rubies.Length == 0)
            {
                return;
            }

            var bodyCharacterPositions = new (float left, float right, float y)[stringWithRuby.body.Length];
            {
                // FIXME : FontSizeをAutoにしているとTextInfoが取得できない（characterCountが0になる）ことがある
                var textInfo = bodyText.GetTextInfo(stringWithRuby.body);
                if (textInfo.characterCount == 0)
                {
                    return;
                }
                for (int i = 0; i < textInfo.characterCount; ++i)
                {
                    var it = textInfo.characterInfo[i];
                    bodyCharacterPositions[it.index] = (
                        it.topLeft.x,
                        it.topRight.x,
                        it.topLeft.y);
                }
            }

            Text.ForceMeshUpdate();
            var mesh = Text.mesh;
            if (vertexBuffer == null)
            {
                vertexBuffer = new List<Vector3>();
            }
            mesh.GetVertices(vertexBuffer);

            foreach (var ruby in stringWithRuby.rubies)
            {
                // 幅がない文字はおそらく制御文字やタグなので撥ねる
                var bodyCharactersForRuby = bodyCharacterPositions
                    .Skip(ruby.bodyStringRange.start)
                    .Take(ruby.bodyStringRange.length)
                    .Where(_ => _.left != _.right)
                    .ToArray();

                if (bodyCharactersForRuby.Length == 0)
                {
                    continue;
                }

                // 改行を挟む場合はルビを分割
                var newLine = new List<int>();
                newLine.Add(0);
                for (int i = 1; i < bodyCharactersForRuby.Length; ++i)
                {
                    var index = i;
                    var prevIndex = i - 1;
                    if (bodyCharactersForRuby[index].left < bodyCharactersForRuby[prevIndex].left)
                    {
                        newLine.Add(i);
                    }
                }
                var splitRubyLength = ruby.textLength / newLine.Count;
                for (int i = 0; i < newLine.Count; ++i)
                {
                    var characterIndex = newLine[i];
                    var characterCount = (i == newLine.Count - 1)
                        ? (bodyCharactersForRuby.Length - newLine[i])
                        : newLine[i + 1] - newLine[i];

                    if (bodyCharactersForRuby.Length < (characterIndex + characterCount))
                    {
                        continue;
                    }

                    int currentRubyLength;
                    if (i == newLine.Count - 1)
                    {
                        currentRubyLength = splitRubyLength + ruby.textLength % newLine.Count;
                    }
                    else
                    {
                        currentRubyLength = splitRubyLength;
                    }

                    var bodyBounds = RubyText.GetBounds(bodyCharactersForRuby, characterIndex, characterCount);

                    var rubyIndex = ruby.textPosition + splitRubyLength * i;

                    ModifyRubyPosition(vertexBuffer, rubyIndex, currentRubyLength, bodyBounds);
                }
            }

            mesh.SetVertices(vertexBuffer);

            Text.UpdateGeometry(mesh, 0);
        }

        void ModifyRubyPosition(List<Vector3> rubyVertices,
            int rubyIndex, int rubyLength,
            (float xMin, float xMax, float yMax) bodyBounds)
        {
            var vertexCount = rubyVertices.Count;

            float rubyBoundsLeft;
            float rubyBoundsRight;
            float advance;
            {
                var rubyWidth = 0f;
                var lastQuadWidth = 0f;
                for (int i = 0; i < rubyLength; ++i)
                {
                    {
                        var requireCount = (rubyIndex + i + 1) * VertexCountPerQuad;
                        if (requireCount > vertexCount)
                        {
                            continue;
                        }
                    }

                    var minX = float.MaxValue;
                    var maxX = float.MinValue;
                    for (int j = 0; j < VertexCountPerQuad; ++j)
                    {
                        var index = j + (i + rubyIndex) * VertexCountPerQuad;
                        minX = Mathf.Min(minX, rubyVertices[index].x);
                        maxX = Mathf.Max(maxX, rubyVertices[index].x);
                    }
                    var quadWidth = maxX - minX;
                    rubyWidth += quadWidth;

                    if (i == rubyLength - 1)
                    {
                        lastQuadWidth = quadWidth;
                    }
                }
                if (rubyLength == 1)
                {
                    var center = (bodyBounds.xMax + bodyBounds.xMin) / 2f;
                    rubyBoundsLeft = center - rubyWidth / 2f;
                    rubyBoundsRight = center + rubyWidth / 2f;
                }
                else if (rubyWidth > (bodyBounds.xMax - bodyBounds.xMin))
                {
                    var center = (bodyBounds.xMax + bodyBounds.xMin) / 2f;
                    rubyBoundsLeft = center - rubyWidth / 2f;
                    rubyBoundsRight = center + rubyWidth / 2f;
                }
                else
                {
                    rubyBoundsLeft = bodyBounds.xMin;
                    rubyBoundsRight = bodyBounds.xMax;
                }

                if (rubyWidth - lastQuadWidth != 0f)
                {
                    advance = (rubyBoundsRight - rubyBoundsLeft - lastQuadWidth) / (rubyWidth - lastQuadWidth);
                }
                else
                {
                    advance = 0f;
                }
            }

            var position = rubyBoundsLeft;
            for (int i = 0; i < rubyLength; ++i)
            {
                {
                    var requireCount = (rubyIndex + i + 1) * VertexCountPerQuad;
                    if (requireCount > vertexCount)
                    {
                        break;
                    }
                }

                var toPosition = new Vector2(position, bodyBounds.yMax + positionY);

                var quadAverageY = 0f;
                var quadLeft = float.MaxValue;
                var quadRight = float.MinValue;
                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    quadAverageY += rubyVertices[index].y;
                    quadRight = Mathf.Max(quadRight, rubyVertices[index].x);
                    quadLeft = Mathf.Min(quadLeft, rubyVertices[index].x);
                }
                quadAverageY /= VertexCountPerQuad;
                var fromPosition = new Vector2(quadLeft, quadAverageY);
                var move = toPosition - fromPosition;

                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    var vertex = rubyVertices[index];
                    vertex.x += move.x;
                    vertex.y += move.y;
                    rubyVertices[index] = vertex;
                }

                position += (quadRight - quadLeft) * advance;
            }
        }
    }
}
