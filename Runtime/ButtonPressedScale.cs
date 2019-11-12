using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TSKT
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class ButtonPressedScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Button button;
        Button Button => button ?? (button = GetComponent<Button>());

        Tweens.Scale tween;

        [SerializeField]
        Vector3 scale = new Vector3(0.9f, 0.9f, 0.9f);

        [SerializeField]
        float tweenDuration = 0.1f;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                tween?.Halt();
                tween = Tween.Scale(gameObject, tweenDuration, scaledTime: false)
                    .To(Vector3.one);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                && Button.interactable
                && Button.isActiveAndEnabled)
            {
                tween?.Halt();
                tween = Tween.Scale(gameObject, tweenDuration, scaledTime: false)
                    .To(scale);
            }
        }
    }
}
