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
            var rubies = new Ruby[left.rubies.Length + right.rubies.Length];

            for (int i = 0; i < left.rubies.Length; ++i)
            {
                rubies[i] = left.rubies[i];
            }

            for (int i = 0; i < right.rubies.Length; ++i)
            {
                var dest = left.rubies.Length + i;
                var rightWord = right.rubies[i];
                var ruby = new Ruby(
                    textPosition: rightWord.textPosition + left.joinedRubyText.Length,
                    textLength: rightWord.textLength,
                    bodyStringRange: new RangeInt(rightWord.bodyStringRange.start + left.body.Length, rightWord.bodyStringRange.length));
                rubies[dest] = ruby;
            }

            var newTags = new Tag[left.tags.Length + right.tags.Length];
            for (int i = 0; i < left.tags.Length; ++i)
            {
                newTags[i] = left.tags[i];
            }
            for (int i = 0; i < right.tags.Length; ++i)
            {
                newTags[i + left.tags.Length] = new Tag(
                    leftIndex: right.tags[i].leftIndex + left.body.Length,
                    left: right.tags[i].left,
                    rightIndex: right.tags[i].rightIndex + left.body.Length,
                    right: right.tags[i].right);
            }

            return new StringWithRuby(left.body + right.body, rubies, left.joinedRubyText + right.joinedRubyText, newTags);
        }

        public StringWithRuby Remove(int startIndex, int count)
        {
            var newBody = body.Remove(startIndex, count);

            // ルビの移動
            var rubyBuilders = new List<(RangeInt bodyRange, Ruby original)>();
            foreach(var it in rubies)
            {
                var range = TrimRange(it.bodyStringRange,
                    new RangeInt(startIndex, count));
                if (range.length > 0)
                {
                    rubyBuilders.Add((range, it));
                }
            }

            // タグの移動
            var newTags = new List<Tag>();
            foreach(var it in tags)
            {
                var range = TrimRange(
                    new RangeInt(it.leftIndex, it.rightIndex - it.leftIndex),
                    new RangeInt(startIndex, count));
                if (range.length > 0)
                {
                    newTags.Add(new Tag(
                            range.start, it.left,
                            range.end, it.right));
                }
            }

            // ルビの再生成
            var newJoinedRubyText = new System.Text.StringBuilder();
            var newRubies = new List<Ruby>();
            foreach(var it in rubyBuilders)
            {
                var ruby = new Ruby(
                    textPosition: newJoinedRubyText.Length,
                    textLength: it.original.textLength,
                    bodyStringRange: it.bodyRange);
                newRubies.Add(ruby);
                newJoinedRubyText.Append(joinedRubyText, it.original.textPosition, it.original.textLength);
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
            var rubyBuilder = new List<(RangeInt range, Ruby ruby, string joinedText)>();
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
            foreach (var builder in rubyBuilder.OrderBy(_ => _.range.start))
            {
                newRubies.Add(new Ruby(
                    textPosition: newRubyText.Length,
                    textLength: builder.ruby.textLength,
                    bodyStringRange: builder.range));
                newRubyText.Append(builder.joinedText, builder.ruby.textPosition, builder.ruby.textLength);
            }

            // tag
            var newTags = new List<Tag>();
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

            return new StringWithRuby(newBody, newRubies.ToArray(), newRubyText.ToString(), newTags.ToArray());
        }

        public StringWithRuby InsertTag(int leftIndex, string left, int rightIndex, string right)
        {
            var tags = new List<Tag>(this.tags);
            tags.Add(new Tag(leftIndex, left, rightIndex, right));

            return new StringWithRuby(body, rubies, joinedRubyText, tags.ToArray());
        }

        public StringWithRuby InsertTags(params Tag[] tags)
        {
            var t = new List<Tag>(this.tags);
            t.AddRange(tags);

            return new StringWithRuby(body, rubies, joinedRubyText, t.ToArray());
        }

        public StringWithRuby FoldTag()
        {
            var tagRanges = new List<RangeInt>();
            var tags = new List<(string name, string value, bool closing)>();
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

                    tags.Add((
                        tagName,
                        value: tagString,
                        closing: closingTag));
                }
            }

            if (tags.Count == 0)
            {
                return this;
            }

            // 削除後の位置計算
            var tagInsertPositions = new List<int>();
            {
                var removeLength = 0;
                foreach (var it in tagRanges)
                {
                    tagInsertPositions.Add(it.start - removeLength);
                    removeLength += it.length;
                }
            }

            var pairTags = new List<Tag>();
            {
                var dict = new Dictionary<string, Stack<((string name, string value, bool closing) tag, int position)>> ();
                var tagIndices = tags.Zip(tagInsertPositions, (t, p) => (tag: t, position: p));
                foreach (var it in tagIndices)
                {
                    if (it.tag.closing)
                    {
                        dict.TryGetValue(it.tag.name, out var stack);
                        var left = stack.Pop();

                        pairTags.Add(new Tag(
                            left.position,
                            left.tag.value,
                            it.position,
                            it.tag.value));
                    }
                    else
                    {
                        if (!dict.TryGetValue(it.tag.name, out var stack))
                        {
                            stack = new Stack<((string name, string value, bool closing) tag, int position)>();
                            dict.Add(it.tag.name, stack);
                        }
                        stack.Push(it);
                    }
                }

                UnityEngine.Assertions.Assert.AreEqual(0, dict.Sum(_ => _.Value.Count), "invalid tag : " + body);
            }

            // bodyからtag文字列を削除
            tagRanges.Reverse();
            var result = this;
            foreach (var range in tagRanges)
            {
                result = result.Remove(range.start, range.length);
            }

            result = result.InsertTags(pairTags.ToArray());

            return result;
        }

        public StringWithRuby UnfoldTag()
        {
            if (tags.Length == 0)
            {
                return this;
            }

            var t = new List<(int index, string value, int subSort)>();
            for (int i = 0; i < tags.Length; ++i)
            {
                var tag = tags[i];
                t.Add((tag.leftIndex, tag.left, subSort: -i));
                t.Add((tag.rightIndex, tag.right, subSort: i));
            }

            var sortedTags = t
                .OrderByDescending(_ => _.index)
                .ThenByDescending(_ => _.subSort);

            var result = this;
            foreach (var (index, value, _) in sortedTags)
            {
                var tag = Parse(value);
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
