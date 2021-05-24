using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(Text))]
    [ExecuteInEditMode]
    public class HyphenationJpn : UIBehaviour
    {
        // http://answers.unity3d.com/questions/424874/showing-a-textarea-field-for-a-string-variable-in.html
        [TextArea(3, 10), SerializeField]
        private string text = default!;

        private Text _Text
        {
            get
            {
                if (_text == null)
                    _text = GetComponent<Text>();
                return _text;
            }
        }
        private Text? _text;
        readonly HyphenationJpns.Ruler ruler = new HyphenationJpns.Ruler();

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateText(text);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateText(text);
        }
#endif

        void UpdateText(string str)
        {
            // override
            _Text.horizontalOverflow = HorizontalWrapMode.Overflow;

            // update Text
            _Text.text = ruler.GetFormattedText(_Text, str);
        }

        public void SetText(string str)
        {
            text = str;
            UpdateText(text);
        }

        // helper
        public float textWidth
        {
            set
            {
                _Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
            }
            get
            {
                return _Text.rectTransform.rect.width;
            }
        }
        public int fontSize
        {
            set
            {
                _Text.fontSize = value;
            }
            get
            {
                return _Text.fontSize;
            }
        }

    }
}