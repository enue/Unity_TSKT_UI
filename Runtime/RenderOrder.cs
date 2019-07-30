using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public readonly struct RenderOrder : System.IComparable<RenderOrder>
    {
        readonly Canvas canvas;
        readonly double hierarchyPosition;

        public RenderOrder(Canvas rootCanvas, Transform target)
        {
            canvas = rootCanvas;
            TryGetHierarchyPosition(rootCanvas.transform, target, out hierarchyPosition);
        }

        public static bool operator >(in RenderOrder x, in RenderOrder y)
        {
            return Compare(in x, in y) > 0;
        }

        public static bool operator <(in RenderOrder x, in RenderOrder y)
        {
            return Compare(in x, in y) < 0;
        }

        public static int Compare(in RenderOrder x, in RenderOrder y)
        {
            if (x.canvas != y.canvas)
            {
                var compareCanvases = CompareCanvases(x.canvas, y.canvas);
                if (compareCanvases != 0)
                {
                    return compareCanvases;
                }
            }

            if (x.hierarchyPosition > y.hierarchyPosition)
            {
                return 1;
            }
            if (x.hierarchyPosition < y.hierarchyPosition)
            {
                return -1;
            }
            return 0;
        }

        static int CompareCanvases(Canvas x, Canvas y)
        {
            if (x == y)
            {
                return 0;
            }

            // worldは考えない
            Debug.Assert(x.renderMode != RenderMode.WorldSpace, "wrong renderMode");
            Debug.Assert(y.renderMode != RenderMode.WorldSpace, "wrong renderMode");

            // ScreenSpaceOverlay のほうが ScreenSpaceCameraより手前
            if (x.renderMode != y.renderMode)
            {
                if (x.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return 1;
                }
                if (y.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return -1;
                }
            }

            if (x.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // renderOrderが大きいほうが手前
                if (x.renderOrder > y.renderOrder)
                {
                    return 1;
                }
                if (x.renderOrder < y.renderOrder)
                {
                    return -1;
                }
            }
            if (x.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // cameraDepthが大きいほうが手前
                if (x.worldCamera.depth > y.worldCamera.depth)
                {
                    return 1;
                }
                if (x.worldCamera.depth < y.worldCamera.depth)
                {
                    return -1;
                }
                if (x.worldCamera == y.worldCamera)
                {
                    if (x.sortingLayerID != y.sortingLayerID)
                    {
                        // sortingLayerの大きいほうが手前
                        if (SortingLayer.GetLayerValueFromID(x.sortingLayerID)
                            > SortingLayer.GetLayerValueFromID(y.sortingLayerID))
                        {
                            return 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }

                    // sortingOrderの大きいほうが手前
                    if (x.sortingOrder > y.sortingOrder)
                    {
                        return 1;
                    }
                    if (x.sortingOrder < y.sortingOrder)
                    {
                        return -1;
                    }
                }
            }
            // 同じ設定になっている場合は判断できない。
            Debug.Assert(false, "can't determine comparing " + x.name + " and " + y.name);

            // とりあえずInstaneIDでごまかす
            var xInstanceID = x.GetInstanceID();
            var yInstanceID = y.GetInstanceID();
            if (xInstanceID > yInstanceID)
            {
                return 1;
            }
            if (xInstanceID < yInstanceID)
            {
                return -1;
            }
            return 0;
        }

        public int CompareTo(RenderOrder other)
        {
            return Compare(this, other);
        }

        static bool TryGetHierarchyPosition(Transform root, Transform target, out double result)
        {
            result = 0.0;

            var current = target;
            while (current != root)
            {
                if (!current.parent)
                {
                    return false;
                }
                var index = current.GetSiblingIndex();
                var count = current.parent.childCount;
                result += index + 1;
                result /= count + 1;
                current = current.parent;
            }

            return true;
        }
    }
}
