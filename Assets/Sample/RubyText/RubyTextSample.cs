﻿using System.Collections;
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
        TMPro.TextMeshProUGUI? textMeshProBody = default;


        [SerializeField]
        TMP_RubyText? textMeshProRubyForTypingMessage = default;

        [SerializeField]
        TMPro.TMP_Text? textMeshProBodyForTypingMessage = default;



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

        IEnumerator UpdateTypingMessage(string text)
        {
            var richTexts = RichTextBuilder.Parse(text)
                .WrapWithHyphenation(body!, new HyphenationJpns.Ruler())
                .GetTypingSequence();

            while (true)
            {
                foreach (var it in richTexts)
                {
                    rubyForTypingMessage!.Set(it.Text);
                    bodyForTypingMessage!.text = it.Text.body;

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
            var richText = RichTextBuilder.Parse(message).ToStringWithRuby();
            textMeshProBody!.text = richText.body;
            textMeshProRuby!.Set(richText, updateMesh: true);
        }

        // ルビを表示すると改行禁則処理でおかしくなるのでいったんルビ表示は諦める。
        // たとえば「ほ\nげ。」と行をまたいで表示すると、
        //「ほ」「ほ\nげ」「ほ\nげ。」　となってほしいが、実際は
        //「ほ」「ほげ」「ほ\nげ。」　となる。（「げ」の位置が途中で変化してしまう）
        // これはTMP_RubyText内で呼んでいるTMP_Text.GetTextInfoの副作用による。
        IEnumerator UpdateTypingMessageTMP(string message)
        {
            var richTexts = RichTextBuilder.Parse(message).GetTypingSequence();
            textMeshProBodyForTypingMessage!.text = richTexts[^1].Text.body;

            while (true)
            {
                foreach (var it in richTexts)
                {
                    textMeshProBodyForTypingMessage.maxVisibleCharacters = it.VisibleBodyLength;
                    textMeshProRubyForTypingMessage.Set(it.Text, updateMesh: true);
                    yield return new WaitForSeconds(0.05f);
                }
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
