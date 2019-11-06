using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class FullScreenButton : MonoBehaviour
    {
        [SerializeField]
        Vector2Int aspectRatio = new Vector2Int(16, 9);

        Vector2Int windowedSize;

        public void OnClicked()
        {
            if (Screen.fullScreen)
            {
                if (windowedSize.sqrMagnitude == 0)
                {
                    // 使える解像度のうち2番目に大きいものを選ぶ
                    windowedSize = GetSizes()
                        .Distinct()
                        .OrderByDescending(_ => _.x)
                        .Skip(1)
                        .DefaultIfEmpty(new Vector2Int(Screen.width, Screen.height))
                        .First();
                }
                Screen.SetResolution(windowedSize.x, windowedSize.y, fullscreen: false);
            }
            else
            {
                windowedSize = new Vector2Int(Screen.width, Screen.height);

                // 使える解像度のうち一番大きいものを選ぶ
                var res = GetSizes()
                    .Distinct()
                    .OrderByDescending(_ => _.x)
                    .First();

                Screen.SetResolution(res.x, res.y, fullscreen: true);
            }
        }

        List<Vector2Int> GetSizes()
        {
            var resolutions = new List<Vector2Int>();
            foreach (var it in Screen.resolutions)
            {
                if (it.width * aspectRatio.y > it.height * aspectRatio.x)
                {
                    // 横長ディスプレイは縦をあわせる
                    resolutions.Add(new Vector2Int(it.height * aspectRatio.x / aspectRatio.y, it.height));
                }
                else
                {
                    // 縦長ディスプレイは横をあわせる
                    resolutions.Add(new Vector2Int(it.width, it.width * aspectRatio.y / aspectRatio.x));
                }
            }
            return resolutions;
        }
    }
}
