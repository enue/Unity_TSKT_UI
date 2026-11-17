#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public readonly struct TypingText
    {
        public int VisibleBodyLength { get; }
        public StringWithRuby Text { get; }

        /// <summary>
        /// 本文とルビを同じテキストに表示している場合（TMP_RubyText）の表示文字数。
        /// 全文を表示したうえでTMP_Text.maxVisibleCharactersに渡す。
        /// </summary>
        public int VisibleLengthWithRuby => VisibleBodyLength + Text.joinedRubyText.Length;

        public TypingText(int visibleBodyLength, StringWithRuby text)
        {
            VisibleBodyLength = visibleBodyLength;
            Text = text;
        }
    }
}
