#nullable enable
using System.Collections;
using UnityEngine;

namespace TSKT
{
    /// <summary>
    /// TMP_RubyTextのサンプル。同じGameObjectのTMP_RubyTextに、ルビ記法（{本文:ルビ}）の文章を表示する。
    /// </summary>
    [RequireComponent(typeof(TMP_RubyText))]
    public class TMP_RubyTextSampleItem : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>全文をそのまま表示する</summary>
            Static,
            /// <summary>maxVisibleCharactersで1文字ずつ表示する</summary>
            Typing,
            /// <summary>TMP_QuadByQuadで1文字ずつエフェクトをかけて表示する</summary>
            QuadByQuad,
        }

        [SerializeField]
        [TextArea(2, 8)]
        string message = "";

        [SerializeField]
        Mode mode = Mode.Static;

        [SerializeField]
        float typingInterval = 0.05f;

        [SerializeField]
        float loopInterval = 1f;

        TMP_RubyText? ruby;
        TMP_RubyText Ruby => ruby ? ruby! : (ruby = GetComponent<TMP_RubyText>());

        void OnEnable()
        {
            StartCoroutine(mode switch
            {
                Mode.Typing => Typing(),
                Mode.QuadByQuad => QuadByQuad(),
                _ => Static(),
            });
        }

        /// <summary>エディタ上でシーンを組むときにも使えるよう、全文の表示だけを行う</summary>
        public void Apply()
        {
            Ruby.Set(RichTextBuilder.Parse(message).ToStringWithRuby());
        }

        IEnumerator Static()
        {
            Apply();
            yield break;
        }

        IEnumerator Typing()
        {
            // 全文をルビ込みで一度だけ設定し、表示文字数だけを進める
            var sequence = RichTextBuilder.Parse(message).GetTypingSequence();
            if (sequence.Length == 0)
            {
                yield break;
            }
            Ruby.Set(sequence[^1].Text);

            while (true)
            {
                foreach (var it in sequence)
                {
                    Ruby.Text.maxVisibleCharacters = it.VisibleLengthWithRuby;
                    yield return new WaitForSeconds(typingInterval);
                }
                yield return new WaitForSeconds(loopInterval);
            }
        }

        IEnumerator QuadByQuad()
        {
            Apply();
            var effect = GetComponent<TMP_QuadByQuad>();
            if (!effect)
            {
                yield break;
            }

            var text = Ruby.Text;
            text.ForceMeshUpdate();
            var duration = effect.GetDuration(text.textInfo.characterCount);

            while (true)
            {
                var startedTime = Time.time;
                while (true)
                {
                    effect.elapsedTime = Time.time - startedTime;
                    // エフェクトは頂点を直接書き換えるので、毎回メッシュを作り直してから適用する
                    text.ForceMeshUpdate();
                    effect.Modifier.Refresh();

                    if (effect.elapsedTime > duration)
                    {
                        break;
                    }
                    yield return null;
                }
                yield return new WaitForSeconds(loopInterval);
            }
        }
    }
}
