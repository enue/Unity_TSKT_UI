#nullable enable
using Cysharp.Text;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TSKT
{
    public readonly struct Ruby
    {
        public readonly int textPosition;
        public readonly int textLength;
        public readonly RangeInt bodyStringRange;

        public Ruby(int textPosition, int textLength, RangeInt bodyStringRange)
        {
            this.textPosition = textPosition;
            this.textLength = textLength;
            this.bodyStringRange = bodyStringRange;
        }
    }

    public readonly struct StringWithRuby
    {
        public readonly Ruby[] rubies;
        public readonly string joinedRubyText;
        public readonly string body;

        public StringWithRuby(string? body, Ruby[]? rubies, string? joinedRubyText)
        {
            this.body = body ?? string.Empty;
            this.rubies = rubies ?? System.Array.Empty<Ruby>();
            this.joinedRubyText = joinedRubyText ?? string.Empty;
        }

        public static StringWithRuby Parse(string originalText)
        {
            var text = originalText.AsSpan();
            var rubies = new ArrayBufferWriter<Ruby>();
            using var bodyText = ZString.CreateStringBuilder();
            using var rubyText = ZString.CreateStringBuilder();

            var currentIndex = 0;
            while (true)
            {
                var beginIndex = text[currentIndex..].IndexOf('{') + currentIndex;
                if (beginIndex < currentIndex)
                {
                    break;
                }
                var separatorIndex = text[beginIndex..].IndexOf(':') + beginIndex;
                if (separatorIndex < beginIndex)
                {
                    break;
                }
                var endIndex = text[beginIndex..].IndexOf('}') + beginIndex;
                if (endIndex < beginIndex)
                {
                    break;
                }
                if (endIndex < separatorIndex)
                {
                    bodyText.Append(text[currentIndex..beginIndex]);
                    currentIndex = beginIndex + 1;
                    continue;
                }

                bodyText.Append(text[currentIndex..beginIndex]);
                currentIndex = endIndex + 1;

                var body = text[(beginIndex + 1)..separatorIndex];
                var ruby = text[(separatorIndex + 1)..endIndex];

                var word = new Ruby(
                    textPosition: rubyText.Length,
                    textLength: ruby.Length,
                    bodyStringRange: new RangeInt(bodyText.Length, body.Length));
                rubies.Add(word);

                bodyText.Append(body);
                rubyText.Append(ruby);
            }
            if (rubies.WrittenCount == 0)
            {
                return new StringWithRuby(originalText, System.Array.Empty<Ruby>(), string.Empty);
            }
            bodyText.Append(text[currentIndex..]);

            return new StringWithRuby(bodyText.ToString(), rubies.WrittenSpan.ToArray(), rubyText.ToString());
        }

        public readonly int[] GetBodyQuadCountRubyQuadCountMap(bool[] bodyCharacterHasQuadList)
        {
            var builder = new ArrayBufferWriter<int>(bodyCharacterHasQuadList.Count(_ => _) + 1);
            builder.Add(0);

            var rubyLength = 0;
            for (int i = 0; i < bodyCharacterHasQuadList.Length; ++i)
            {
                if (!bodyCharacterHasQuadList[i])
                {
                    continue;
                }

                var rubyIndex = System.Array.FindIndex(rubies, _ => _.bodyStringRange.start <= i && i < _.bodyStringRange.end);
                if (rubyIndex >= 0)
                {
                    var ruby = rubies[rubyIndex];
                    var totalQuadCountUnderRuby = bodyCharacterHasQuadList
                        .Skip(ruby.bodyStringRange.start)
                        .Take(ruby.bodyStringRange.length)
                        .Count(_ => _);
                    var currentQuadCountUnderRuby = bodyCharacterHasQuadList
                        .Skip(ruby.bodyStringRange.start)
                        .Take(i - ruby.bodyStringRange.start + 1)
                        .Count(_ => _);

                    rubyLength = ruby.textLength * currentQuadCountUnderRuby / totalQuadCountUnderRuby + ruby.textPosition;
                }
                builder.Add(rubyLength);
            }

            return builder.WrittenSpan.ToArray();
        }

        public readonly string ToHtml()
        {
            using var result = ZString.CreateStringBuilder();
            result.Append(body);
            foreach (var it in rubies.Reverse())
            {
                var ruby = joinedRubyText.Substring(it.textPosition, it.textLength);
                result.Insert(it.bodyStringRange.end, $"</rb><rt>{ruby}</rt></ruby>");
                result.Insert(it.bodyStringRange.start, "<ruby><rb>");
            }
            return result.ToString();
        }
    }

    public readonly ref struct RichTextBuilder
    {
        public readonly struct Tag
        {
            readonly public int leftIndex;
            readonly public int rightIndex;
            readonly public string left;
            readonly public string? right;

            public Tag(int leftIndex, string left, int rightIndex, string? right)
            {
                this.leftIndex = leftIndex;
                this.left = left;

                this.rightIndex = rightIndex;
                this.right = right;
            }
        }
        readonly struct Ruby
        {
            readonly (string joined, RangeInt range) text;
            public readonly RangeInt bodyStringRange;

            public Ruby((string, RangeInt) text, RangeInt bodyStringRange)
            {
                this.text = text;
                this.bodyStringRange = bodyStringRange;
            }
            public ReadOnlySpan<char> AsReadOnlySpan() => text.joined.AsSpan(text.range.start, text.range.length);

            public Ruby Move(RangeInt bodyStringRange) => new(text, bodyStringRange);
        }

        readonly ReadOnlySpan<Ruby> rubies;
        public readonly ReadOnlySpan<char> body;
        public readonly ReadOnlySpan<Tag> tags;

        RichTextBuilder(string? body)
        {
            this.body = body ?? string.Empty;
            rubies = default;
            tags = default;
        }

        RichTextBuilder(ReadOnlySpan<char> body, ReadOnlySpan<TSKT.Ruby> rubies, string joinedRubies)
        {
            this.body = body;
            if (rubies.Length > 0)
            {
                var rubyBuilder = new ArrayBufferWriter<Ruby>(rubies.Length);
                foreach (var it in rubies)
                {
                    var ruby = new Ruby(
                        (joinedRubies, new RangeInt(it.textPosition, it.textLength)),
                        it.bodyStringRange);
                    rubyBuilder.Add(ruby);
                }
                this.rubies = rubyBuilder.WrittenSpan;
            }
            else
            {
                this.rubies = default;
            }
            tags = default;
        }

        RichTextBuilder(ReadOnlySpan<char> body, ReadOnlySpan<Ruby> rubies, ReadOnlySpan<Tag> tags)
        {
            this.body = body;
            this.rubies = rubies;
            this.tags = tags;
        }

        public static RichTextBuilder Combine(in RichTextBuilder left, in RichTextBuilder right)
        {
            ReadOnlySpan<Ruby> newRubies;

            if (right.rubies.Length == 0)
            {
                newRubies = left.rubies;
            }
            else
            {
                var rubyBuilder = new ArrayBufferWriter<Ruby>(left.rubies.Length + right.rubies.Length);

                rubyBuilder.Write(left.rubies);

                foreach (var it in right.rubies)
                {
                    var ruby = it.Move(new RangeInt(it.bodyStringRange.start + left.body.Length, it.bodyStringRange.length));
                    rubyBuilder.Add(ruby);
                }

                newRubies = rubyBuilder.WrittenSpan;
            }

            ReadOnlySpan<Tag> newTags;
            if (right.tags.Length == 0)
            {
                newTags = left.tags;
            }
            else
            {
                var tagBuilder = new ArrayBufferWriter<Tag>(left.tags.Length + right.tags.Length);
                tagBuilder.Write(left.tags);
                foreach (var it in right.tags)
                {
                    var t = new Tag(
                        leftIndex: it.leftIndex + left.body.Length,
                        left: it.left,
                        rightIndex: it.rightIndex + left.body.Length,
                        right: it.right);
                    tagBuilder.Add(t);
                }
                newTags = tagBuilder.WrittenSpan;
            }

            using var combined = ZString.CreateStringBuilder();
            combined.Append(left.body);
            combined.Append(right.body);

            return new RichTextBuilder(combined.ToString(), newRubies, newTags);
        }

        public readonly RichTextBuilder Remove(int startIndex, int count)
        {
            using var newBody = ZString.CreateStringBuilder();
            newBody.Append(body);
            newBody.Remove(startIndex, count);

            var removeRange = new RangeInt(startIndex, count);

            // ルビの移動
            ReadOnlySpan<Ruby> newRubies;
            if (rubies.Length > 0)
            {
                var builder = new ArrayBufferWriter<Ruby>(rubies.Length);
                foreach (var it in rubies)
                {
                    var newBodyRange = TrimRange(it.bodyStringRange, removeRange);
                    if (newBodyRange.length > 0)
                    {
                        var ruby = it.Move(bodyStringRange: newBodyRange);
                        builder.Add(ruby);
                    }
                }
                newRubies = builder.WrittenSpan;
            }
            else
            {
                newRubies = default;
            }

            // タグの移動
            ReadOnlySpan<Tag> newTags;
            if (tags.Length > 0)
            {
                var builder = new ArrayBufferWriter<Tag>(tags.Length);
                foreach (var it in tags)
                {
                    var range = TrimRange(
                        new RangeInt(it.leftIndex, it.rightIndex - it.leftIndex),
                        removeRange);

                    // 対象範囲がゼロになったタグは削除。ただしもともと閉じタグがない場合は残す。
                    if (range.length > 0 || it.right == null)
                    {
                        builder.Add(new Tag(
                                range.start, it.left,
                                range.end, it.right));
                    }
                }
                newTags = builder.WrittenSpan;
            }
            else
            {
                newTags = default;
            }

            return new RichTextBuilder(newBody.ToString(),
                newRubies,
                newTags);
        }

        public readonly RichTextBuilder Substring(int startIndex, int length)
        {
            if (startIndex < 0
                || length < 0
                || startIndex + length > body.Length)
            {
                throw new System.ArgumentException();
            }

            // 削除部分に重なっているルビを削除
            Ruby[] newRubies;
            if (rubies.Length > 0)
            {
                var builder = new ArrayBufferWriter<Ruby>(rubies.Length);
                foreach (var it in rubies)
                {
                    if (it.bodyStringRange.start >= startIndex)
                    {
                        if (it.bodyStringRange.end <= startIndex + length)
                        {
                            builder.Add(it);
                        }
                    }
                }
                newRubies = builder.WrittenSpan.ToArray();
            }
            else
            {
                newRubies = System.Array.Empty<Ruby>();
            }

            // 完全に範囲外になるタグを削除
            Tag[] newTags;
            if (tags.Length > 0)
            {
                var builder = new ArrayBufferWriter<Tag>(tags.Length);
                foreach (var it in tags)
                {
                    if (it.rightIndex > startIndex)
                    {
                        if (it.leftIndex < startIndex + length)
                        {
                            var t = new Tag(
                                leftIndex: Mathf.Clamp(it.leftIndex, startIndex, startIndex + length),
                                left: it.left,
                                rightIndex: Mathf.Clamp(it.rightIndex, startIndex, startIndex + length),
                                right: it.right);
                            builder.Add(t);
                        }
                    }
                }
                newTags = builder.WrittenSpan.ToArray();
            }
            else
            {
                newTags = System.Array.Empty<Tag>();
            }

            // joinedRubyTextの切り出し

            // 頭を削除するとインデックスがずれる
            if (startIndex > 0)
            {
                {
                    for (int i = 0; i < newRubies.Length; i++)
                    {
                        var _ = newRubies[i];
                        newRubies[i] = _.Move(new RangeInt(_.bodyStringRange.start - startIndex, _.bodyStringRange.length));
                    }
                }
                {
                    for (int i = 0; i < newTags.Length; ++i)
                    {
                        var _ = newTags[i];
                        var t = new Tag(
                                _.leftIndex - startIndex,
                                _.left,
                                _.rightIndex - startIndex,
                                _.right);
                        newTags[i] = t;
                    }
                }
            }

            var newBody = body.Slice(startIndex, length);

            return new RichTextBuilder(newBody, newRubies, newTags);
        }

        readonly public RichTextBuilder RemoveRubyAt(int index)
        {
            var newRubies = new Ruby[rubies.Length - 1];
            for (int i = 0; i < rubies.Length; ++i)
            {
                if (i < index)
                {
                    newRubies[i] = rubies[i];
                }
                else if (i > index)
                {
                    newRubies[i - 1] = rubies[i];
                }
            }

            return new RichTextBuilder(body, newRubies, tags);
        }

        readonly public RichTextBuilder Insert(int startIndex, in RichTextBuilder value)
        {
            using var newBody = ZString.CreateStringBuilder();
            newBody.Append(body[..startIndex]);
            newBody.Append(value.body);
            newBody.Append(body[startIndex..]);

            // ルビ
            ReadOnlySpan<Ruby> newRubies;
            if (rubies.Length + value.rubies.Length > 0)
            {
                var builder = new ArrayBufferWriter<Ruby>(rubies.Length + value.rubies.Length);
                foreach (var it in rubies)
                {
                    if (it.bodyStringRange.end <= startIndex)
                    {
                        // 挿入部分より前
                        builder.Add(it);
                    }
                    else if (it.bodyStringRange.start < startIndex
                        && it.bodyStringRange.end >= startIndex)
                    {
                        // 挿入部分をまたぐ
                        builder.Add(it.Move(new RangeInt(it.bodyStringRange.start, it.bodyStringRange.length + value.body.Length)));
                    }
                }

                // 挿入部分
                foreach (var it in value.rubies)
                {
                    builder.Add(it.Move(new RangeInt(it.bodyStringRange.start + startIndex, it.bodyStringRange.length)));
                }

                foreach (var it in rubies)
                {
                    if (it.bodyStringRange.start >= startIndex)
                    {
                        // 挿入部分より後
                        builder.Add(it.Move(new RangeInt(it.bodyStringRange.start + value.body.Length, it.bodyStringRange.length)));
                    }
                }
                newRubies = builder.WrittenSpan;
            }
            else
            {
                newRubies = default;
            }

            // tag
            ReadOnlySpan<Tag> newTags;
            if (tags.Length + value.tags.Length > 0)
            {
                var builder = new ArrayBufferWriter<Tag>(tags.Length + value.tags.Length);
                foreach (var it in tags)
                {
                    if (it.rightIndex <= startIndex)
                    {
                        builder.Add(it);
                    }
                    else if (it.leftIndex < startIndex
                        && it.rightIndex >= startIndex)
                    {
                        builder.Add(new Tag(
                            it.leftIndex,
                            it.left,
                            it.rightIndex + value.body.Length,
                            it.right));
                    }
                    else
                    {
                        builder.Add(new Tag(
                            it.leftIndex + value.body.Length,
                            it.left,
                            it.rightIndex + value.body.Length,
                            it.right));
                    }
                }
                foreach (var it in value.tags)
                {
                    builder.Add(new Tag(
                        it.leftIndex + startIndex,
                        it.left,
                        it.rightIndex + startIndex,
                        it.right));
                }
                newTags = builder.WrittenSpan;
            }
            else
            {
                newTags = default;
            }
            return new RichTextBuilder(newBody.ToString(), newRubies, newTags);
        }

        readonly public RichTextBuilder InsertTag(int leftIndex, string left, int rightIndex, string? right)
        {
            var tagBuilder = new ArrayBufferWriter<Tag>(tags.Length + 1);

            tagBuilder.Write(tags);
            tagBuilder.Add(new Tag(leftIndex, left, rightIndex, right));

            return new RichTextBuilder(body, rubies, tagBuilder.WrittenSpan);
        }

        public readonly RichTextBuilder InsertTags(params Tag[] array)
        {
            return InsertTags(new ReadOnlySpan<Tag>(array));
        }

        public readonly RichTextBuilder InsertTags(ReadOnlySpan<Tag> array)
        {
            if (tags.Length + array.Length > 0)
            {
                var tagBuilder = new ArrayBufferWriter<Tag>(tags.Length + array.Length);
                tagBuilder.Write(tags);
                tagBuilder.Write(array);

                return new RichTextBuilder(body, rubies, tagBuilder.WrittenSpan);
            }
            return this;
        }

        public readonly RichTextBuilder ClearTags()
        {
            return new RichTextBuilder(body, rubies, null);
        }

        public readonly StringWithRuby ToStringWithRuby()
        {
            var result = this;

            if (tags.Length > 0)
            {
                var tagCount = 0;
                foreach (var it in tags)
                {
                    ++tagCount;
                    if (it.right != null)
                    {
                        ++tagCount;
                    }
                }

                {
                    var builder = new ArrayBufferWriter<(int index, string value, int subSort)>(tagCount);
                    for (int i = 0; i < tags.Length; ++i)
                    {
                        var tag = tags[i];
                        builder.Add((tag.leftIndex, tag.left, subSort: -i));
                        if (tag.right != null)
                        {
                            builder.Add((tag.rightIndex, tag.right, subSort: i));
                        }
                    }
                    var sortedTags = builder.WrittenSpan
                        .ToArray()
                        .OrderByDescending(_ => _.index)
                        .ThenByDescending(_ => _.subSort);

                    foreach (var (index, value, _) in sortedTags)
                    {
                        var tag = new RichTextBuilder(value);
                        result = result.Insert(index, tag);
                    }
                }
            }

            TSKT.Ruby[]? newRubies;
            string? joinedRuby;
            if (result.rubies.Length > 0)
            {
                var rubyBuilder = new ArrayBufferWriter<TSKT.Ruby>(result.rubies.Length);
                using var joinedRubyBuilder = ZString.CreateStringBuilder();

                foreach (var it in result.rubies)
                {
                    var ruby = it.AsReadOnlySpan();
                    rubyBuilder.Add(new TSKT.Ruby(joinedRubyBuilder.Length, ruby.Length, it.bodyStringRange));
                    joinedRubyBuilder.Append(ruby);
                }
                newRubies = rubyBuilder.WrittenSpan.ToArray();
                joinedRuby = joinedRubyBuilder.ToString();
            }
            else
            {
                newRubies = null;
                joinedRuby = null;
            }

            return new StringWithRuby(result.body.ToString(),
                newRubies,
                joinedRuby);
        }

        public static RichTextBuilder Parse(string originalText)
        {
            var stringWithRuby = StringWithRuby.Parse(originalText);
            return Parse(stringWithRuby.body, stringWithRuby.rubies, stringWithRuby.joinedRubyText);
        }

        public static RichTextBuilder Parse(string originalText, bool tag, bool ruby)
        {
            StringWithRuby stringWithRuby;
            if (ruby)
            {
                stringWithRuby = StringWithRuby.Parse(originalText);
            }
            else
            {
                stringWithRuby = new StringWithRuby(originalText, null, null);
            }
            if (tag)
            {
                return Parse(stringWithRuby.body, stringWithRuby.rubies, stringWithRuby.joinedRubyText);
            }
            else
            {
                return new RichTextBuilder(stringWithRuby.body, stringWithRuby.rubies, stringWithRuby.joinedRubyText);
            }
        }

        public static RichTextBuilder Parse(string source, TSKT.Ruby[]? rubies, string? joinedRubyText)
        {
            var body = source.AsSpan();
            var tagRanges = new List<RangeInt>();
            var tagElements = new List<(string name, string value, bool closing)>();
            var tagPairCount = 0;
            {
                var position = 0;
                while (true)
                {
                    var left = body[position..].IndexOf('<') + position;
                    if (left < position)
                    {
                        break;
                    }
                    var right = body[left..].IndexOf('>') + left;
                    if (right < left)
                    {
                        break;
                    }

                    position = right;
                    tagRanges.Add(new RangeInt(left, right + 1 - left));
                    var tagString = body[left..(right + 1)];

                    ReadOnlySpan<char> head;
                    {
                        var i = tagString.IndexOf('=');
                        if (i < 0)
                        {
                            head = tagString;
                        }
                        else
                        {
                            head = tagString[..i];
                        }
                        head = head[1..].Trim(" >");
                    }
                    ReadOnlySpan<char> tagName;
                    bool closingTag;
                    if (head[0] == '/')
                    {
                        tagName = head[1..].Trim();
                        closingTag = true;
                    }
                    else
                    {
                        tagName = head;
                        closingTag = false;
                        ++tagPairCount;
                    }

                    tagElements.Add((
                        tagName.ToString(),
                        value: tagString.ToString(),
                        closing: closingTag));
                }
            }

            if (tagElements.Count == 0)
            {
                return new RichTextBuilder(source, rubies, joinedRubyText ?? "");
            }

            ReadOnlySpan<Tag> pairTags;
            if (tagPairCount > 0)
            {
                var builder = new ArrayBufferWriter<Tag>(tagPairCount);
                var positionOffset = 0;
                var dict = new Dictionary<string, Stack<(string tagValue, int position)>>();
                for (int i = 0; i < tagElements.Count; ++i)
                {
                    var tag = tagElements[i];
                    var position = tagRanges[i].start - positionOffset;
                    positionOffset += tagRanges[i].length;
                    if (tag.closing)
                    {
                        dict.TryGetValue(tag.name, out var stack);
                        var left = stack.Pop();

                        builder.Add(new Tag(
                            left.position,
                            left.tagValue,
                            position,
                            tag.value));
                    }
                    else
                    {
                        if (!dict.TryGetValue(tag.name, out var stack))
                        {
                            stack = new Stack<(string tagValue, int position)>();
                            dict.Add(tag.name, stack);
                        }
                        stack.Push((tag.value, position));
                    }
                }

                // 閉じタグがみつからない場合は開始タグ単独で登録。
                // ちなみに閉じタグがない場合、UnityUIは無効なのに対してTextMeshProは有効。
                // UnityUIだとおそらくデータミスなので無視して、TMPにあわせておく。
                foreach (var it in dict)
                {
                    foreach(var (tagValue, position) in it.Value)
                    {
                        builder.Add(new Tag(position, tagValue, position, null));
                    }
                }

                pairTags = builder.WrittenSpan;
            }
            else
            {
                pairTags = default;
            }

            // bodyからtag文字列を削除
            var result = new RichTextBuilder(source, rubies, joinedRubyText ?? "");
            {
                var removedRange = 0;
                foreach (var range in tagRanges)
                {
                    result = result.Remove(range.start - removedRange, range.length);
                    removedRange += range.length;
                }
            }

            result = result.InsertTags(pairTags);

            return result;
        }

        readonly public RichTextBuilder WrapWithHyphenation(UnityEngine.UI.Text text, HyphenationJpns.Ruler ruler, bool allowSplitRuby = false)
        {
            var newLinePositions = ruler.GetNewLinePositions(text.rectTransform.rect.width,
                text.font,
                text.fontSize,
                text.fontStyle,
                body.ToString(),
                false,
                allowSplitRuby ? null : rubies.ToArray().Select(_ => _.bodyStringRange).ToArray());

            var result = this;
            if (newLinePositions.Count > 0)
            {
                var newLine = new RichTextBuilder("\n");
                for (int i = 0; i < newLinePositions.Count; ++i)
                {
                    result = result.Insert(newLinePositions[i] + (i * newLine.body.Length), newLine);
                }
            }
            return result;
        }

        static RangeInt TrimRange(RangeInt original, RangeInt removeRange)
        {
            if (original.end <= removeRange.start)
            {
                // 削除範囲より前にある
                // 影響なし
                return original;
            }
            else if (original.start < removeRange.start
                && original.end > removeRange.start
                && original.end <= removeRange.end)
            {
                // 後ろが重なっている
                return new RangeInt(original.start, removeRange.start - original.start);
            }
            else if (original.start <= removeRange.start
                && original.end >= removeRange.end)
            {
                // 削除部分を包含
                return new RangeInt(original.start, original.length - removeRange.length);
            }
            else if (original.start >= removeRange.start
                && original.end <= removeRange.end)
            {
                // 削除部分に包含
                // 長さ0になる
                return new RangeInt(removeRange.start, 0);
            }
            else if (original.start >= removeRange.start
                && original.start <= removeRange.end
                && original.end > removeRange.end)
            {
                // 前が重なっている
                return new RangeInt(removeRange.start, original.end - removeRange.end);
            }
            else
            {
                // 削除範囲より後ろにある
                return new RangeInt(original.start - removeRange.length, original.length);
            }
        }

        public TypingText[] GetTypingSequence(string skipCharacters = "\n")
        {
            var end = ToStringWithRuby();
            var result = new List<TypingText>();
            for (int i = 0; i < body.Length - 1; ++i)
            {
                if (!skipCharacters.Contains(body[i]))
                {
                    var substring = Substring(0, i + 1).ToStringWithRuby();
                    result.Add(new TypingText(
                        i + 1,
                        new StringWithRuby(end.body, substring.rubies, substring.joinedRubyText)));
                }
            }
            result.Add(new TypingText(end.body.Length, end));
            return result.ToArray();
        }
    }
}
