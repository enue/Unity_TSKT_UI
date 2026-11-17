using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class RubyTextSample : MonoBehaviour
    {
        [SerializeField]
        RubyText? ruby = default;

        [SerializeField]
        Text? body = default;


        [SerializeField]
        RubyText? rubyForTypingMessage = default;

        [SerializeField]
        Text? bodyForTypingMessage = default;


        [SerializeField]
        RubyText? rubyForTypingMessageWithFade = default;

        [SerializeField]
        TypingEffect? rubyTypingEffect = default;

        [SerializeField]
        QuadByQuad?[] bodyTypingEffects = default!;


        [SerializeField]
        TMP_RubyText? textMeshProRuby = default;

        [SerializeField]
        TMP_RubyText? textMeshProRubyForTypingMessage = default;


        void Start()
        {
            var message = "　{吾輩:わがはい}は猫である。名前はまだ無い。\n　どこで生れたかとんと{見<color=red>当:けんとう}がつか</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
            // https://www.aozora.gr.jp/cards/000148/files/789_14547.html
            // https://www.aozora.gr.jp/guide/kijyunn.html
            // 吾輩は猫である
            // 夏目漱石
            // 底本：「夏目漱石全集1」ちくま文庫、筑摩書房
            // 1987（昭和62）年9月29日第1刷発行
            // 入力：柴田卓治
            // 校正：渡部峰子（一）

            ShowStringWithRuby(message);
            ShowStringWithRubyTMP(message);
            StartCoroutine(UpdateTypingMessage(message));
            StartCoroutine(UpdateTypingMessageWithFade(message));
            StartCoroutine(UpdateTypingMessageTMP(message));
        }

        void ShowStringWithRuby(string text)
        {
            var wrapped = RichTextBuilder.Parse(text)
                .WrapWithHyphenation(body!, new HyphenationJpns.Ruler())
                .ToStringWithRuby();
            ruby!.Set(wrapped);
            body!.text = wrapped.body;
        }

        // 本文とルビが別のTextなので、1文字増やすごとに両方の文字列を作り直す
        StringWithRuby[] GetTypingTexts(string text)
        {
            var wrapped = RichTextBuilder.Parse(text)
                .WrapWithHyphenation(body!, new HyphenationJpns.Ruler());

            var result = new List<StringWithRuby>();
            for (int i = 1; i <= wrapped.body.Length; ++i)
            {
                if (wrapped.body[i - 1] != '\n')
                {
                    result.Add(wrapped.Substring(0, i).ToStringWithRuby());
                }
            }
            return result.ToArray();
        }

        IEnumerator UpdateTypingMessage(string text)
        {
            var richTexts = GetTypingTexts(text);

            while (true)
            {
                foreach (var it in richTexts)
                {
                    rubyForTypingMessage!.Set(it);
                    bodyForTypingMessage!.text = it.body;

                    yield return new WaitForSeconds(0.05f);
                }
                yield return new WaitForSeconds(1f);
            }
        }
        IEnumerator UpdateTypingMessageWithFade(string text)
        {
            var stringWithRuby = RichTextBuilder.Parse(text)
                .WrapWithHyphenation(body!, new HyphenationJpns.Ruler())
                .ToStringWithRuby();

            var typingEffect = new RubyTextTypingEffect(stringWithRuby,
                rubyForTypingMessageWithFade!,
                rubyTypingEffect!,
                bodyTypingEffects!);
            while (true)
            {
                var startedTime = Time.time;
                while (true)
                {
                    var elapsedTime = Time.time - startedTime;
                    typingEffect.Update(elapsedTime);

                    if (elapsedTime > typingEffect.duration)
                    {
                        break;
                    }
                    yield return null;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        void ShowStringWithRubyTMP(string message)
        {
            // 本文とルビはTMP_RubyTextが同じテキストにまとめて表示する
            var richText = RichTextBuilder.Parse(message).ToStringWithRuby();
            textMeshProRuby!.Set(richText);
        }

        // 全文をルビ込みで一度だけ設定し、表示文字数だけを進める。
        // レイアウトは変わらないので、途中で文字の位置が動くことはない。
        IEnumerator UpdateTypingMessageTMP(string message)
        {
            var stringWithRuby = RichTextBuilder.Parse(message).ToStringWithRuby();
            textMeshProRubyForTypingMessage!.Set(stringWithRuby);
            var sequence = stringWithRuby.GetTypingSequence();
            var text = textMeshProRubyForTypingMessage.Text;

            while (true)
            {
                foreach (var it in sequence)
                {
                    text.maxVisibleCharacters = it;
                    yield return new WaitForSeconds(0.05f);
                }
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
