#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public static class ScrollRectExtensions
    {
        public static void AdjustScrollPositionToCointainInViewport(this ScrollRect scrollRect,
            RectTransform item, Vector2 margin)
        {
            var itemRect = item.GetLocalRect(scrollRect.viewport);
            itemRect.xMin -= margin.x;
            itemRect.xMax += margin.x;
            itemRect.yMin -= margin.y;
            itemRect.yMax +=  margin.y;
            var contentRect = scrollRect.content.GetLocalRect(scrollRect.viewport);
            var viewportRect = scrollRect.viewport.rect;

            if (scrollRect.TryGetHorizontalNormalizedPositionToContainInViewport(out var x, itemRect, viewportRect, contentRect))
            {
                scrollRect.horizontalNormalizedPosition = x;
            }
            if (scrollRect.TryGetVerticalNormalziedPositionToContainInViewport(out var y, itemRect, viewportRect, contentRect))
            {
                scrollRect.verticalNormalizedPosition = y;
            }
        }

        public static bool TryGetVerticalNormalziedPositionToContainInViewport(this ScrollRect scrollRect,
            out float result, Rect itemRect, Rect viewportRect, Rect contentRect)
        {
            if (!scrollRect.vertical)
            {
                result = default;
                return false;
            }
            if (itemRect.yMin < viewportRect.yMin)
            {
                result = (itemRect.yMin - contentRect.yMin) / (contentRect.height - viewportRect.height);
                result = Mathf.Clamp01(result);
                return true;
            }
            else if (itemRect.yMax > viewportRect.yMax)
            {
                result = 1f - ((contentRect.yMax - itemRect.yMax) / (contentRect.height - viewportRect.height));
                result = Mathf.Clamp01(result);
                return true;
            }
            result = default;
            return false;
        }

        public static bool TryGetHorizontalNormalizedPositionToContainInViewport(this ScrollRect scrollRect,
            out float result, Rect itemRect, Rect viewportRect, Rect contentRect)
        {
            if (!scrollRect.horizontal)
            {
                result = default;
                return false;
            }
            if (itemRect.xMin < viewportRect.xMin)
            {
                result = (itemRect.xMin - contentRect.xMin) / (contentRect.height - viewportRect.height);
                result = Mathf.Clamp01(result);
                return true;
            }
            else if (itemRect.xMax > viewportRect.xMax)
            {
                result = 1f - ((contentRect.xMax - itemRect.xMax) / (contentRect.width - viewportRect.width));
                result = Mathf.Clamp01(result);
                return true;
            }
            result = default;
            return false;
        }

    }
}
