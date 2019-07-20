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
        public const int VertexCountPerQuad = 4;

        Text text;
        Text Text
        {
            get
            {
                return text ?? (text = GetComponent<Text>());
            }
        }

        [SerializeField]
        Text bodyText = default;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        bool forceRefresh = default;

        StringWithRuby stringWithRuby;

        List<UICharInfo> charInfoBuffer;

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
            if (stringWithRuby.rubies == null)
            {
                return;
            }

            (float left, float right, float y)[] bodyCharacterPositions;
            {
                var settings = bodyText.GetGenerationSettings(bodyText.rectTransform.rect.size);

                using (var generator = new TextGenerator())
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
                    foreach(var character in characters)
                    {
                        var left = character.cursorPos.x * scale;
                        var right = (character.cursorPos.x + character.charWidth) * scale;
                        var y = character.cursorPos.y * scale;
                        builder.Add((left, right, y));
                    }
                    bodyCharacterPositions = builder.Array;
                }
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

                    var bodyBounds = GetBounds(bodyCharactersForRuby, characterIndex, characterCount);

                    var rubyIndex = ruby.textPosition + splitRubyLength * i;

                    ModifyRubyPosition(ref vh, rubyIndex, currentRubyLength, bodyBounds);
                }
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
            for (int i = 0; i < rubyLength; ++i)
            {
                {
                    var requireCount = (rubyIndex + i + 1) * VertexCountPerQuad;
                    if (requireCount > vertexCount)
                    {
                        break;
                    }
                }

                var t = Mathf.InverseLerp(
                    -0.4f,
                    rubyLength - 1f + 0.4f,
                    i);
                var toPosition = new Vector2(
                    Mathf.Lerp(bodyBounds.xMin, bodyBounds.xMax, t),
                    bodyBounds.yMax + positionY);

                var average = Vector2.zero;
                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    rubyVertices.PopulateUIVertex(ref vertex, index);
                    average += new Vector2(vertex.position.x, vertex.position.y);
                }
                average /= VertexCountPerQuad;

                var fromPosition = average;
                var move = toPosition - fromPosition;

                for (int j = 0; j < VertexCountPerQuad; ++j)
                {
                    var index = (rubyIndex + i) * VertexCountPerQuad + j;
                    rubyVertices.PopulateUIVertex(ref vertex, index);
                    vertex.position.x += move.x;
                    vertex.position.y += move.y;
                    rubyVertices.SetUIVertex(vertex, index);
                }
            }
        }
    }
}
