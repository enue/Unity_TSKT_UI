using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TSKT
{
    [RequireComponent(typeof(Selectable))]
    public class ButtonColorTintSync : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        Selectable selectable;
        Selectable Selectable => selectable ? selectable : (selectable = GetComponent<Selectable>());

        Color currentColor = Color.white;
        bool isPointerDown = false;
        bool isPointerInside = false;
        bool hasSelect = false;

        [SerializeField]
        Graphic[] targets = default;

        bool[] updatedTargets = null;
        bool[] UpdatedTargets
        {
            get
            {
                if (updatedTargets == null)
                {
                    updatedTargets = new bool[targets.Length];
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        updatedTargets[i] = false;
                    }
                }
                return updatedTargets;
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < UpdatedTargets.Length; ++i)
            {
                UpdatedTargets[i] = false;
            }
        }

        void Update()
        {
            var colorBlock = Selectable.colors;
            Color color;
            var tweenInstant = false;
            if (!Selectable.interactable)
            {
                color = colorBlock.disabledColor;
                tweenInstant = true;
            }
            else if (isPointerDown)
            {
                color = colorBlock.pressedColor;
            }
            else if (hasSelect)
            {
                color = colorBlock.selectedColor;
            }
            else if (isPointerInside)
            {
                color = colorBlock.highlightedColor;
            }
            else
            {
                color = colorBlock.normalColor;
            }

            var shouldModify = (currentColor != color);
            currentColor = color;
            var fadeDuration = tweenInstant ? 0f : colorBlock.fadeDuration;
            for (int i = 0; i < targets.Length; ++i)
            {
                var target = targets[i];
                if (target.isActiveAndEnabled)
                {
                    if (shouldModify || !UpdatedTargets[i])
                    {
                        target.CrossFadeColor(currentColor, fadeDuration, true, true);
                    }
                    UpdatedTargets[i] = true;
                }
                else
                {
                    UpdatedTargets[i] = false;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isPointerDown = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isPointerDown = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasSelect = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasSelect = false;
        }
    }
}
