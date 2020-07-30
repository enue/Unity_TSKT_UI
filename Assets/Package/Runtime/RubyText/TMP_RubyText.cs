using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if TSKT_UI_SUPPORT_TEXTMESHPRO

namespace TSKT
{
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class TMP_RubyText : TMP_BaseMeshEffect
    {
        public const int VertexCountPerQuad = 4;

        TMPro.TMP_Text text;
        TMPro.TMP_Text Text
        {
            get
            {
                return text ? text : (text = GetComponent<TMPro.TMP_Text>());
            }
        }

        [SerializeField]
        TMPro.TMP_Text bodyText = default;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        float boundsWidthDelta = 0f;

        StringWithRuby stringWithRuby;

        public void Set(in StringWithRuby stringWithRuby)
        {
            Text.text = stringWithRuby.joinedRubyText;
            this.stringWithRuby = stringWithRuby;
        }

        public override void Modify(TMP_VertexHelper vertexHelper)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            if (stringWithRuby.rubies == null
                || stringWithRuby.rubies.Length == 0)
            {
                return;
            }

            if (!TryGetBodyCharacterPositions(out var bodyCharacterPositions))
            {
                return;
            }

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
                    bodyBounds.xMin -= boundsWidthDelta;
                    bodyBounds.xMax += boundsWidthDelta;

                    var rubyIndex = ruby.textPosition + splitRubyLength * i;

                    ModifyRubyPosition(vertexHelper, rubyIndex, currentRubyLength, bodyBounds);
                }
            }
        }

        void ModifyRubyPosition(TMP_VertexHelper rubyVertices,
            int rubyIndex, int rubyLength,
            (float xMin, float xMax, float yMax) bodyBounds)
        {
            var vertexCount = rubyVertices.Vertices.Count;

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
                        minX = Mathf.Min(minX, rubyVertices.Vertices[index].x);
                        maxX = Mathf.Max(maxX, rubyVertices.Vertices[index].x);
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
                    quadAverageY += rubyVertices.Vertices[index].y;
                    quadRight = Mathf.Max(quadRight, rubyVertices.Vertices[index].x);
                    quadLeft = Mathf.Min(quadLeft, rubyVertices.Vertices[index].x);
                }
                quadAverageY /= VertexCountPerQuad;
                var fromPosition = new Vector2(quadLeft, quadAverageY);
                var move = toPosition - fromPosition;

                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    var vertex = rubyVertices.Vertices[index];
                    vertex.x += move.x;
                    vertex.y += move.y;
                    rubyVertices.Vertices[index] = vertex;
                }

                position += (quadRight - quadLeft) * advance;
            }
        }

        bool TryGetBodyCharacterPositions(out (float left, float right, float y)[] result)
        {
            // FIXME : FontSizeをAutoにしているとTextInfoが取得できない（characterCountが0になる）ことがある
            var textInfo = bodyText.GetTextInfo(stringWithRuby.body);
            if (textInfo.characterCount == 0)
            {
                result = null;
                return false;
            }

            result = new (float left, float right, float y)[stringWithRuby.body.Length];
            for (int i = 0; i < textInfo.characterCount; ++i)
            {
                var it = textInfo.characterInfo[i];
                if (it.isVisible)
                {
                    result[it.index] = (
                        it.topLeft.x,
                        it.topRight.x,
                        it.topLeft.y);
                }
            }
            return true;
        }

        bool TryGetBodyCharacterHasQuadList(out bool[] result)
        {
            // FIXME : FontSizeをAutoにしているとTextInfoが取得できない（characterCountが0になる）ことがある
            var textInfo = bodyText.GetTextInfo(stringWithRuby.body);
            if (textInfo.characterCount == 0)
            {
                result = null;
                return false;
            }

            result = new bool[stringWithRuby.body.Length];
            for (int i = 0; i < textInfo.characterCount; ++i)
            {
                var it = textInfo.characterInfo[i];
                result[it.index] = it.isVisible;
            }
            return true;
        }

        public bool TryGetBodyQuadCountRubyQuadCountMap(out int[] result)
        {
            if (!TryGetBodyCharacterHasQuadList(out var list))
            {
                result = null;
                return false;
            }
            result = stringWithRuby.GetBodyQuadCountRubyQuadCountMap(list);
            return true;
        }

        public bool TryGetBodyCharacterCountBodyQuadCountMap(out int[] result)
        {
            if (!TryGetBodyCharacterHasQuadList(out var list))
            {
                result = null;
                return false;
            }

            result = new int[list.Length + 1];
            var quadCount = 0;
            for (int i = 0; i < list.Length; ++i)
            {
                if (list[i])
                {
                    ++quadCount;
                }

                result[i + 1] = quadCount;
            }

            return true;
        }
    }
}
#endif
