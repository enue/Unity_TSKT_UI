#nullable enable
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class RectTransformExtensions
    {
        static public Rect GetLocalRect(this RectTransform rect, RectTransform? parent = null)
        {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            if (!parent || rect == parent)
            {
                var corners = ArrayPool<Vector3>.Shared.Rent(4);
                try
                {
                    rect.GetLocalCorners(corners);
                    foreach (var it in corners.AsSpan(0, 4))
                    {
                        xMin = Mathf.Min(xMin, it.x);
                        xMax = Mathf.Max(xMax, it.x);
                        yMin = Mathf.Min(yMin, it.y);
                        yMax = Mathf.Max(yMax, it.y);
                    }
                }
                finally
                {
                    ArrayPool<Vector3>.Shared.Return(corners);
                }
            }
            else
            {
                var corners = ArrayPool<Vector3>.Shared.Rent(4);
                try
                {
                    rect.GetWorldCorners(corners);
                    foreach (var it in corners.AsSpan(0, 4))
                    {
                        var pos = parent!.worldToLocalMatrix.MultiplyPoint(it);
                        xMin = Mathf.Min(xMin, pos.x);
                        xMax = Mathf.Max(xMax, pos.x);
                        yMin = Mathf.Min(yMin, pos.y);
                        yMax = Mathf.Max(yMax, pos.y);
                    }
                }
                finally
                {
                    ArrayPool<Vector3>.Shared.Return(corners);
                }
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static public Rect GetWorldRect(this RectTransform rect)
        {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            var corners = ArrayPool<Vector3>.Shared.Rent(4);
            try
            {
                rect.GetWorldCorners(corners);
                foreach (var it in corners.AsSpan(0, 4))
                {
                    xMin = Mathf.Min(xMin, it.x);
                    xMax = Mathf.Max(xMax, it.x);
                    yMin = Mathf.Min(yMin, it.y);
                    yMax = Mathf.Max(yMax, it.y);
                }
            }
            finally
            {
                ArrayPool<Vector3>.Shared.Return(corners);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
