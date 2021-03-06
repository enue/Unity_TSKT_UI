﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TSKT
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class ButtonPressedScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        Selectable selectable;
        Selectable Selectable => selectable ? selectable : (selectable = GetComponent<Selectable>());

        [SerializeField]
        Vector3 scale = new Vector3(0.9f, 0.9f, 0.9f);

        [SerializeField]
        Vector3 hoveredScale = new Vector3(1.1f, 1.1f, 1.1f);

        [SerializeField]
        float tweenDuration = 0.1f;

        bool pressed;
        bool hovered;
        Tweens.Scale tween;
        Vector3 toScale = Vector3.one;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                pressed = false;
                Refresh();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                && Selectable.interactable
                && Selectable.isActiveAndEnabled)
            {
                pressed = true;
                Refresh();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Selectable.interactable
                && Selectable.isActiveAndEnabled)
            {
                hovered = true;
                Refresh();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh();
        }

        void Refresh()
        {
            Vector3 to;
            if (pressed)
            {
                to = scale;
            }
            else if (hovered)
            {
                to = hoveredScale;
            }
            else
            {
                to = Vector3.one;
            }

            if (toScale != to)
            {
                toScale = to;
                tween?.Halt();
                tween = Tween.Scale(gameObject, tweenDuration, scaledTime: false)
                    .To(to);
            }
        }
    }
}
