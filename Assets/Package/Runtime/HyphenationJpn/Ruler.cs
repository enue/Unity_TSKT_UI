using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT.HyphenationJpns
{
    public class Ruler
    {
        public static class DefaultHyphenation
        {
            // 禁則処理 http://ja.wikipedia.org/wiki/%E7%A6%81%E5%89%87%E5%87%A6%E7%90%86
            // 行頭禁則文字
            public const string CharactersThatCanNotBeginLine =
                (",)]｝、。）〕〉》」』】〙〗〟’”｠»" +// 終わり括弧類 簡易版
                 "ァィゥェォッャュョヮヵヶっぁぃぅぇぉっゃゅょゎ" +//行頭禁則和字 
                 "‐゠–〜ー" +//ハイフン類
                 "?!！？‼⁇⁈⁉" +//区切り約物
                 "・:;" +//中点類
                 "。.");//句点類

            public const string CharactersThatCanNotEndLine =
                 "(（[｛〔〈《「『【〘〖〝‘“｟«";//始め括弧類

            public const string UnbreakabkeSequenceCharacters =
                ("abcdefghijklmnopqrstuvwxyz" +
                 "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                 "0123456789" +
                 "<>=/().,#");
        }

        readonly Dictionary<char, float> characterWidthCache = new Dictionary<char, float>();

        public string CharactersThatCanNotBeginLine { get; set; } = DefaultHyphenation.CharactersThatCanNotBeginLine;
        public string CharactersThatCanNotEndLine { get; set; } = DefaultHyphenation.CharactersThatCanNotEndLine;
        public string UnbreakabkeSequenceCharacters { get; set; } = DefaultHyphenation.UnbreakabkeSequenceCharacters;

        Font cachedFont;
        int cachedFontSize;
        FontStyle cachedFontStyle;

        public float GetTextWidth(string message, Font font, int fontSize, FontStyle fontStyle, bool richText, bool updateTexture = true)
        {
            if (richText)
            {
                message = RITCH_TEXT_REPLACE.Replace(message, string.Empty);
            }

            if (updateTexture)
            {
                font.RequestCharactersInTexture(message, fontSize, fontStyle);
            }

            var lineWidth = 0f;
            var maxWidth = 0f;
            foreach (var it in message)
            {
                if (it == '\n')
                {
                    maxWidth = Mathf.Max(maxWidth, lineWidth);
                    lineWidth = 0f;
                }
                else
                {
                    lineWidth += GetCharacterWidth(font, fontSize, fontStyle, it);
                }
            }

            return Mathf.Max(maxWidth, lineWidth);
        }

        float GetLastLineWidth(Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
        {
            if (supportRichText)
            {
                message = RITCH_TEXT_REPLACE.Replace(message, string.Empty);
            }
            float lineWidth = 0f;
            foreach (var character in message)
            {
                if (character == '\n')
                {
                    lineWidth = 0f;
                }
                else
                {
                    lineWidth += GetCharacterWidth(font, fontSize, fontStyle, character);
                }
            }
            return lineWidth;
        }

        float GetLastLineWidth(Font font, int fontSize, FontStyle fontStyle, StringBuilder message)
        {
            float lineWidth = 0f;
            for (int i = 0; i < message.Length; ++i)
            {
                var character = message[i];
                if (character == '\n')
                {
                    lineWidth = 0f;
                }
                else
                {
                    lineWidth += GetCharacterWidth(font, fontSize, fontStyle, character);
                }
            }
            return lineWidth;
        }

        float GetCharacterWidth(Font font, int fontSize, FontStyle fontStyle, char character)
        {
            if (cachedFont == font
                && cachedFontSize == fontSize
                && cachedFontStyle == fontStyle)
            {
                if (characterWidthCache.TryGetValue(character, out var result))
                {
                    return result;
                }
            }
            else
            {
                cachedFont = font;
                cachedFontSize = fontSize;
                cachedFontStyle = fontStyle;
                characterWidthCache.Clear();
            }
            var foundInfo = font.GetCharacterInfo(character, out var info, fontSize, fontStyle);
            if (foundInfo)
            {
                characterWidthCache.Add(character, info.advance);
            }
            Debug.Assert(foundInfo, "not found character info : " + character);

            return info.advance;
        }

        public string GetFormattedText(Text text, string message)
        {
            return GetFormattedText(text.rectTransform.rect.width, text.font, text.fontSize, text.fontStyle, message, text.supportRichText);
        }

        public string GetFormattedText(float rectWidth, Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
        {
            var newLinePositions = GetNewLinePositions(rectWidth, font, fontSize, fontStyle, message, supportRichText, null);

            if (newLinePositions.Count > 0)
            {
                var builder = new StringBuilder(message, message.Length + newLinePositions.Count);
                for (int i = 0; i < newLinePositions.Count; ++i)
                {
                    var position = newLinePositions[i] + i;
                    builder.Insert(position, "\n");
                }
                message = builder.ToString();
            }
            return message;
        }

        public List<int> GetNewLinePositions(float rectWidth,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            string message,
            bool supportRichText,
            RangeInt[] disallowRanges = null)
        {
            var result = new List<int>();

            if (string.IsNullOrEmpty(message))
            {
                return result;
            }

            font.RequestCharactersInTexture(message, fontSize, fontStyle);

            // work
            var currentPosition = 0;

            float lineWidth = 0f;
            foreach (var word in GetWordList(message, disallowRanges))
            {
                if (word[word.Length - 1] == '\n')
                {
                    lineWidth = 0f;
                    currentPosition += word.Length;
                }
                else if (word.Length == 1)
                {
                    float textWidth = GetCharacterWidth(font, fontSize, fontStyle, word[0]);
                    if (lineWidth != 0f && lineWidth + textWidth > rectWidth)
                    {
                        result.Add(currentPosition);
                        lineWidth = 0f;
                    }
                    lineWidth += textWidth;
                    currentPosition += word.Length;
                }
                else if (supportRichText)
                {
                    float textWidth = GetLastLineWidth(font, fontSize, fontStyle, word.ToString(), supportRichText);
                    if (word[0] == '\n')
                    {
                        lineWidth = 0f;
                    }
                    else if (lineWidth != 0f && lineWidth + textWidth > rectWidth)
                    {
                        result.Add(currentPosition);
                        lineWidth = 0f;
                    }
                    lineWidth += textWidth;
                    currentPosition += word.Length;
                }
                else
                {
                    var textWidth = GetLastLineWidth(font, fontSize, fontStyle, word);
                    if (lineWidth + textWidth <= rectWidth)
                    {
                        if (word[0] == '\n')
                        {
                            lineWidth = 0f;
                        }
                        lineWidth += textWidth;
                        currentPosition += word.Length;
                    }
                    else if (textWidth <= rectWidth)
                    {
                        if (word[0] == '\n')
                        {
                            lineWidth = 0f;
                        }
                        else if (lineWidth != 0f)
                        {
                            result.Add(currentPosition);
                            lineWidth = 0f;
                        }
                        lineWidth += textWidth;
                        currentPosition += word.Length;
                    }
                    else
                    {
                        // wordの横幅がrectの横幅を超える場合は禁則を無視して改行するしかない
                        for (int i = 0; i < word.Length; ++i)
                        {
                            var character = word[i];
                            if (character == '\n')
                            {
                                ++currentPosition;
                                lineWidth = 0f;
                            }
                            else
                            {
                                var characterWidth = GetCharacterWidth(font, fontSize, fontStyle, character);
                                if (lineWidth > 0f && lineWidth + characterWidth > rectWidth)
                                {
                                    result.Add(currentPosition);
                                    lineWidth = 0f;
                                }
                                ++currentPosition;
                                lineWidth += characterWidth;
                            }
                        }
                    }
                }
            }

            return result;
        }

        IEnumerable<StringBuilder> GetWordList(string tmpText, RangeInt[] unsplittableRanges)
        {
            var word = new StringBuilder();

            for (int characterIndex = 0; characterIndex < tmpText.Length;)
            {
                var firstIndex = characterIndex;
                var lastIndex = characterIndex;

                if (unsplittableRanges != null)
                {
                    foreach (var it in unsplittableRanges)
                    {
                        if (it.start == firstIndex)
                        {
                            lastIndex = Mathf.Min(it.end - 1, tmpText.Length - 1);
                            break;
                        }
                    }
                }

                var lastCharacter = tmpText[lastIndex];

                characterIndex = lastIndex + 1;

                if (firstIndex == lastIndex)
                {
                    // 改行コード単品は即処理
                    var firstCharacter = tmpText[firstIndex];
                    if ((firstCharacter == '\n') && word.Length == 0)
                    {
                        word.Append(firstCharacter);
                        yield return word;
                        word.Length = 0;
                        continue;
                    }

                    word.Append(firstCharacter);
                }
                else
                {
                    // unsplittableRanges指定がある場合はまとめて処理する
                    word.Append(tmpText, firstIndex, lastIndex - firstIndex + 1);
                }

                // 自分の後ろで改行していいかどうかの判定
                if (lastIndex < tmpText.Length - 1)
                {
                    var canEndLine = true;
                    var nextCharacter = tmpText[lastIndex + 1];

                    if (!CanBreakSequence(lastCharacter, nextCharacter))
                    {
                        // 分割禁止文字
                        canEndLine = false;
                    }
                    else if (!CanEndLine(lastCharacter))
                    {
                        // 末尾が行末禁則文字
                        canEndLine = false;
                    }
                    else if (!CanBeginLine(nextCharacter))
                    {
                        // 次の文字が行頭禁則文字
                        canEndLine = false;
                    }

                    if (canEndLine)
                    {
                        yield return word;
                        word.Length = 0;
                    }
                }
            }
            if (word.Length > 0)
            {
                yield return word;
            }
        }

        public static readonly Regex RITCH_TEXT_REPLACE = new Regex(
            "(\\<color=.*?\\>|</color>|" +
            "\\<size=.n\\>|</size>|" +
            "<b>|</b>|" +
            "<i>|</i>)");

        bool CanBeginLine(char str)
        {
            return CharactersThatCanNotBeginLine.IndexOf(str) < 0;
        }

        bool CanEndLine(char str)
        {
            return CharactersThatCanNotEndLine.IndexOf(str) < 0;
        }

        bool CanBreakSequence(char left, char right)
        {
            return (UnbreakabkeSequenceCharacters.IndexOf(left) < 0)
                || (UnbreakabkeSequenceCharacters.IndexOf(right) < 0);
        }
    }
}
