#nullable enable
using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace TSKT
{
    /// <summary>
    /// ルビを本文と同じTMP_Textにリッチテキストタグで埋め込んで表示する。
    /// 改行や自動サイズ調整はTextMeshProに任せられる。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TMP_RubyText : MonoBehaviour
    {
        TMP_Text? text;
        public TMP_Text Text => text ? text! : (text = GetComponent<TMP_Text>());

        [SerializeField]
        [Tooltip("本文に対するルビの大きさ")]
        [Range(0.1f, 1f)]
        float rubyScale = 0.5f;

        [SerializeField]
        [Tooltip("本文のベースラインからルビのベースラインまでの高さ（em）")]
        float rubyOffset = 1f;

        [SerializeField]
        [Tooltip("ルビが本文より短いとき、本文の幅いっぱいに字間を広げる")]
        bool justify = true;

        public void Set(in StringWithRuby stringWithRuby)
        {
            Text.text = ToTaggedText(stringWithRuby, Text, rubyScale, rubyOffset, justify);
        }

        public static string ToTaggedText(in StringWithRuby source, TMP_Text text,
            float rubyScale = 0.5f, float rubyOffset = 1f, bool justify = true)
        {
            if (source.rubies == null || source.rubies.Length == 0)
            {
                return source.body ?? string.Empty;
            }

            // 字間はsizeタグに関係なく、本来のフォントサイズの1/100em単位で1文字ごとに足される
            var characterSpacing = text.characterSpacing;
            var font = text.font;
            if (font)
            {
                characterSpacing += font.normalSpacingOffset;
                if ((text.fontStyle & FontStyles.Bold) != 0)
                {
                    characterSpacing += font.boldSpacing;
                }
            }
            characterSpacing *= 0.01f;

            var body = source.body.AsSpan();
            var builder = new StringBuilder(source.body.Length + source.joinedRubyText.Length + source.rubies.Length * 96);
            Span<char> buffer = body.Length <= 512 ? stackalloc char[body.Length] : new char[body.Length];
            var position = 0;
            foreach (var it in source.rubies)
            {
                var range = it.bodyStringRange;
                if (range.length <= 0
                    || it.textLength <= 0
                    || range.start < position
                    || range.end > body.Length)
                {
                    continue;
                }
                builder.Append(body[position..range.start]);
                position = range.end;

                var rubyBody = body.Slice(range.start, range.length);
                var ruby = source.joinedRubyText.AsSpan(it.textPosition, it.textLength);

                var plainBody = RemoveTags(rubyBody, buffer);
                var rubyCount = CountTextElements(ruby);
                var bodyWidth = Measure(text, plainBody) + characterSpacing * CountTextElements(plainBody);
                var rubyWidth = Measure(text, ruby) * rubyScale + characterSpacing * rubyCount;

                AppendRuby(builder, rubyBody, ruby, bodyWidth, rubyWidth, rubyCount,
                    rubyScale, rubyOffset, justify);
            }
            builder.Append(body[position..]);
            return builder.ToString();
        }

        /// <summary>
        /// 本文、ルビの順に並べ、spaceタグでカーソルを戻してルビを本文の上に重ねる。
        /// ブロック全体の幅は本文とルビの広いほうになる。
        /// </summary>
        static void AppendRuby(StringBuilder builder, ReadOnlySpan<char> body, ReadOnlySpan<char> ruby,
            float bodyWidth, float rubyWidth, int rubyCount,
            float rubyScale, float rubyOffset, bool justify)
        {
            var blockWidth = Mathf.Max(bodyWidth, rubyWidth);
            // ルビのほうが長ければ本文を中央に寄せる
            var bodyStart = (blockWidth - bodyWidth) / 2f;

            float rubyStart;
            var gap = 0f;
            if (justify && rubyCount > 1 && rubyWidth < bodyWidth)
            {
                rubyStart = 0f;
                gap = (bodyWidth - rubyWidth) / (rubyCount - 1);
            }
            else
            {
                rubyStart = (blockWidth - rubyWidth) / 2f;
            }
            // 最後の文字の後ろのcspaceは、閉じタグでTextMeshProが取り消す
            var rubyEnd = rubyStart + rubyWidth + gap * (rubyCount - 1);

            builder.Append("<nobr>");
            AppendSpace(builder, bodyStart);
            builder.Append(body);
            AppendSpace(builder, rubyStart - (bodyStart + bodyWidth));

            // emは現在のフォントサイズ基準なので、sizeタグより外側で指定する
            if (gap > 0f)
            {
                builder.Append("<cspace=").Append(Format(gap)).Append("em>");
            }
            builder.Append("<voffset=").Append(Format(rubyOffset)).Append("em>");
            builder.Append("<size=").Append(Format(rubyScale * 100f)).Append("%>");
            builder.Append(ruby);
            builder.Append("</size></voffset>");
            if (gap > 0f)
            {
                builder.Append("</cspace>");
            }

            AppendSpace(builder, blockWidth - rubyEnd);
            builder.Append("</nobr>");
        }

        static void AppendSpace(StringBuilder builder, float em)
        {
            if (Mathf.Abs(em) < 0.001f)
            {
                return;
            }
            builder.Append("<space=").Append(Format(em)).Append("em>");
        }

        static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        static ReadOnlySpan<char> RemoveTags(ReadOnlySpan<char> source, Span<char> buffer)
        {
            var length = 0;
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] == '<')
                {
                    var close = source[i..].IndexOf('>');
                    if (close >= 0)
                    {
                        i += close;
                        continue;
                    }
                }
                buffer[length] = source[i];
                ++length;
            }
            return buffer[..length];
        }

        static int CountTextElements(ReadOnlySpan<char> value)
        {
            var count = 0;
            foreach (var it in value)
            {
                if (!char.IsLowSurrogate(it))
                {
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// グリフの送り幅からem単位の幅を求める。字間は含まない。カーニングとタグによる太字は考慮しない。
        /// </summary>
        static float Measure(TMP_Text text, ReadOnlySpan<char> value)
        {
            var font = text.font;
            var width = 0f;
            for (int i = 0; i < value.Length; ++i)
            {
                uint unicode = value[i];
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    unicode = (uint)char.ConvertToUtf32(value[i], value[i + 1]);
                    ++i;
                }

                TMP_Character? character = null;
                if (font)
                {
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, font, true, text.fontStyle, text.fontWeight, out _);
                    if (character == null && TMP_Settings.fallbackFontAssets != null)
                    {
                        character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, font, TMP_Settings.fallbackFontAssets, true, text.fontStyle, text.fontWeight, out _);
                    }
                }
                if (character == null || !character.textAsset)
                {
                    // グリフが見つからない文字は全角幅とみなす
                    width += 1f;
                    continue;
                }

                var face = character.textAsset.faceInfo;
                width += character.glyph.metrics.horizontalAdvance * character.scale * character.glyph.scale
                    / face.pointSize * face.scale;
            }
            return width;
        }
    }
}
