using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TSKT
{
    public class ButtonPressedScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Tweens.Scale tween;

        [SerializeField]
        Vector3 scale = new Vector3(0.9f, 0.9f, 0.9f);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                tween?.Halt();
                tween = Tween.Scale(gameObject, 0.1f, scaledTime: false)
                    .To(Vector3.one);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                tween?.Halt();
                tween = Tween.Scale(gameObject, 0.1f, scaledTime: false)
                    .To(scale);
            }
        }
    }
}
