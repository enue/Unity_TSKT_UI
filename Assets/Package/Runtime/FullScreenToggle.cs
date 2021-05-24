using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class FullScreenToggle : MonoBehaviour
    {
        void Start()
        {
            var toggle = GetComponent<Toggle>();
            if (toggle)
            {
                toggle.SetIsOnWithoutNotify(Screen.fullScreen);
            }
        }

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                var res = FullScreenButton.SuggestFullScreenSize();
                Screen.SetResolution(res.x, res.y, fullscreen: true);
            }
            else
            {
                var windowedSize = FullScreenButton.SuggestWindowedSize();
                Screen.SetResolution(windowedSize.x, windowedSize.y, fullscreen: false);
            }
        }
    }
}
