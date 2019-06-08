using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    static public class UIUtil
    {
        static public bool TryGetHierarchyPosition(Transform root, Transform target, out double result)
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
