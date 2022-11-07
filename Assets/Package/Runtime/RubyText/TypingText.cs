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

        public TypingText(int visibleBodyLength, StringWithRuby text)
        {
            VisibleBodyLength = visibleBodyLength;
            Text = text;
        }
    }
}
