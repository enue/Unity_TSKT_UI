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
        Text bodyText;

        [SerializeField]
        float positionY = 8f;

        [SerializeField]
        bool forceRefresh;

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
            if (bodyText.text == null)
            {
                return;
            }

            var bodyCharacterPositions = new List<(float left, float right, float y)>();
            {
                var settings = bodyText.GetGenerationSettings(bodyText.rectTransform.rect.size);

                using (var generator = new TextGenerator())
                {
                    generator.PopulateWithErrors(bodyText.text, settings, gameObject);

                    if (charInfoBuffer == null)
                    {
                        charInfoBuffer = new List<UICharInfo>();
                    }
                    charInfoBuffer.Clear();

                    var characters = charInfoBuffer;
                    generator.GetCharacters(characters);
                    var scale = 1f / bodyText.pixelsPerUnit;
                    for (int i = 0; i < characters.Count; ++i)
                    {
                        var character = characters[i];
                        var left = character.cursorPos.x * scale;
                        var right = (character.cursorPos.x + character.charWidth) * scale;
                        var y = character.cursorPos.y * scale;
                        bodyCharacterPositions.Add((left, right, y));
                    }
                }
            }

            foreach (var ruby in stringWithRuby.rubies)
            {
                // 改行を挟む場合はルビを分割
                var newLine = new List<int>();
                newLine.Add(0);
                for (int i = 1; i < ruby.bodyStringRange.length; ++i)
                {
                    var index = (ruby.bodyStringRange.start + i);
                    var prevIndex = (ruby.bodyStringRange.start + i - 1);
                    if (bodyCharacterPositions.Count > index && bodyCharacterPositions.Count > prevIndex)
                    {
                        if (bodyCharacterPositions[index].left < bodyCharacterPositions[prevIndex].left)
                        {
                            newLine.Add(i);
                        }
                    }
                }
                var splitRubyLength = ruby.textLength / newLine.Count;
                for (int i = 0; i < newLine.Count; ++i)
                {
                    var targetCharacterIndex = ruby.bodyStringRange.start + newLine[i];
                    var targetCharacterCount = (i == newLine.Count - 1)
                        ? (ruby.bodyStringRange.length - newLine[i])
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
                    var currentBodyPositions = bodyCharacterPositions
                            .Skip(targetCharacterIndex)
                            .Take(targetCharacterCount)
                            .ToArray();

                    ModifyRubyPosition(ref list, currentBodyPositions, rubyIndex, currentRubyLength);
                }
            }
        }

        void ModifyRubyPosition(ref List<UIVertex> targets,
            (float left, float right, float y)[] bodyCharacterPositions,
            int rubyIndex, int rubyLength)
        {
            var bodyXMax = bodyCharacterPositions[0].right;
            var bodyXMin = bodyCharacterPositions[0].left;
            var bodyYMax = bodyCharacterPositions[0].y;
            for(int i=1; i<bodyCharacterPositions.Length; ++i)
            {
                bodyXMax = Mathf.Max(bodyXMax, bodyCharacterPositions[i].right);
                bodyXMin = Mathf.Min(bodyXMin, bodyCharacterPositions[i].left);
                bodyYMax = Mathf.Max(bodyYMax, bodyCharacterPositions[i].y);
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
