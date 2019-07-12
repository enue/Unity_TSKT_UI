using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public readonly struct StringWithRuby
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

        public readonly struct Tag
        {
            readonly public int leftIndex;
            readonly public int rightIndex;
            readonly public string left;
            readonly public string right;

            public Tag(int leftIndex, string left, int rightIndex, string right)
            {
                this.leftIndex = leftIndex;
                this.left = left;

                this.rightIndex = rightIndex;
                this.right = right;
            }
        }

        struct ArrayBuilder<T>
        {
            readonly T[] array;
            public T[] Array
            {
                get
                {
                    UnityEngine.Assertions.Assert.AreEqual(array.Length, index);
                    return array;
                }
            }

            int index;

            public ArrayBuilder(int count)
            {
                array = new T[count];
                index = 0;
            }

            public void Add(T value)
            {
                array[index] = value;
                ++index;
            }
        }

        readonly public Ruby[] rubies;
        readonly public string joinedRubyText;
        readonly public string body;
        readonly public Tag[] tags;

        public StringWithRuby(string body, Ruby[] rubies, string joinedText, Tag[] tags)
        {
            this.body = body ?? string.Empty;
            this.rubies = rubies ?? System.Array.Empty<Ruby>();
            this.joinedRubyText = joinedText ?? string.Empty;
            this.tags = tags ?? System.Array.Empty<Tag>();
        }

        public static StringWithRuby Combine(StringWithRuby left, StringWithRuby right)
        {
            Ruby[] newRubies;

            if (right.rubies.Length == 0)
            {
                newRubies = left.rubies;
            }
            else
            {
                var rubyBuilder = new ArrayBuilder<Ruby>(left.rubies.Length + right.rubies.Length);

                foreach(var it in left.rubies)
                {
                    rubyBuilder.Add(it);
                }

                foreach(var it in right.rubies)
                {
                    var ruby = new Ruby(
                        textPosition: it.textPosition + left.joinedRubyText.Length,
                        textLength: it.textLength,
                        bodyStringRange: new RangeInt(it.bodyStringRange.start + left.body.Length, it.bodyStringRange.length));

                    rubyBuilder.Add(ruby);
                }

                newRubies = rubyBuilder.Array;
            }

            Tag[] newTags;
            if (right.tags.Length == 0)
            {
                newTags = left.tags;
            }
            else
            {
                var tagBuilder = new ArrayBuilder<Tag>(left.tags.Length + right.tags.Length);
                foreach(var it in left.tags)
                {
                    tagBuilder.Add(it);
                }
                foreach(var it in right.tags)
                {
                    var t = new Tag(
                        leftIndex: it.leftIndex + left.body.Length,
                        left: it.left,
                        rightIndex: it.rightIndex + left.body.Length,
                        right: it.right);
                    tagBuilder.Add(t);
                }
                newTags = tagBuilder.Array;
            }

            return new StringWithRuby(left.body + right.body, newRubies, left.joinedRubyText + right.joinedRubyText, newTags);
        }

        public StringWithRuby Remove(int startIndex, int count)
        {
            var newBody = body.Remove(startIndex, count);
            var removeRange = new RangeInt(startIndex, count);

            // ルビの移動
            var newJoinedRubyText = new System.Text.StringBuilder();
            var newRubies = new List<Ruby>();
            foreach (var it in rubies)
            {
                var newBodyRange = TrimRange(it.bodyStringRange, removeRange);
                if (newBodyRange.length > 0)
                {
                    var ruby = new Ruby(
                        textPosition: newJoinedRubyText.Length,
                        textLength: it.textLength,
                        bodyStringRange: newBodyRange);
                    newRubies.Add(ruby);
                    newJoinedRubyText.Append(joinedRubyText, it.textPosition, it.textLength);
                }
            }

            // タグの移動
            var newTags = new List<Tag>(tags.Length);
            foreach(var it in tags)
            {
                var range = TrimRange(
                    new RangeInt(it.leftIndex, it.rightIndex - it.leftIndex),
                    removeRange);

                if (range.length > 0)
                {
                    newTags.Add(new Tag(
                            range.start, it.left,
                            range.end, it.right));
                }
            }

            return new StringWithRuby(newBody,
                newRubies.ToArray(),
                newJoinedRubyText.ToString(),
                newTags.ToArray());
        }

        public StringWithRuby Substring(int startIndex, int length)
        {
            // 削除部分に重なっているルビを削除
            var newRubies = rubies
                .Where(_ => _.bodyStringRange.start >= startIndex)
                .Where(_ => _.bodyStringRange.end <= startIndex + length);

            // 完全に範囲外になるタグを削除
            var newTags = tags
                .Where(_ => _.rightIndex > startIndex)
                .Where(_ => _.leftIndex < startIndex + length)
                .Select(_ => new Tag(
                    leftIndex: Mathf.Clamp(_.leftIndex, startIndex, startIndex + length),
                    left: _.left,
                    rightIndex: Mathf.Clamp(_.rightIndex, startIndex, startIndex + length),
                    right: _.right));

            // joinedRubyTextの切り出し
            var rubyTextPosition = newRubies.FirstOrDefault().textPosition;
            var rubyTextLength = newRubies.Sum(_ => _.textLength);
            var newJoinedRubyText = joinedRubyText.Substring(rubyTextPosition, rubyTextLength);

            // 頭を削除するとインデックスがずれる
            if (startIndex > 0)
            {
                newRubies = newRubies
                    .Select(_ => new Ruby(
                        _.textPosition - rubyTextPosition,
                        _.textLength,
                        new RangeInt(_.bodyStringRange.start - startIndex, _.bodyStringRange.length)));

                newTags = newTags
                    .Select(_ => new Tag(
                        _.leftIndex - startIndex,
                        _.left,
                        _.rightIndex - startIndex,
                        _.right));
            }

            var newBody = body.Substring(startIndex, length);

            return new StringWithRuby(newBody, newRubies.ToArray(), newJoinedRubyText, newTags.ToArray());
        }

        public StringWithRuby RemoveRubyAt(int index)
        {
            var newRubies = new Ruby[rubies.Length - 1];
            for(int i=0; i<rubies.Length; ++i)
            {
                if (i < index)
                {
                    newRubies[i] = rubies[i];
                }
                else if (i > index)
                {
                    newRubies[i - 1] = new Ruby(
                        textPosition: rubies[i].textPosition - rubies[index].textLength,
                        textLength: rubies[i].textLength,
                        bodyStringRange: rubies[i].bodyStringRange);
                }
            }

            var newRubyText = joinedRubyText.Remove(rubies[index].textPosition, rubies[index].textLength);

            return new StringWithRuby(body, newRubies, newRubyText, tags);
        }

        public StringWithRuby Insert(int startIndex, StringWithRuby value)
        {
            var newBody = body.Insert(startIndex, value.body);

            // ルビ
            var rubyBuilder = new ArrayBuilder<(RangeInt range, Ruby ruby, string joinedText)>(rubies.Length + value.rubies.Length);
            foreach (var it in rubies)
            {
                if (it.bodyStringRange.end <= startIndex)
                {
                    // 挿入部分より前
                    rubyBuilder.Add((it.bodyStringRange, it, joinedRubyText));
                }
                else if (it.bodyStringRange.start < startIndex
                    && it.bodyStringRange.end >= startIndex)
                {
                    // 挿入部分をまたぐ
                    rubyBuilder.Add((
                        new RangeInt(it.bodyStringRange.start, it.bodyStringRange.length + value.body.Length),
                        it,
                        joinedRubyText));
                }
                else
                {
                    // 挿入部分より後
                    rubyBuilder.Add((
                        new RangeInt(it.bodyStringRange.start + value.body.Length, it.bodyStringRange.length),
                        it,
                        joinedRubyText));
                }
            }

            // 挿入部分
            foreach (var it in value.rubies)
            {
                rubyBuilder.Add((
                    new RangeInt(it.bodyStringRange.start + startIndex, it.bodyStringRange.length),
                    it,
                    value.joinedRubyText));
            }

            var newRubies = new List<Ruby>();
            var newRubyText = new System.Text.StringBuilder();
            foreach (var builder in rubyBuilder.Array.OrderBy(_ => _.range.start))
            {
                newRubies.Add(new Ruby(
                    textPosition: newRubyText.Length,
                    textLength: builder.ruby.textLength,
                    bodyStringRange: builder.range));
                newRubyText.Append(builder.joinedText, builder.ruby.textPosition, builder.ruby.textLength);
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

            return new StringWithRuby(newBody, newRubies.ToArray(), newRubyText.ToString(), newTags.Array);
        }

        public StringWithRuby InsertTag(int leftIndex, string left, int rightIndex, string right)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + 1);

            foreach(var it in tags)
            {
                tagBuilder.Add(it);
            }
            tagBuilder.Add(new Tag(leftIndex, left, rightIndex, right));

            return new StringWithRuby(body, rubies, joinedRubyText, tagBuilder.Array);
        }

        public StringWithRuby InsertTags(params Tag[] array)
        {
            var tagBuilder = new ArrayBuilder<Tag>(tags.Length + array.Length);

            foreach (var it in tags)
            {
                tagBuilder.Add(it);
            }
            foreach (var it in array)
            {
                tagBuilder.Add(it);
            }

            return new StringWithRuby(body, rubies, joinedRubyText, tagBuilder.Array);
        }

        public StringWithRuby FoldTag()
        {
            var tagRanges = new List<RangeInt>();
            var tagElements = new List<(string name, string value, bool closing)>();
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
                    }

                    tagElements.Add((
                        tagName,
                        value: tagString,
                        closing: closingTag));
                }
            }

            if (tagElements.Count == 0)
            {
                return this;
            }

            var pairTags = new ArrayBuilder<Tag>(tagElements.Count / 2);
            {
                var positionOffset = 0;
                var dict = new Dictionary<string, Stack<(string tagValue, int position)>> ();
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

                UnityEngine.Assertions.Assert.AreEqual(0, dict.Sum(_ => _.Value.Count), "invalid tag : " + body);
            }

            // bodyからtag文字列を削除
            var result = this;
            {
                var removedRange = 0;
                foreach (var range in tagRanges)
                {
                    result = result.Remove(range.start - removedRange, range.length);
                    removedRange += range.length;
                }
            }

            result = result.InsertTags(pairTags.Array);

            return result;
        }

        public StringWithRuby UnfoldTag()
        {
            if (tags.Length == 0)
            {
                return this;
            }

            var tagElements = new ArrayBuilder<(int index, string value, int subSort)>(tags.Length * 2);
            for (int i = 0; i < tags.Length; ++i)
            {
                var tag = tags[i];
                tagElements.Add((tag.leftIndex, tag.left, subSort: -i));
                tagElements.Add((tag.rightIndex, tag.right, subSort: i));
            }

            var sortedTags = tagElements.Array
                .OrderByDescending(_ => _.index)
                .ThenByDescending(_ => _.subSort);

            var result = this;
            foreach (var (index, value, _) in sortedTags)
            {
                var tag = new StringWithRuby(value, null, null, null);
                result = result.Insert(index, tag);
            }

            return new StringWithRuby(result.body,
                result.rubies,
                result.joinedRubyText,
                null);
        }

        public static StringWithRuby Parse(string originalText)
        {
            var rubies = new List<Ruby>();
            var bodyText = new System.Text.StringBuilder();
            var rubyText = new System.Text.StringBuilder();

            var currentIndex = 0;
            while (true)
            {
                var beginIndex = originalText.IndexOf('{', currentIndex);
                if (beginIndex < 0)
                {
                    if (rubies.Count == 0)
                    {
                        return new StringWithRuby(originalText, System.Array.Empty<Ruby>(), string.Empty, System.Array.Empty<Tag>());
                    }
                    bodyText.Append(originalText, currentIndex, originalText.Length - currentIndex);
                    break;
                }
                var endIndex = originalText.IndexOf('}', beginIndex);

                bodyText.Append(originalText, currentIndex, beginIndex - currentIndex);
                currentIndex = endIndex + 1;

                var attributes = originalText.Substring(beginIndex + 1, endIndex - beginIndex - 1).Split(':');
                var body = attributes[0];
                var ruby = attributes[1];

                var word = new Ruby(
                    textPosition: rubyText.Length,
                    textLength: ruby.Length,
                    bodyStringRange: new RangeInt(bodyText.Length, body.Length));
                rubies.Add(word);

                bodyText.Append(body);
                rubyText.Append(ruby);
            }

            return new StringWithRuby(bodyText.ToString(), rubies.ToArray(), rubyText.ToString(), System.Array.Empty<Tag>());
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
