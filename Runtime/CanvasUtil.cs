using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    public class CanvasUtil
    {
        static public Vector3 GetScreenPosition(Graphic obj)
        {
            return GetScreenPosition(obj, obj.transform.localPosition);
        }

        static public Vector3 GetScreenPosition(Graphic obj, Vector3 localPosition)
        {
            var worldPosition = obj.transform.localToWorldMatrix.MultiplyPoint(localPosition);
            return GetScreenPosition(obj.canvas, worldPosition);
        }

        static public Vector3 GetScreenPosition(Canvas canvas, Vector3 worldPosition)
        {
            var rootCanvas = canvas.rootCanvas;
            if (rootCanvas.worldCamera)
            {
                return rootCanvas.worldCamera.WorldToScreenPoint(worldPosition);
            }
            else
            {
                return worldPosition;
            }
        }

    }
}
