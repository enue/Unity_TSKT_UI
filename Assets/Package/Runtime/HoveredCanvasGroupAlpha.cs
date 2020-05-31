using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace TSKT
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HoveredCanvasGroupAlpha : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        CanvasGroup canvasGroup;
        CanvasGroup CanvasGroup => canvasGroup  ? canvasGroup: (canvasGroup = GetComponent<CanvasGroup>());

        [SerializeField]
        float duration = 0.1f;

        [SerializeField]
        [Range(0f, 1f)]
        float normalAlpha = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        float hoverAlpha = 0.33f;

        Tweens.Value tween;
        float? toAlpha;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Cursor.visible)
            {
                return;
            }

            if (toAlpha == hoverAlpha)
            {
                return;
            }
            toAlpha = hoverAlpha;
            tween?.Halt();
            tween = Tween.Value(gameObject, duration, scaledTime: false)
                .From(CanvasGroup.alpha)
                .To(hoverAlpha)
                .Callback(_ => CanvasGroup.alpha = _);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (toAlpha == normalAlpha)
            {
                return;
            }

            toAlpha = normalAlpha;
            tween?.Halt();
            tween = Tween.Value(gameObject, duration, scaledTime: false)
                .From(CanvasGroup.alpha)
                .To(normalAlpha)
                .Callback(_ => CanvasGroup.alpha = _);
        }
    }
}
