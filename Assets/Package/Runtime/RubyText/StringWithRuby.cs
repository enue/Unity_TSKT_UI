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
            var rubies = new ArrayBuilder<Ruby>(0);
            var bodyText = new System.Text.StringBuilder();
            var rubyText = new System.Text.StringBuilder();

            var currentIndex = 0;
            while (true)
            {
                var beginIndex = originalText.IndexOf('{', currentIndex);
                if (beginIndex < 0)
                {
                    break;
                }
                var separatorIndex = originalText.IndexOf(':', beginIndex);
                if (separatorIndex < 0)
                {
                    break;
                }
                var endIndex = originalText.IndexOf('}', beginIndex);
                if (endIndex < 0)
                {
                    break;
                }
                if (endIndex < separatorIndex)
                {
                    bodyText.Append(originalText, currentIndex, beginIndex - currentIndex);
                    currentIndex = beginIndex + 1;
                    continue;
                }

                bodyText.Append(originalText, currentIndex, beginIndex - currentIndex);
                currentIndex = endIndex + 1;

                var body = originalText.Substring(beginIndex + 1, separatorIndex - beginIndex - 1);
                var ruby = originalText.Substring(separatorIndex + 1, endIndex - separatorIndex - 1);

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
            bodyText.Append(originalText, currentIndex, originalText.Length - currentIndex);

            return new StringWithRuby(bodyText.ToString(), rubies.writer.WrittenSpan.ToArray(), rubyText.ToString());
        }

        readonly public int[] GetBodyQuadCountRubyQuadCountMap(bool[] bodyCharacterHasQuadList)
        {
            var result = new ArrayBuilder<int>(bodyCharacterHasQuadList.Count(_ => _) + 1);
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

            return result.writer.WrittenSpan.ToArray();
        }
    }

    public readonly struct RichTextBuilder
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

        readonly ReadOnlyMemory<Ruby> rubies;
        public readonly string body;
        public readonly ReadOnlyMemory<Tag> tags;

        RichTextBuilder(string? body)
        {
            this.body = body ?? string.Empty;
            rubies = default;
            tags = default;
        }

        RichTextBuilder(string? body, ReadOnlySpan<TSKT.Ruby> rubies, string joinedRubies)
        {
            this.body = body ?? string.Empty;
            var rubyBuilder = new ArrayBuilder<Ruby>(rubies.Length);
            foreach (var it in rubies)
            {
                var ruby = new Ruby(
                    joinedRubies.AsMemory(it.textPosition, it.textLength),
                    it.bodyStringRange);
                rubyBuilder.Add(ruby);
            }
            this.rubies = rubyBuilder.writer.WrittenMemory;
            tags = default;
        }

        RichTextBuilder(string? body, ReadOnlyMemory<Ruby> rubies, ReadOnlyMemory<Tag> tags)
        {
            this.body = body ?? string.Empty;
            this.rubies = rubies;
            this.tags = tags;
        }

        public static RichTextBuilder Combine(in RichTextBuilder left, in RichTextBuilder right)
        {
            ReadOnlyMemory<Ruby> newRubies;

            if (right.rubies.Length == 0)
            {
                newRubies = left.rubies;
            }
            else
            {
                var rubyBuilder = new ArrayBuilder<Ruby>(left.rubies.Length + right.rubies.Length);

                rubyBuilder.writer.Write(left.rubies.Span);

                foreach (var it in right.rubies.Span)
                {
                    var ruby = new Ruby(
                        text: it.text,
                        bodyStringRange: new RangeInt(it.bodyStringRange.start + left.body.Length, it.bodyStringRange.length));

                    rubyBuilder.Add(ruby);
                }

                newRubies = rubyBuilder.writer.WrittenMemory;
            }

            ReadOnlyMemory<Tag> newTags;
            if (right.tags.Length == 0)
            {
                newTags = left.tags;
            }
            else
            {
                var tagBuilder = new ArrayBuilder<Tag>(left.tags.Length + right.tags.Length);
                tagBuilder.writer.Write(left.tags.Span);
                foreach (var it in right.tags.Span)
                {
                    var t = new Tag(
                        leftIndex: it.leftIndex + left.body.Length,
                        left: it.left,
                        rightIndex: it.rightIndex + left.body.Length,
                        right: it.right);
                    tagBuilder.Add(t);
                }
                newTags = tagBuilder.writer.WrittenMemory;
            }

            return new RichTextBuilder(left.body + right.body, newRubies, newTags);
        }

        readonly public RichTextBuilder Remove(int startIndex, int count)
        {
            var newBody = body.Remove(startIndex, count);
            var removeRange = new RangeInt(startIndex, count);

            // ルビの移動
            var newRubies = new ArrayBuilder<Ruby>(rubies.Length);
            foreach (var it in rubies.Span)
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
            foreach (var it in tags.Span)
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
                newRubies.writer.WrittenMemory,
                newTags.writer.WrittenMemory);
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
            var newRubies = rubies
                .ToArray()
                .Where(_ => _.bodyStringRange.start >= startIndex)
                .Where(_ => _.bodyStringRange.end <= startIndex + length);

            // 完全に範囲外になるタグを削除
            var newTags = tags
                .ToArray()
                .Where(_ => _.rightIndex > startIndex)
                .Where(_ => _.leftIndex < startIndex + length)
                .Select(_ => new Tag(
                    leftIndex: Mathf.Clamp(_.leftIndex, startIndex, startIndex + length),
                    left: _.left,
                    rightIndex: Mathf.Clamp(_.rightIndex, startIndex, startIndex + length),
                    right: _.right));

            // joinedRubyTextの切り出し

            // 頭を削除するとインデックスがずれる
            if (startIndex > 0)
            {
                newRubies = newRubies
                    .Select(_ => new Ruby(
                        _.text,
                        new RangeInt(_.bodyStringRange.start - startIndex, _.bodyStringRange.length)));

                newTags = newTags
                    .Select(_ => new Tag(
                        _.leftIndex - startIndex,
                        _.left,
                        _.rightIndex - startIndex,
                        _.right));
            }

            var newBody = body.Substring(startIndex, length);

            return new RichTextBuilder(newBody, newRubies.ToArray(), newTags.ToArray());
        }

        readonly public RichTextBuilder RemoveRubyAt(int index)
        {
            var newRubies = new Ruby[rubies.Length - 1];
            for (int i = 0; i < rubies.Length; ++i)
            {
                if (i < index)
                {
                    newRubies[i] = rubies.Span[i];
                }
                else if (i > index)
                {
                    newRubies[i - 1] = new Ruby(
                        text: rubies.Span[i].text,
                        bodyStringRange: rubies.Span[i].bodyStringRange);
                }
            }

            return new RichTextBuilder(body, newRubies, tags);
        }

        readonly public RichTextBuilder Insert(int startIndex, in RichTextBuilder value)
        {
            var newBody = body.Insert(startIndex, value.body);

            // ルビ
            var newRubies = new ArrayBuilder<Ruby>(rubies.Length + value.rubies.Length);
            foreach (var it in rubies.Span)
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
            foreach (var it in value.rubies.Span)
            {
                newRubies.Add(new Ruby(
                    it.text,
                    new RangeInt(it.bodyStringRange.start + startIndex, it.bodyStringRange.length)));
            }

            foreach (var it in rubies.Span)
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
            foreach (var it in tags.Span)
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
            foreach (var it in value.tags.Span)
            {
                newTags.Add(new Tag(
                    it.leftIndex + startIndex,
                    it.left,
                    it.rightIndex + startIndex,
                    it.right));
            }

            return new RichTextBuilder(newBody, newRubies.writer.WrittenMemory, newTags.writer.WrittenMemory);
        }

        readonly public RichTextBuilder InsertTag(int leftIndex, string left, int rightIndex, string? right)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + 1);

            tagBuilder.writer.Write(tags.Span);
            tagBuilder.Add(new Tag(leftIndex, left, rightIndex, right));

            return new RichTextBuilder(body, rubies, tagBuilder.writer.WrittenMemory);
        }

        public readonly RichTextBuilder InsertTags(params Tag[] array)
        {
            return InsertTags(new ReadOnlySpan<Tag>(array));
        }

        public readonly RichTextBuilder InsertTags(ReadOnlySpan<Tag> array)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + array.Length);
            tagBuilder.writer.Write(tags.Span);
            tagBuilder.writer.Write(array);

            return new RichTextBuilder(body, rubies, tagBuilder.writer.WrittenMemory);
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
                foreach (var it in tags.Span)
                {
                    ++tagCount;
                    if (it.right != null)
                    {
                        ++tagCount;
                    }
                }

                var tagElements = new ArrayBuilder<(int index, string value, int subSort)>(tagCount);
                for (int i = 0; i < tags.Length; ++i)
                {
                    var tag = tags.Span[i];
                    tagElements.Add((tag.leftIndex, tag.left, subSort: -i));
                    if (tag.right != null)
                    {
                        tagElements.Add((tag.rightIndex, tag.right, subSort: i));
                    }
                }

                var sortedTags = tagElements.writer.WrittenSpan
                    .ToArray()
                    .OrderByDescending(_ => _.index)
                    .ThenByDescending(_ => _.subSort);

                foreach (var (index, value, _) in sortedTags)
                {
                    var tag = new RichTextBuilder(value);
                    result = result.Insert(index, tag);
                }
            }

            var rubyBuilder = new ArrayBuilder<TSKT.Ruby>(result.rubies.Length);
            var writer = new ArrayBufferWriter<char>();
            foreach (var it in result.rubies.Span)
            {
                rubyBuilder.Add(new TSKT.Ruby(writer.WrittenCount, it.text.Length, it.bodyStringRange));
                writer.Write(it.text.Span);
            }

            return new StringWithRuby(result.body,
                rubyBuilder.writer.WrittenMemory.ToArray(),
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

        public static RichTextBuilder Parse(string body, TSKT.Ruby[]? rubies, string? joinedRubyText)
        {
            var tagRanges = new List<RangeInt>();
            var tagElements = new List<(string name, string value, bool closing)>();
            var tagPairCount = 0;
            {
                var position = 0;
                while (true)
                {
                    var left = body.IndexOf('<', position);
                    if (left < 0)
                    {
                        break;
                    }
                    var right = body.IndexOf('>', left);
                    if (right < 0)
                    {
                        break;
                    }

                    position = right;
                    tagRanges.Add(new RangeInt(left, right - left + 1));

                    var tagString = body.Substring(left, right - left + 1);

                    var attributes = tagString.Split('=');
                    var head = attributes[0].Substring(1).Trim(' ', '>');
                    string tagName;
                    bool closingTag;
                    if (head[0] == '/')
                    {
                        tagName = head.Substring(1).Trim();
                        closingTag = true;
                    }
                    else
                    {
                        tagName = head;
                        closingTag = false;
                        ++tagPairCount;
                    }

                    tagElements.Add((
                        tagName,
                        value: tagString,
                        closing: closingTag));
                }
            }

            if (tagElements.Count == 0)
            {
                return new RichTextBuilder(body, rubies, joinedRubyText);
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
            var result = new RichTextBuilder(body, rubies, joinedRubyText);
            {
                var removedRange = 0;
                foreach (var range in tagRanges)
                {
                    result = result.Remove(range.start - removedRange, range.length);
                    removedRange += range.length;
                }
            }

            result = result.InsertTags(pairTags.writer.WrittenSpan.ToArray());

            return result;
        }

        readonly public RichTextBuilder WrapWithHyphenation(UnityEngine.UI.Text text, HyphenationJpns.Ruler ruler, bool allowSplitRuby = false)
        {
            var newLinePositions = ruler.GetNewLinePositions(text.rectTransform.rect.width,
                text.font,
                text.fontSize,
                text.fontStyle,
                body,
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
    }
}
