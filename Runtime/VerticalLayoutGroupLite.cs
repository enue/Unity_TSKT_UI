using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TSKT
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VerticalLayoutGroupLite : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        RectTransform rectTransform;
        RectTransform RectTransform => rectTransform ?? (rectTransform = (RectTransform)transform);

        [SerializeField]
        float childHeight = 0f;

        [SerializeField]
        float paddingTop = 0f;

        [SerializeField]
        float paddingBottom = 0f;

        [SerializeField]
        float spacing = 0f;

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => -1f;
        public float preferredHeight { get; private set; }
        public float flexibleHeight => -1f;
        public int layoutPriority => 0;

        public void CalculateLayoutInputHorizontal()
        {
            // nop
        }

        public void CalculateLayoutInputVertical()
        {
            var childCount = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    ++childCount;
                }
            }
            if (childCount == 0)
            {
                preferredHeight = paddingTop + paddingBottom;
            }
            else
            {
                preferredHeight =
                    childHeight * childCount
                    + spacing * (childCount - 1)
                    + paddingTop + paddingBottom;
            }
        }

        public void SetLayoutHorizontal()
        {
            // nop
        }

        public void SetLayoutVertical()
        {
            var rectTransform = RectTransform;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

            var pivot = rectTransform.pivot;
            var top = (1f - pivot.y) * preferredHeight - paddingTop;

            var index = 0;
            foreach (RectTransform child in transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }
                var pos = child.localPosition;
                pos.y = top - index * (childHeight + spacing) - childHeight * (1f - child.pivot.y);
                child.localPosition = pos;
                ++index;
            }
        }

        protected void SetDirty()
        {
            if (!IsActive())
                return;

            if (!CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            else
                StartCoroutine(DelayedSetDirty(rectTransform));
        }

        IEnumerator DelayedSetDirty(RectTransform rectTransform)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}
