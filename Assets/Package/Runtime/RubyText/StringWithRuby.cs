#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Buffers;

namespace TSKT
{
    public readonly struct Ruby
    {
        readonly public int textPosition;
        readonly public int textLength;
        readonly public RangeInt bodyStringRange;

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
            var rubies = new ArrayBuilder<Ruby>(0);
            var bodyText = new System.Text.StringBuilder();
            var rubyText = new System.Text.StringBuilder();

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
            if (rubies.writer.WrittenCount == 0)
            {
                return new StringWithRuby(originalText, System.Array.Empty<Ruby>(), string.Empty);
            }
            bodyText.Append(text[currentIndex..]);

            return new StringWithRuby(bodyText.ToString(), rubies.writer.WrittenSpan.ToArray(), rubyText.ToString());
        }

        readonly public int[] GetBodyQuadCountRubyQuadCountMap(bool[] bodyCharacterHasQuadList)
        {
            using var rent = System.Buffers.MemoryPool<int>.Shared.Rent(bodyCharacterHasQuadList.Count(_ => _) + 1);
            var result = new MemoryBuilder<int>(rent.Memory);
            result.Add(0);

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
                result.Add(rubyLength);
            }

            return result.Memory.ToArray();
        }

        public readonly string ToHtml()
        {
            var result = body;
            foreach (var it in rubies.Reverse())
            {
                var ruby = joinedRubyText.Substring(it.textPosition, it.textLength);
                result = result.Insert(it.bodyStringRange.end, $"</rb><rt>{ruby}</rt></ruby>");
                result = result.Insert(it.bodyStringRange.start, "<ruby><rb>");
            }
            return result;
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
            public readonly ReadOnlyMemory<char> text;
            public readonly RangeInt bodyStringRange;

            public Ruby(ReadOnlyMemory<char> text, RangeInt bodyStringRange)
            {
                this.text = text;
                this.bodyStringRange = bodyStringRange;
            }
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
            var rubyBuilder = new ArrayBuilder<Ruby>(rubies.Length);
            foreach (var it in rubies)
            {
                var ruby = new Ruby(
                    joinedRubies.AsMemory(it.textPosition, it.textLength),
                    it.bodyStringRange);
                rubyBuilder.Add(ruby);
            }
            this.rubies = rubyBuilder.writer.WrittenSpan;
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
                var rubyBuilder = new ArrayBuilder<Ruby>(left.rubies.Length + right.rubies.Length);

                rubyBuilder.writer.Write(left.rubies);

                foreach (var it in right.rubies)
                {
                    var ruby = new Ruby(
                        text: it.text,
                        bodyStringRange: new RangeInt(it.bodyStringRange.start + left.body.Length, it.bodyStringRange.length));

                    rubyBuilder.Add(ruby);
                }

                newRubies = rubyBuilder.writer.WrittenSpan;
            }

            ReadOnlySpan<Tag> newTags;
            if (right.tags.Length == 0)
            {
                newTags = left.tags;
            }
            else
            {
                var tagBuilder = new ArrayBuilder<Tag>(left.tags.Length + right.tags.Length);
                tagBuilder.writer.Write(left.tags);
                foreach (var it in right.tags)
                {
                    var t = new Tag(
                        leftIndex: it.leftIndex + left.body.Length,
                        left: it.left,
                        rightIndex: it.rightIndex + left.body.Length,
                        right: it.right);
                    tagBuilder.Add(t);
                }
                newTags = tagBuilder.writer.WrittenSpan;
            }

            // left.body.ToString() + right.body.ToString();
            Span<char> combined = new char[left.body.Length + right.body.Length];
            left.body.CopyTo(combined);
            right.body.CopyTo(combined[left.body.Length..]);

            return new RichTextBuilder(combined, newRubies, newTags);
        }

        readonly public RichTextBuilder Remove(int startIndex, int count)
        {
            // var newBody = body.ToString().Remove(startIndex, count);
            Span<char> newBody = new char[body.Length - count];
            body[..startIndex].CopyTo(newBody);
            body[(startIndex + count)..].CopyTo(newBody[startIndex..]);

            var removeRange = new RangeInt(startIndex, count);

            // ルビの移動
            var newRubies = new ArrayBuilder<Ruby>(rubies.Length);
            foreach (var it in rubies)
            {
                var newBodyRange = TrimRange(it.bodyStringRange, removeRange);
                if (newBodyRange.length > 0)
                {
                    var ruby = new Ruby(
                        text: it.text,
                        bodyStringRange: newBodyRange);
                    newRubies.Add(ruby);
                }
            }

            // タグの移動
            var newTags = new ArrayBuilder<Tag>(tags.Length);
            foreach (var it in tags)
            {
                var range = TrimRange(
                    new RangeInt(it.leftIndex, it.rightIndex - it.leftIndex),
                    removeRange);

                // 対象範囲がゼロになったタグは削除。ただしもともと閉じタグがない場合は残す。
                if (range.length > 0 || it.right == null)
                {
                    newTags.Add(new Tag(
                            range.start, it.left,
                            range.end, it.right));
                }
            }

            return new RichTextBuilder(newBody,
                newRubies.writer.WrittenSpan,
                newTags.writer.WrittenSpan);
        }

        readonly public RichTextBuilder Substring(int startIndex, int length)
        {
            if (startIndex < 0
                || length < 0
                || startIndex + length > body.Length)
            {
                throw new System.ArgumentException();
            }

            // 削除部分に重なっているルビを削除
            using var rubyBuffer = MemoryPool<Ruby>.Shared.Rent(rubies.Length);
            var newRubies = new MemoryBuilder<Ruby>(rubyBuffer.Memory);
            foreach(var it in rubies)
            {
                if (it.bodyStringRange.start >= startIndex)
                {
                    if (it.bodyStringRange.end <= startIndex + length)
                    {
                        newRubies.Add(it);
                    }
                }
            }

            // 完全に範囲外になるタグを削除
            using var tagBuffer = MemoryPool<Tag>.Shared.Rent(tags.Length);
            var newTags = new MemoryBuilder<Tag>(tagBuffer.Memory);
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
                        newTags.Add(t);
                    }
                }
            }

            // joinedRubyTextの切り出し

            // 頭を削除するとインデックスがずれる
            if (startIndex > 0)
            {
                {
                    var span = newRubies.Memory.Span;
                    for (int i = 0; i < span.Length; i++)
                    {
                        var _ = span[i];
                        var r = new Ruby(
                            _.text,
                            new RangeInt(_.bodyStringRange.start - startIndex, _.bodyStringRange.length));
                        span[i] = r;
                    }
                }
                {
                    var span = newTags.Memory.Span;
                    for (int i = 0; i < span.Length; ++i)
                    {
                        var _ = span[i];
                        var t = new Tag(
                                _.leftIndex - startIndex,
                                _.left,
                                _.rightIndex - startIndex,
                                _.right);
                        span[i] = t;
                    }
                }
            }

            var newBody = body.Slice(startIndex, length);

            return new RichTextBuilder(newBody, newRubies.Memory.ToArray().AsSpan(), newTags.Memory.ToArray());
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
                    newRubies[i - 1] = new Ruby(
                        text: rubies[i].text,
                        bodyStringRange: rubies[i].bodyStringRange);
                }
            }

            return new RichTextBuilder(body, newRubies, tags);
        }

        readonly public RichTextBuilder Insert(int startIndex, in RichTextBuilder value)
        {
            // var newBody = body.ToString().Insert(startIndex, value.body.ToString());
            Span<char> newBody = new char[body.Length + value.body.Length];
            body[..startIndex].CopyTo(newBody);
            value.body.CopyTo(newBody[startIndex..]);
            body[startIndex..].CopyTo(newBody[(startIndex + value.body.Length)..]);

            // ルビ
            var newRubies = new ArrayBuilder<Ruby>(rubies.Length + value.rubies.Length);
            foreach (var it in rubies)
            {
                if (it.bodyStringRange.end <= startIndex)
                {
                    // 挿入部分より前
                    newRubies.Add(it);
                }
                else if (it.bodyStringRange.start < startIndex
                    && it.bodyStringRange.end >= startIndex)
                {
                    // 挿入部分をまたぐ
                    newRubies.Add(new Ruby(it.text,
                        new RangeInt(it.bodyStringRange.start, it.bodyStringRange.length + value.body.Length)));
                }
            }

            // 挿入部分
            foreach (var it in value.rubies)
            {
                newRubies.Add(new Ruby(
                    it.text,
                    new RangeInt(it.bodyStringRange.start + startIndex, it.bodyStringRange.length)));
            }

            foreach (var it in rubies)
            {
                if (it.bodyStringRange.start >= startIndex)
                {
                    // 挿入部分より後
                    newRubies.Add(new Ruby(it.text,
                        new RangeInt(it.bodyStringRange.start + value.body.Length, it.bodyStringRange.length)));
                }
            }

            // tag
            var newTags = new ArrayBuilder<Tag>(tags.Length + value.tags.Length);
            foreach (var it in tags)
            {
                if (it.rightIndex <= startIndex)
                {
                    newTags.Add(it);
                }
                else if (it.leftIndex < startIndex
                    && it.rightIndex >= startIndex)
                {
                    newTags.Add(new Tag(
                        it.leftIndex,
                        it.left,
                        it.rightIndex + value.body.Length,
                        it.right));
                }
                else
                {
                    newTags.Add(new Tag(
                        it.leftIndex + value.body.Length,
                        it.left,
                        it.rightIndex + value.body.Length,
                        it.right));
                }
            }
            foreach (var it in value.tags)
            {
                newTags.Add(new Tag(
                    it.leftIndex + startIndex,
                    it.left,
                    it.rightIndex + startIndex,
                    it.right));
            }

            return new RichTextBuilder(newBody, newRubies.writer.WrittenSpan, newTags.writer.WrittenSpan);
        }

        readonly public RichTextBuilder InsertTag(int leftIndex, string left, int rightIndex, string? right)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + 1);

            tagBuilder.writer.Write(tags);
            tagBuilder.Add(new Tag(leftIndex, left, rightIndex, right));

            return new RichTextBuilder(body, rubies, tagBuilder.writer.WrittenSpan);
        }

        public readonly RichTextBuilder InsertTags(params Tag[] array)
        {
            return InsertTags(new ReadOnlySpan<Tag>(array));
        }

        public readonly RichTextBuilder InsertTags(ReadOnlySpan<Tag> array)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + array.Length);
            tagBuilder.writer.Write(tags);
            tagBuilder.writer.Write(array);

            return new RichTextBuilder(body, rubies, tagBuilder.writer.WrittenSpan);
        }

        public readonly RichTextBuilder ClearTags()
        {
            return new RichTextBuilder(body, rubies, null);
        }

        readonly public StringWithRuby ToStringWithRuby()
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

                using var buffer = MemoryPool<(int index, string value, int subSort)>.Shared.Rent(tagCount);
                var tagElements = new MemoryBuilder<(int index, string value, int subSort)>(buffer.Memory);
                for (int i = 0; i < tags.Length; ++i)
                {
                    var tag = tags[i];
                    tagElements.Add((tag.leftIndex, tag.left, subSort: -i));
                    if (tag.right != null)
                    {
                        tagElements.Add((tag.rightIndex, tag.right, subSort: i));
                    }
                }
                var sortedTags = tagElements.Memory
                    .ToArray()
                    .OrderByDescending(_ => _.index)
                    .ThenByDescending(_ => _.subSort);

                foreach (var (index, value, _) in sortedTags)
                {
                    var tag = new RichTextBuilder(value);
                    result = result.Insert(index, tag);
                }
            }

            using var rubyBuffer = MemoryPool<TSKT.Ruby>.Shared.Rent(result.rubies.Length);
            var rubyBuilder = new MemoryBuilder<TSKT.Ruby>(rubyBuffer.Memory);
            var writer = new ArrayBufferWriter<char>();
            foreach (var it in result.rubies)
            {
                rubyBuilder.Add(new TSKT.Ruby(writer.WrittenCount, it.text.Length, it.bodyStringRange));
                writer.Write(it.text.Span);
            }

            return new StringWithRuby(result.body.ToString(),
                rubyBuilder.Memory.ToArray(),
                new string(writer.WrittenSpan));
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

            var pairTags = new ArrayBuilder<Tag>(tagPairCount);
            {
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

                        pairTags.Add(new Tag(
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
                        pairTags.Add(new Tag(position, tagValue, position, null));
                    }
                }
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

            result = result.InsertTags(pairTags.writer.WrittenSpan);

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

        public TypingText[] GetTypingSequence()
        {
            var result = new List<TypingText>();
            for (int i = 0; i < body.Length; ++i)
            {
                if (body[i] == '\n')
                {
                    // 改行はカウントしない
                }
                else
                {
                    result.Add(new TypingText(
                        i + 1,
                        Substring(0, i + 1).ToStringWithRuby()));
                }
            }
            return result.ToArray();
        }
    }
}
