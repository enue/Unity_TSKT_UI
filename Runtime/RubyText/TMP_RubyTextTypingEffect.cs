using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public readonly struct TMP_RubyTextTypingEffect
    {
        readonly TMP_TypingEffect rubyTypingEffect;
        readonly TMP_QuadByQuad[] bodyTypingEffects;
        readonly int[] bodyQuadCountRubyQuadCountMap;
        readonly public float duration;

        public TMP_RubyTextTypingEffect(in StringWithRuby text, TMP_RubyText ruby, TMP_Text body, TMP_TypingEffect rubyTypingEffect,
            params TMP_QuadByQuad[] bodyTypingEffects)
        {
            this.rubyTypingEffect = rubyTypingEffect;
            this.bodyTypingEffects = bodyTypingEffects;

            ruby.Set(text);
            body.text = text.body;
            ruby.TryGetBodyQuadCountRubyQuadCountMap(out bodyQuadCountRubyQuadCountMap);
            duration = bodyTypingEffects[0].GetDuration(bodyQuadCountRubyQuadCountMap.Length - 1);

            Update(0f);
        }

        public void Update(float elapsedTime)
        {
            foreach (var it in bodyTypingEffects)
            {
                it.elapsedTime = elapsedTime;
            }
            bodyTypingEffects[0].Modifier.Refresh();

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
            rubyTypingEffect.visibleQuadCount = bodyQuadCountRubyQuadCountMap[visibleBodyQuadCount];
            rubyTypingEffect.Modifier.Refresh();
        }
    }
}
