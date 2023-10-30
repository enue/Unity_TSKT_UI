using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class TMP_RubyText : TMP_BaseMeshEffect
    {
        public const int VertexCountPerQuad = 4;

        TMPro.TMP_Text? text;
        public TMPro.TMP_Text Text => text ? text! : (text = GetComponent<TMPro.TMP_Text>());

        [SerializeField]
        TMPro.TMP_Text? bodyText = default;

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
                Text.text = null;
                return;
            }

            if (!TryGetBodySourceTextPositions(out var bodySourceTextPositions))
            {
                Text.text = null;
                return;
            }

            foreach (var ruby in stringWithRuby.rubies)
            {
                // 幅がない文字はおそらく制御文字やタグなので撥ねる
                Span<(float left, float right, float y)> bodyCharactersForRuby = stackalloc (float left, float right, float y)[ruby.bodyStringRange.length];
                {
                    int index = 0;
                    foreach (var it in bodySourceTextPositions.AsSpan(ruby.bodyStringRange.start, ruby.bodyStringRange.length))
                    {
                        if (it.left != it.right)
                        {
                            bodyCharactersForRuby[index] = it;
                            ++index;
                        }
                    }
                    bodyCharactersForRuby = bodyCharactersForRuby[..index];
                }

                if (bodyCharactersForRuby.Length == 0)
                {
                    continue;
                }

                // 改行を挟む場合はルビを分割
                var newLine = new List<int>() { 0 };
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

                    var bodyBounds = RubyText.GetBounds(bodyCharactersForRuby.Slice(characterIndex, characterCount));
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
            var characterCount = rubyVertices.CharacterCount;

            float rubyBoundsLeft;
            float advance;
            {
                float rubyBoundsRight;
                var rubyWidth = 0f;
                var lastQuadWidth = 0f;
                for (int i = 0; i < rubyLength; ++i)
                {
                    {
                        var requireCount = (rubyIndex + i + 1);
                        if (requireCount > characterCount)
                        {
                            continue;
                        }
                    }

                    var quadWidth = rubyVertices.GetCharacterBounds(i + rubyIndex).size.x;
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
                    var requireCount = (rubyIndex + i + 1);
                    if (requireCount > rubyVertices.CharacterCount)
                    {
                        break;
                    }
                }

                var toPosition = new Vector2(position, bodyBounds.yMax + positionY);

                var bounds = rubyVertices.GetCharacterBounds(rubyIndex + i);
                var fromPosition = new Vector2(bounds.min.x, bounds.center.y);
                var move = toPosition - fromPosition;


                var vertices = rubyVertices.GetVertex(rubyIndex + i);
                for (int j = 0; j < vertices.Length; ++j)
                {
                    vertices[j].x += move.x;
                    vertices[j].y += move.y;
                }

                position += bounds.size.x * advance;
            }
        }

        bool TryGetBodySourceTextPositions(out (float left, float right, float y)[]? result)
        {
            // FIXME : FontSizeをAutoにしているとTextInfoが取得できない（characterCountが0になる）ことがある
            var textInfo = bodyText!.GetTextInfo(stringWithRuby.body);
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
    }
}
