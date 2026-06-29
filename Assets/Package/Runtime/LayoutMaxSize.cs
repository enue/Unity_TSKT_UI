#nullable enable
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace TSKT
{
#if UNITY_6000_6_OR_NEWER
    [Obsolete("use LayoutElement")]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class LayoutMaxSize : MonoBehaviour, ILayoutElement
    {
        Text? text;
        Text Text => text ? text! : (text = GetComponent<Text>());


        public float flexibleHeight => -1f;
        public float flexibleWidth => -1f;
        public int layoutPriority => 1;
        public float minHeight => -1f;
        public float minWidth => -1f;

#if UNITY_6000_6_OR_NEWER
        public float maxWidth => -1f;
        public float maxHeight => -1f;
        public float preferredWidth => -1f;
        public float preferredHeight => -1f;
#else
        [SerializeField]
        float maxWidth = -1;

        [SerializeField]
        float maxHeight = -1;
        public float preferredHeight
        {
            get
            {
                if (maxHeight < 0f)
                {
                    return Text.preferredHeight;
                }
                return Mathf.Min(Text.preferredHeight, maxHeight);
            }
            set
            {
                maxHeight = value;
                SetDirty();
            }
        }
        public float preferredWidth
        {
            get
            {
                if (maxWidth < 0f)
                {
                    return Text.preferredWidth;
                }
                return Mathf.Min(Text.preferredWidth, maxWidth);
            }
            set
            {
                maxWidth = value;
                SetDirty();
            }
        }
#endif
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        protected void SetDirty()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}

