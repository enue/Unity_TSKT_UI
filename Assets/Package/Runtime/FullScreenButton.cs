using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class FullScreenButton : MonoBehaviour
    {
        public void OnClicked()
        {
            if (Screen.fullScreen)
            {
                var windowedSize = SuggestWindowedSize();
                Screen.SetResolution(windowedSize.x, windowedSize.y, fullscreen: false);

#if UNITY_EDITOR
                Debug.Log("windowedSize : " + windowedSize);
#endif
            }
            else
            {
                var res = SuggestFullScreenSize();
                Screen.SetResolution(res.x, res.y, fullscreen: true);

#if UNITY_EDITOR
                Debug.Log("fullScreenResolution : " + res);
#endif
            }
        }

        static public Vector2Int SuggestWindowedSize()
        {
            // 最大サイズの2/3以下の解像度を選ぶ
            // モニタが1920x1080ならウィンドウサイズ1280x720が選ばれる想定
            var res = GetResolutions();
            var w = res.Max(_ => _.x) * 2 / 3;
            var h = res.Max(_ => _.y) * 2 / 3;

            return res.Where(_ => _.x <= w)
                .Where(_ => _.y <= h)
                .OrderByDescending(_ => _.x)
                .ThenByDescending(_ => _.y)
                .DefaultIfEmpty(new Vector2Int(Screen.width, Screen.height))
                .First();
        }

        static public Vector2Int SuggestFullScreenSize()
        {
            // 使える解像度のうち一番大きいものを選ぶ
            return GetResolutions()
                .OrderByDescending(_ => _.x)
                .ThenByDescending(_ => _.y)
                .First();
        }

        static public Vector2Int[] GetResolutions()
        {
            return Screen.resolutions
                .Select(_ => new Vector2Int(_.width, _.height))
                .Distinct()
                .ToArray();
        }
    }
}
