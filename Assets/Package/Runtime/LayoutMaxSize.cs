using UnityEngine;
using System.Collections;
using UnityEngine.UI;
#nullable enable

namespace TSKT
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class LayoutMaxSize : MonoBehaviour, ILayoutElement
    {
        Text? text;
        Text Text => text ? text! : (text = GetComponent<Text>());

        [SerializeField]
        float maxWidth = -1;

        [SerializeField]
        float maxHeight = -1;

        public float flexibleHeight => -1f;
        public float flexibleWidth => -1f;
        public int layoutPriority => 1;
        public float minHeight => -1f;
        public float minWidth => -1f;

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

