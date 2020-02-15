using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public readonly struct RubyTextTypingEffect
    {
        readonly TypingEffect rubyTypingEffect;
        readonly QuadByQuad[] bodyTypingEffects;
        readonly int[] bodyQuadCountRubyQuadCountMap;
        readonly public float duration;

        public RubyTextTypingEffect(in StringWithRuby text, RubyText ruby, TypingEffect rubyTypingEffect,
            params QuadByQuad[] bodyTypingEffects)
        {
            this.rubyTypingEffect = rubyTypingEffect;
            this.bodyTypingEffects = bodyTypingEffects;

            var body = bodyTypingEffects[0].Text;
            ruby.Set(text);
            body.text = text.body;
            bodyQuadCountRubyQuadCountMap = ruby.GetBodyQuadCountRubyQuadCountMap();
            duration = bodyTypingEffects[0].GetDuration(bodyQuadCountRubyQuadCountMap.Length - 1);

            Update(0f);
        }

        public void Update(float elapsedTime)
        {
            foreach (var it in bodyTypingEffects)
            {
                it.ElapsedTime = elapsedTime;
            }

            int visibleBodyQuadCount;
            if (bodyTypingEffects[0].delayPerQuad == 0f)
            {
                visibleBodyQuadCount = bodyQuadCountRubyQuadCountMap.Length - 1;
            }
            else
            {
                visibleBodyQuadCount = Mathf.Clamp(
                   Mathf.FloorToInt(elapsedTime / bodyTypingEffects[0].delayPerQuad),
                   0,
                   bodyQuadCountRubyQuadCountMap.Length - 1);
            }
            rubyTypingEffect.VisibleQuadCount = bodyQuadCountRubyQuadCountMap[visibleBodyQuadCount];
        }
    }
}
