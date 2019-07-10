﻿using System.Collections;
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
            readonly public int bodyQuadIndex;
            readonly public int bodyQuadCount;
            readonly public RangeInt bodyStringRange;

            public int RequiredTargetVerticesCount => (bodyQuadIndex + bodyQuadCount) * RubyText.VertexCountPerQuad;

            public Ruby(int textPosition, int textLength, int bodyQuadIndex, int bodyQuadCount, RangeInt bodyStringRange)
            {
                this.textPosition = textPosition;
                this.textLength = textLength;
                this.bodyQuadIndex = bodyQuadIndex;
                this.bodyQuadCount = bodyQuadCount;
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
            var leftBodyQuadCount = CountQuad(left.body);

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
                    bodyQuadIndex: rightWord.bodyQuadIndex + leftBodyQuadCount,
                    bodyQuadCount: rightWord.bodyQuadCount,
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
                var removedBeginningBody = body.Substring(0, startIndex);
                var removedBeginningQuad = CountQuad(removedBeginningBody);

                newRubies = newRubies
                    .Select(_ => new Ruby(
                        _.textPosition - rubyTextPosition,
                        _.textLength,
                        _.bodyQuadIndex - removedBeginningQuad,
                        _.bodyQuadCount,
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
                        bodyQuadIndex: rubies[i].bodyQuadIndex,
                        bodyQuadCount: rubies[i].bodyQuadCount,
                        bodyStringRange: rubies[i].bodyStringRange);
                }
            }

            var newRubyText = joinedRubyText.Remove(rubies[index].textPosition, rubies[index].textLength);

            return new StringWithRuby(body, newRubies, newRubyText, tags);
        }

        public StringWithRuby Insert(int startIndex, StringWithRuby value)
        {
            // 挿入箇所にルビがまたがる場合、ルビは消す

            // 前半部分
            var newRubies = new List<Ruby>();
            foreach (var it in rubies)
            {
                if (it.bodyStringRange.end > startIndex)
                {
                    break;
                }
                newRubies.Add(it);
            }
            var newBody = body.Substring(0, startIndex);
            var newRubyText = new System.Text.StringBuilder();
            newRubyText.Append(joinedRubyText.Substring(0, newRubies.Sum(_ => _.textLength)));

            // 挿入部分
            var QuadCountBeforeStartIndex = CountQuad(newBody);
            foreach (var it in value.rubies)
            {
                newRubies.Add(new Ruby(
                    textPosition: it.textPosition + newRubyText.Length,
                    textLength: it.textLength,
                    bodyQuadIndex: it.bodyQuadIndex + QuadCountBeforeStartIndex,
                    bodyQuadCount: it.bodyQuadCount,
                    bodyStringRange: new RangeInt(it.bodyStringRange.start + startIndex, it.bodyStringRange.length)));
            }
            newBody += value.body;
            newRubyText.Append(value.joinedRubyText);

            // 後半部分
            var valueQuadCount = CountQuad(value.body);
            foreach (var it in rubies)
            {
                if (it.bodyStringRange.start >= startIndex)
                {
                    newRubies.Add(new Ruby(
                        textPosition: newRubyText.Length,
                        textLength: it.textLength,
                        bodyQuadIndex: it.bodyQuadIndex + valueQuadCount,
                        bodyQuadCount: it.bodyQuadCount,
                        bodyStringRange: new RangeInt(it.bodyStringRange.start + value.body.Length, it.bodyStringRange.length)));
                    newRubyText.Append(joinedRubyText.Substring(it.textPosition, it.textLength));
                }
            }
            newBody += body.Substring(startIndex, body.Length - startIndex);

            // tag
            var newTags = new List<Tag>();
            foreach (var it in tags)
            {
                if (it.rightIndex < startIndex)
                {
                    newTags.Add(it);
                }
                else if (it.leftIndex > startIndex)
                {
                    newTags.Add(new Tag(
                        it.leftIndex + value.body.Length,
                        it.left,
                        it.rightIndex + value.body.Length,
                        it.right));
                }
                else
                {
                    newTags.Add(new Tag(
                        it.leftIndex,
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

        public StringWithRuby ParseTag()
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
                var left = result.Substring(0, range.start);
                var right = result.Substring(range.end, result.body.Length - range.end);
                result = Combine(left, right);
            }

            result = result.InsertTags(pairTags.ToArray());

            return result;
        }

        public string TaggedBody
        {
            get
            {
                if (tags.Length == 0)
                {
                    return body;
                }

                var t = new List<(int index, string value, int subSort)>();
                for(int i=0; i<tags.Length; ++i)
                {
                    var tag = tags[i];
                    t.Add((tag.leftIndex, tag.left, subSort: -i));
                    t.Add((tag.rightIndex, tag.right, subSort: i));
                }

                var sortedTags = t
                    .OrderByDescending(_ => _.index)
                    .ThenByDescending(_ => _.subSort);

                var result = body;
                foreach (var (index, value, _) in sortedTags)
                {
                    result = result.Insert(index, value);
                }
                return result;
            }
        }

        public static int CountQuad(string text)
        {
            if (text == null)
            {
                return 0;
            }
            var result = 0;
            foreach (var it in text)
            {
                if (it == '\0')
                {
                    continue;
                }
                if (it == '\t')
                {
                    continue;
                }
                if (it == '\n')
                {
                    // \nがポリゴンを作るかは場合による。
                    // 基本的には作らないが、改行コードが文字列の末尾にある場合は作られるらしい。
                    // ただしこれを厳密に考えると文字列連結時にQuad数を計算するのが面倒になるので、とりあえず常に作らない扱いで数えている。
                    continue;
                }
                if (it == ' ')
                {
                    continue;
                }
                ++result;
            }
            return result;
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
                    bodyQuadIndex: CountQuad(bodyText.ToString()),
                    bodyQuadCount: CountQuad(body),
                    bodyStringRange: new RangeInt(bodyText.Length, body.Length));
                rubies.Add(word);

                bodyText.Append(body);
                rubyText.Append(ruby);
            }

            return new StringWithRuby(bodyText.ToString(), rubies.ToArray(), rubyText.ToString(), System.Array.Empty<Tag>());
        }
    }
}