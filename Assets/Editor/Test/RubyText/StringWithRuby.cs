using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace TSKT.Tests
{
    public class StringWithRuby
    {
        [Test]
        public void Parse()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var text = TSKT.RichTextBuilder.Parse(message);

            Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！", text.body);
            Assert.AreEqual("ハイ・クラスじゃろォ～～～", text.joinedRubyText);

            Assert.AreEqual(2, text.rubies.Length);

            Assert.AreEqual(0, text.rubies[0].textPosition);
            Assert.AreEqual(6, text.rubies[0].textLength);
            Assert.AreEqual(0, text.rubies[0].bodyStringRange.start);
            Assert.AreEqual(3, text.rubies[0].bodyStringRange.length);

            Assert.AreEqual(6, text.rubies[1].textPosition);
            Assert.AreEqual(7, text.rubies[1].textLength);

            Assert.AreEqual(6, text.rubies[1].bodyStringRange.start);
            Assert.AreEqual(6, text.rubies[1].bodyStringRange.length);
        }

        [Test]
        [TestCase("abc{def}ghi", "abc{def}ghi", "")]
        [TestCase("abc{def{}ghi", "abc{def{}ghi", "")]
        [TestCase("abc{def}g:hi", "abc{def}g:hi", "")]
        [TestCase("abc{defghi", "abc{defghi", "")]
        [TestCase("abc{de{f:g}hi", "abcde{fhi", "g")]
        public void Parse2(string source, string body, string ruby)
        {
            var text = TSKT.RichTextBuilder.Parse(source);

            Assert.AreEqual(body, text.body);
            Assert.AreEqual(ruby, text.joinedRubyText);
        }

        [Test]
        public void Combine()
        {
            var hoge = TSKT.RichTextBuilder.Parse("{hoge:ほげ}");
            var fuga = TSKT.RichTextBuilder.Parse("{fuga:ふが}").InsertTag(1, "piyo", 2, "piyo");
            var text = TSKT.RichTextBuilder.Combine(hoge, fuga);

            Assert.AreEqual("ほげふが", text.joinedRubyText);

            Assert.AreEqual(2, text.rubies.Length);

            Assert.AreEqual(hoge.rubies[0].textPosition, text.rubies[0].textPosition);
            Assert.AreEqual(hoge.rubies[0].textLength, text.rubies[0].textLength);

            Assert.AreEqual(fuga.rubies[0].textPosition + 2, text.rubies[1].textPosition);
            Assert.AreEqual(fuga.rubies[0].textLength, text.rubies[1].textLength);
            Assert.AreEqual(4, text.rubies[1].bodyStringRange.start);
            Assert.AreEqual(4, text.rubies[1].bodyStringRange.length);

            Assert.AreEqual(1, text.tags.Length);
            Assert.AreEqual(5, text.tags[0].leftIndex);
        }
        [Test]
        public void Substring()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var text = TSKT.RichTextBuilder.Parse(message)
                .InsertTags(
                    new TSKT.RichTextBuilder.Tag(2, "tag1", 3, "tag1"),
                    new TSKT.RichTextBuilder.Tag(6, "tag2", 7, "tag2"),
                    new TSKT.RichTextBuilder.Tag(12, "tag3", 13, "tag3"));
            var sub = text.Substring(2, 10);

            Assert.AreEqual("だ！！\n邪神広告機構", sub.body);
            Assert.AreEqual("じゃろォ～～～", sub.joinedRubyText);

            Assert.AreEqual(1, sub.rubies.Length);

            Assert.AreEqual(0, sub.rubies[0].textPosition);
            Assert.AreEqual(7, sub.rubies[0].textLength);

            Assert.AreEqual(4, sub.rubies[0].bodyStringRange.start);
            Assert.AreEqual(6, sub.rubies[0].bodyStringRange.length);

            Assert.AreEqual(2, sub.tags.Length);
            Assert.AreEqual(0, sub.tags[0].leftIndex);
            Assert.AreEqual(4, sub.tags[1].leftIndex);
        }
        [Test]
        public void RemoveRubyAt()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var text = TSKT.RichTextBuilder.Parse(message);

            {
                var removed = text.RemoveRubyAt(0);

                Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！", removed.body);
                Assert.AreEqual("じゃろォ～～～", removed.joinedRubyText);

                Assert.AreEqual(1, removed.rubies.Length);

                Assert.AreEqual(0, removed.rubies[0].textPosition);
                Assert.AreEqual(7, removed.rubies[0].textLength);

                Assert.AreEqual(6, removed.rubies[0].bodyStringRange.start);
                Assert.AreEqual(6, removed.rubies[0].bodyStringRange.length);
            }
            {
                var removed = text.RemoveRubyAt(1);

                Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！", removed.body);
                Assert.AreEqual("ハイ・クラス", removed.joinedRubyText);

                Assert.AreEqual(1, removed.rubies.Length);

                Assert.AreEqual(0, removed.rubies[0].textPosition);
                Assert.AreEqual(6, removed.rubies[0].textLength);

                Assert.AreEqual(0, removed.rubies[0].bodyStringRange.start);
                Assert.AreEqual(3, removed.rubies[0].bodyStringRange.length);
            }
        }
        [Test]
        public void Insert()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var text = TSKT.RichTextBuilder.Parse(message)
                .InsertTag(3, "hoge", 4, "/hoge");

            {
                var inserted = text.Insert(0, text);
                Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！上等だ！！\n邪神広告機構～～～ッ！！", inserted.body);
                Assert.AreEqual("ハイ・クラスじゃろォ～～～ハイ・クラスじゃろォ～～～", inserted.joinedRubyText);
            }
            {
                var inserted = text.Insert(3, text);
                Assert.AreEqual("上等だ上等だ！！\n邪神広告機構～～～ッ！！！！\n邪神広告機構～～～ッ！！", inserted.body);
                Assert.AreEqual("ハイ・クラスハイ・クラスじゃろォ～～～じゃろォ～～～", inserted.joinedRubyText);
            }
            {
                var inserted = text.Insert(2, text);
                Assert.AreEqual("上等上等だ！！\n邪神広告機構～～～ッ！！だ！！\n邪神広告機構～～～ッ！！", inserted.body);
                Assert.AreEqual("ハイ・クラスハイ・クラスじゃろォ～～～じゃろォ～～～", inserted.joinedRubyText);

                Assert.AreEqual(4, inserted.rubies.Length);

                Assert.AreEqual(0, inserted.rubies[0].textPosition);
                Assert.AreEqual(6, inserted.rubies[0].textLength);
                Assert.AreEqual(0, inserted.rubies[0].bodyStringRange.start);
                Assert.AreEqual(21, inserted.rubies[0].bodyStringRange.length);

                Assert.AreEqual(6, inserted.rubies[1].textPosition);
                Assert.AreEqual(6, inserted.rubies[1].textLength);
                Assert.AreEqual(2, inserted.rubies[1].bodyStringRange.start);
                Assert.AreEqual(3, inserted.rubies[1].bodyStringRange.length);

                Assert.AreEqual(12, inserted.rubies[2].textPosition);
                Assert.AreEqual(7, inserted.rubies[2].textLength);
                Assert.AreEqual(8, inserted.rubies[2].bodyStringRange.start);
                Assert.AreEqual(6, inserted.rubies[2].bodyStringRange.length);

                Assert.AreEqual(19, inserted.rubies[3].textPosition);
                Assert.AreEqual(7, inserted.rubies[3].textLength);
                Assert.AreEqual(24, inserted.rubies[3].bodyStringRange.start);
                Assert.AreEqual(6, inserted.rubies[3].bodyStringRange.length);

                Assert.AreEqual(2, inserted.tags.Length);
                Assert.AreEqual(21, inserted.tags[0].leftIndex);
                Assert.AreEqual(5, inserted.tags[1].leftIndex);
            }
        }
        [Test]
        public void Remove()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var text = TSKT.RichTextBuilder.Parse(message)
                .Remove(0, 3);
            Assert.AreEqual("！！\n邪神広告機構～～～ッ！！", text.body);
            Assert.AreEqual(1, text.rubies.Length);
            Assert.AreEqual(0, text.rubies[0].textPosition);
            Assert.AreEqual(7, text.rubies[0].textLength);
            Assert.AreEqual(3, text.rubies[0].bodyStringRange.start);
            Assert.AreEqual(6, text.rubies[0].bodyStringRange.length);
        }

        [Test]
        public void Tag()
        {
            var message = "hoge";
            var text = TSKT.RichTextBuilder.Parse(message)
                .InsertTag(1, "<piyo>", 2, "<fuga>");

            Assert.AreEqual("h<piyo>o<fuga>ge", text.ToStringWithRuby().body);

            {
                var combined = TSKT.RichTextBuilder.Combine(text, text);
                Assert.AreEqual("h<piyo>o<fuga>geh<piyo>o<fuga>ge", combined.ToStringWithRuby().body);
            }
            {
                var sub = text.Substring(2, 2);
                Assert.AreEqual("ge", sub.ToStringWithRuby().body);
            }
            {
                var sub = text.Substring(0, 1);
                Assert.AreEqual("h", sub.ToStringWithRuby().body);
            }
            {
                var sub = text.Substring(1, 1);
                Assert.AreEqual("<piyo>o<fuga>", sub.ToStringWithRuby().body);
            }
            {
                var sub = text.Substring(1, 3);
                Assert.AreEqual("<piyo>o<fuga>ge", sub.ToStringWithRuby().body);
            }
            {
                var sub = text.Substring(0, 2);
                Assert.AreEqual("h<piyo>o<fuga>", sub.ToStringWithRuby().body);
            }
            {
                var inserted = text.Insert(1, text);
                Assert.AreEqual("hh<piyo>o<fuga>ge<piyo>o<fuga>ge", inserted.ToStringWithRuby().body);
            }
            {
                var inserted = text.Insert(2, text);
                Assert.AreEqual("h<piyo>o<fuga>h<piyo>o<fuga>gege", inserted.ToStringWithRuby().body);
            }
            {
                var inserted = text.Insert(3, text);
                Assert.AreEqual("h<piyo>o<fuga>gh<piyo>o<fuga>gee", inserted.ToStringWithRuby().body);
            }
        }
        [Test]
        [TestCase("<fuga>hoge</ fuga>", "hoge", "<fuga>hoge</ fuga>")]
        [TestCase("<b><a>hoge</ a></b>", "hoge", "<b><a>hoge</ a></b>")]
        [TestCase("<b><a>hoge</b></ a>", "hoge", "<a><b>hoge</b></ a>")]
        [TestCase("<a>hoge<b>fuga</ a>piyo</b>", "hogefugapiyo", "<a>hoge<b>fuga</ a>piyo</b>")]
        [TestCase("hoge>", "hoge>", "hoge>")]
        [TestCase("<hoge", "<hoge", "<hoge")]
        [TestCase("<color=red>hoge</color>", "hoge", "<color=red>hoge</color>")]
        [TestCase("<space=1em>hoge", "hoge", "<space=1em>hoge")]
        public void ToStringWithRuby(string originalString, string body, string taggedBody)
        {
            var text = TSKT.RichTextBuilder.Parse(originalString);

            Assert.AreEqual(body, text.body);
            Assert.AreEqual(taggedBody, text.ToStringWithRuby().body);
        }

        [Test]
        public void ParseOption()
        {
            var message = "{hoge:fuga}<color=red>piyo</color>";
            var trueTrue = RichTextBuilder.Parse(message, tag: true, ruby: true);
            Assert.AreEqual("hogepiyo", trueTrue.body);
            var falseTrue = RichTextBuilder.Parse(message, tag: false, ruby: true);
            Assert.AreEqual("hoge<color=red>piyo</color>", falseTrue.body);
            var trueFalse = RichTextBuilder.Parse(message, tag: true, ruby: false);
            Assert.AreEqual("{hoge:fuga}piyo", trueFalse.body);
            var falseFalse = RichTextBuilder.Parse(message, tag: false, ruby: false);
            Assert.AreEqual(message, falseFalse.body);
        }
    }
}
