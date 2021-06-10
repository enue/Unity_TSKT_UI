#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class RectTransformExtensions
    {
        static readonly Vector3[] worldCorners = new Vector3[4];

        static public Rect GetLocalRect(this RectTransform rect, RectTransform? parent = null)
        {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            rect.GetWorldCorners(worldCorners);
            foreach (var it in worldCorners)
            {
                Vector2 pos;
                if (!parent || rect == parent)
                {
                    pos = it;
                }
                else
                {
                    pos = parent!.worldToLocalMatrix.MultiplyPoint(it);
                }
                xMin = Mathf.Min(xMin, pos.x);
                xMax = Mathf.Max(xMax, pos.x);
                yMin = Mathf.Min(yMin, pos.y);
                yMax = Mathf.Max(yMax, pos.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static public Rect GetWorldRect(this RectTransform rect)
        {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            rect.GetWorldCorners(worldCorners);
            foreach (var it in worldCorners)
            {
                xMin = Mathf.Min(xMin, it.x);
                xMax = Mathf.Max(xMax, it.x);
                yMin = Mathf.Min(yMin, it.y);
                yMax = Mathf.Max(yMax, it.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
