﻿using System;
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
        public const int VertexCountPerQuad = 4;

        Text text;
        Text Text
        {
            get
            {
                return text ? text : (text = GetComponent<Text>());
            }
        }

        [SerializeField]
        Text bodyText = default;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        float boundsWidthDelta = 0f;

        [SerializeField]
        bool forceRefresh = default;

        StringWithRuby stringWithRuby;

        List<UICharInfo> charInfoBuffer;

        public void Set(in StringWithRuby stringWithRuby)
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
            if (stringWithRuby.rubies == null)
            {
                return;
            }

            var bodyCharacterPositions = GetBodyCharacterPositions();
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

                    var bodyBounds = GetBounds(bodyCharactersForRuby, characterIndex, characterCount);
                    bodyBounds.xMin -= boundsWidthDelta;
                    bodyBounds.xMax += boundsWidthDelta;

                    var rubyIndex = ruby.textPosition + splitRubyLength * i;

                    ModifyRubyPosition(ref vh, rubyIndex, currentRubyLength, bodyBounds);
                }
            }
        }

        (float left, float right, float y)[] GetBodyCharacterPositions()
        {
            var settings = bodyText.GetGenerationSettings(bodyText.rectTransform.rect.size);

            using (var generator = new TextGenerator(stringWithRuby.body.Length))
            {
                generator.PopulateWithErrors(stringWithRuby.body, settings, gameObject);

                if (charInfoBuffer == null)
                {
                    charInfoBuffer = new List<UICharInfo>();
                }
                charInfoBuffer.Clear();

                var characters = charInfoBuffer;
                generator.GetCharacters(characters);

                var builder = new ArrayBuilder<(float left, float right, float y)>(characters.Count);
                var scale = 1f / bodyText.pixelsPerUnit;
                foreach (var character in characters)
                {
                    var left = character.cursorPos.x * scale;
                    var right = (character.cursorPos.x + character.charWidth) * scale;
                    var y = character.cursorPos.y * scale;
                    builder.Add((left, right, y));
                }
                return builder.Array;
            }
        }

        bool[] GetBodyCharacterHasQuadList()
        {
            var settings = bodyText.GetGenerationSettings(bodyText.rectTransform.rect.size);

            using (var generator = new TextGenerator(stringWithRuby.body.Length))
            {
                generator.PopulateWithErrors(stringWithRuby.body, settings, gameObject);

                if (charInfoBuffer == null)
                {
                    charInfoBuffer = new List<UICharInfo>();
                }
                charInfoBuffer.Clear();

                var characters = charInfoBuffer;
                generator.GetCharacters(characters);

                var builder = new ArrayBuilder<bool>(characters.Count);
                foreach (var character in characters)
                {
                    builder.Add(character.charWidth != 0f);
                }
                return builder.Array;
            }
        }

        public static (float xMin, float xMax, float yMax) GetBounds((float left, float right, float y)[] characters,
            int startIndex, int count)
        {
            var xMax = characters[startIndex].right;
            var xMin = characters[startIndex].left;
            var yMax = characters[startIndex].y;
            for (int i = 1; i < count; ++i)
            {
                var index = startIndex + i;
                xMax = Mathf.Max(xMax, characters[index].right);
                xMin = Mathf.Min(xMin, characters[index].left);
                yMax = Mathf.Max(yMax, characters[index].y);
            }
            return (xMin, xMax, yMax);
        }

        void ModifyRubyPosition(ref VertexHelper rubyVertices,
            int rubyIndex, int rubyLength,
            (float xMin, float xMax, float yMax) bodyBounds)
        {
            UIVertex vertex = default;
            var vertexCount = rubyVertices.currentVertCount;

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
                        rubyVertices.PopulateUIVertex(ref vertex, j + (i + rubyIndex) * VertexCountPerQuad);
                        minX = Mathf.Min(minX, vertex.position.x);
                        maxX = Mathf.Max(maxX, vertex.position.x);
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
                    rubyVertices.PopulateUIVertex(ref vertex, index);
                    quadAverageY += vertex.position.y;
                    quadRight = Mathf.Max(quadRight, vertex.position.x);
                    quadLeft = Mathf.Min(quadLeft, vertex.position.x);
                }
                quadAverageY /= VertexCountPerQuad;
                var fromPosition = new Vector2(quadLeft, quadAverageY);
                var move = toPosition - fromPosition;

                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    rubyVertices.PopulateUIVertex(ref vertex, index);
                    vertex.position.x += move.x;
                    vertex.position.y += move.y;
                    rubyVertices.SetUIVertex(vertex, index);
                }

                position += (quadRight - quadLeft) * advance;
            }
        }

        public int[] GetBodyQuadCountRubyQuadCountMap()
        {
            var characterHasQuadList = GetBodyCharacterHasQuadList();
            return stringWithRuby.GetBodyQuadCountRubyQuadCountMap(characterHasQuadList);
        }
    }
}
