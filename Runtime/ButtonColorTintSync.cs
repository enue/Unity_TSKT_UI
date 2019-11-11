using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TSKT
{
    [RequireComponent(typeof(Button))]
    public class ButtonColorTintSync : MonoBehaviour
    {
        Button button;
        Button Button => button ?? (button = GetComponent<Button>());

        Color currentColor = Color.white;

        [SerializeField]
        Graphic[] targets = default;

        void Update()
        {
            var colorBlock = Button.colors;
            var color = Button.interactable ? colorBlock.normalColor : colorBlock.disabledColor;
            if (currentColor != color)
            {
                var fadeDuration = colorBlock.fadeDuration;
                currentColor = color;
                foreach (var target in targets)
                {
                    target.CrossFadeColor(currentColor, fadeDuration, true, true);
                }
            }
        }
    }
}
