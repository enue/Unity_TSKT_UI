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
            var ruby = TSKT.StringWithRuby.Parse(message);

            Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！", ruby.body);
            Assert.AreEqual("ハイ・クラスじゃろォ～～～", ruby.joinedRubyText);

            Assert.AreEqual(2, ruby.rubies.Length);

            Assert.AreEqual(0, ruby.rubies[0].textPosition);
            Assert.AreEqual(6, ruby.rubies[0].textLength);
            Assert.AreEqual(0, ruby.rubies[0].bodyStringRange.start);
            Assert.AreEqual(3, ruby.rubies[0].bodyStringRange.length);

            Assert.AreEqual(6, ruby.rubies[1].textPosition);
            Assert.AreEqual(7, ruby.rubies[1].textLength);

            Assert.AreEqual(6, ruby.rubies[1].bodyStringRange.start);
            Assert.AreEqual(6, ruby.rubies[1].bodyStringRange.length);
        }

        [Test]
        public void Combine()
        {
            var hoge = TSKT.StringWithRuby.Parse("{hoge:ほげ}");
            var fuga = TSKT.StringWithRuby.Parse("{fuga:ふが}").InsertTag(1, "piyo", 2, "piyo");
            var ruby = TSKT.StringWithRuby.Combine(hoge, fuga);

            Assert.AreEqual("ほげふが", ruby.joinedRubyText);

            Assert.AreEqual(2, ruby.rubies.Length);

            Assert.AreEqual(hoge.rubies[0].textPosition, ruby.rubies[0].textPosition);
            Assert.AreEqual(hoge.rubies[0].textLength, ruby.rubies[0].textLength);

            Assert.AreEqual(fuga.rubies[0].textPosition + 2, ruby.rubies[1].textPosition);
            Assert.AreEqual(fuga.rubies[0].textLength, ruby.rubies[1].textLength);
            Assert.AreEqual(4, ruby.rubies[1].bodyStringRange.start);
            Assert.AreEqual(4, ruby.rubies[1].bodyStringRange.length);

            Assert.AreEqual(1, ruby.tags.Length);
            Assert.AreEqual(5, ruby.tags[0].leftIndex);
        }
        [Test]
        public void Substring()
        {
            var message = "{上等だ:ハイ・クラス}！！\n{邪神広告機構:じゃろォ～～～}～～～ッ！！";
            var ruby = TSKT.StringWithRuby.Parse(message)
                .InsertTags(
                    new TSKT.StringWithRuby.Tag(2, "tag1", 3, "tag1"),
                    new TSKT.StringWithRuby.Tag(6, "tag2", 7, "tag2"),
                    new TSKT.StringWithRuby.Tag(12, "tag3", 13, "tag3"));
            var sub = ruby.Substring(2, 10);

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
            var ruby = TSKT.StringWithRuby.Parse(message);

            {
                var removed = ruby.RemoveRubyAt(0);

                Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！", removed.body);
                Assert.AreEqual("じゃろォ～～～", removed.joinedRubyText);

                Assert.AreEqual(1, removed.rubies.Length);

                Assert.AreEqual(0, removed.rubies[0].textPosition);
                Assert.AreEqual(7, removed.rubies[0].textLength);

                Assert.AreEqual(6, removed.rubies[0].bodyStringRange.start);
                Assert.AreEqual(6, removed.rubies[0].bodyStringRange.length);
            }
            {
                var removed = ruby.RemoveRubyAt(1);

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
            var ruby = TSKT.StringWithRuby.Parse(message)
                .InsertTag(3, "hoge", 4, "/hoge");

            {
                var inserted = ruby.Insert(0, ruby);
                Assert.AreEqual("上等だ！！\n邪神広告機構～～～ッ！！上等だ！！\n邪神広告機構～～～ッ！！", inserted.body);
                Assert.AreEqual("ハイ・クラスじゃろォ～～～ハイ・クラスじゃろォ～～～", inserted.joinedRubyText);
            }
            {
                var inserted = ruby.Insert(3, ruby);
                Assert.AreEqual("上等だ上等だ！！\n邪神広告機構～～～ッ！！！！\n邪神広告機構～～～ッ！！", inserted.body);
                Assert.AreEqual("ハイ・クラスハイ・クラスじゃろォ～～～じゃろォ～～～", inserted.joinedRubyText);
            }
            {
                var inserted = ruby.Insert(2, ruby);
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
            var ruby = TSKT.StringWithRuby.Parse(message)
                .Remove(0, 3);
            Assert.AreEqual("！！\n邪神広告機構～～～ッ！！", ruby.body);
            Assert.AreEqual(1, ruby.rubies.Length);
            Assert.AreEqual(0, ruby.rubies[0].textPosition);
            Assert.AreEqual(7, ruby.rubies[0].textLength);
            Assert.AreEqual(3, ruby.rubies[0].bodyStringRange.start);
            Assert.AreEqual(6, ruby.rubies[0].bodyStringRange.length);
        }

        [Test]
        public void UnfoldTag()
        {
            var message = "{吾輩:わがはい}は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと{見当:けんとう}がつ<color=red>か</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
            var ruby = TSKT.StringWithRuby.Parse(message).FoldTag().UnfoldTag();
            Assert.AreEqual("吾輩は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと見当がつ<color=red>か</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。",
                ruby.body);

            Assert.AreEqual(2, ruby.rubies.Length);

            Assert.AreEqual(0, ruby.rubies[0].bodyStringRange.start);
            Assert.AreEqual(2, ruby.rubies[0].bodyStringRange.length);

            Assert.AreEqual(46, ruby.rubies[1].bodyStringRange.start);
            Assert.AreEqual(2, ruby.rubies[1].bodyStringRange.length);
        }

        [Test]
        public void Tag()
        {
            var message = "hoge";
            var value = TSKT.StringWithRuby.Parse(message)
                .InsertTag(1, "<piyo>", 2, "<fuga>");

            Assert.AreEqual("h<piyo>o<fuga>ge", value.UnfoldTag().body);

            {
                var combined = TSKT.StringWithRuby.Combine(value, value);
                Assert.AreEqual("h<piyo>o<fuga>geh<piyo>o<fuga>ge", combined.UnfoldTag().body);
            }
            {
                var sub = value.Substring(2, 2);
                Assert.AreEqual("ge", sub.UnfoldTag().body);
            }
            {
                var sub = value.Substring(0, 1);
                Assert.AreEqual("h", sub.UnfoldTag().body);
            }
            {
                var sub = value.Substring(1, 1);
                Assert.AreEqual("<piyo>o<fuga>", sub.UnfoldTag().body);
            }
            {
                var sub = value.Substring(1, 3);
                Assert.AreEqual("<piyo>o<fuga>ge", sub.UnfoldTag().body);
            }
            {
                var sub = value.Substring(0, 2);
                Assert.AreEqual("h<piyo>o<fuga>", sub.UnfoldTag().body);
            }
            {
                var inserted = value.Insert(1, value);
                Assert.AreEqual("hh<piyo>o<fuga>ge<piyo>o<fuga>ge", inserted.UnfoldTag().body);
            }
            {
                var inserted = value.Insert(2, value);
                Assert.AreEqual("h<piyo>o<fuga>h<piyo>o<fuga>gege", inserted.UnfoldTag().body);
            }
            {
                var inserted = value.Insert(3, value);
                Assert.AreEqual("h<piyo>o<fuga>gh<piyo>o<fuga>gee", inserted.UnfoldTag().body);
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
        public void FoldTag(string originalString, string body, string taggedBody)
        {
            var value = TSKT.StringWithRuby.Parse(originalString).FoldTag();

            Assert.AreEqual(body, value.body);
            Assert.AreEqual(taggedBody, value.UnfoldTag().body);
        }
    }
}
