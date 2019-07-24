using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public readonly struct RubyTextTypingEffect
    {
        readonly TypingEffect rubyTypingEffect;
        readonly QuadByQuad bodyTypingEffect;
        readonly int[] bodyQuadCountRubyQuadCountMap;
        readonly public float duration;

        public RubyTextTypingEffect(StringWithRuby text, RubyText ruby, Text body, TypingEffect rubyTypingEffect, QuadByQuad bodyTypingEffect)
        {
            this.rubyTypingEffect = rubyTypingEffect;
            this.bodyTypingEffect = bodyTypingEffect;

            ruby.Set(text);
            body.text = text.body;
            bodyQuadCountRubyQuadCountMap = ruby.GetBodyQuadCountRubyQuadCountMap();
            duration = bodyTypingEffect.GetDuration(bodyQuadCountRubyQuadCountMap.Length - 1);

            Update(0f);
        }

        public void Update(float elapsedTime)
        {
            bodyTypingEffect.ElapsedTime = elapsedTime;

            var visibleBodyQuadCount = Mathf.Clamp(
               Mathf.FloorToInt(elapsedTime / bodyTypingEffect.delayPerQuad),
               0,
               bodyQuadCountRubyQuadCountMap.Length - 1);
            rubyTypingEffect.VisibleQuadCount = bodyQuadCountRubyQuadCountMap[visibleBodyQuadCount];
        }
    }
}
