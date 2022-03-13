using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(RectTransform))]
    public class DicingImage : MonoBehaviour
    {
        [SerializeField]
        Image? imagePrefab = default;

        [SerializeField]
        bool useSpriteMesh = false;

        [SerializeField]
        bool preserveAspect = false;

        [SerializeField]
        Color color = Color.white;

        [SerializeField]
        DicingSprite sprite = default;

        [SerializeField]
        List<Image?> items = default!;

        public RectTransform RectTransform => (RectTransform)transform;

        public DicingSprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite.sprites != value.sprites)
                {
                    sprite = value;
                    Rebuild();
                }
            }
        }

        public Color Color
        {
            get => color;
            set
            {
                color = value;
                foreach (var it in items)
                {
                    if (it)
                    {
                        it!.color = color;
                    }
                }
            }
        }

        public void SetSingleSprite(Sprite sprite)
        {
            Sprite = new DicingSprite(new Sprite[1] { sprite });
            Rebuild();
        }

        public void Rebuild()
        {
            var position = 0;
            try
            {
                if (sprite.sprites == null || sprite.sprites.Length == 0)
                {
                    return;
                }

                if (sprite.sprites.Any(_ => !_))
                {
                    return;
                }

                var spriteSize = sprite.Size;
                Rect areaInImage;
                if (preserveAspect)
                {
                    var imageSize = RectTransform.rect.size;
                    // if (spriteSize.y / spriteSize.x > size.y / size.x)
                    if (spriteSize.y * imageSize.x > imageSize.y * spriteSize.x)
                    {
                        // 画像が縦長
                        var width = imageSize.y * spriteSize.x / (imageSize.x * spriteSize.y);
                        areaInImage = Rect.MinMaxRect(
                            0.5f - width / 2f, 0f,
                            0.5f + width / 2f, 1f);
                    }
                    else if (spriteSize.y * imageSize.x < imageSize.y * spriteSize.x)
                    {
                        // 画像が横長
                        var height = imageSize.x * spriteSize.y / (imageSize.y * spriteSize.x);
                        areaInImage = Rect.MinMaxRect(
                            0f, 0.5f - height / 2f,
                            1f, 0.5f + height / 2f);
                    }
                    else
                    {
                        areaInImage = Rect.MinMaxRect(0f, 0f, 1f, 1f);
                    }
                }
                else
                {
                    areaInImage = Rect.MinMaxRect(0f, 0f, 1f, 1f);
                }

                foreach (var it in sprite.sprites.Zip(sprite.SpriteRects, (sprite, rect) => (sprite, rect)))
                {
                    if (items.Count == position)
                    {
                        items.Add(null);
                    }
                    var item = items[position];
                    if (!item)
                    {
                        if (imagePrefab)
                        {
                            item = Instantiate(imagePrefab, transform)!;
                            items[position] = item;
                        }
                        else
                        {
                            var gameObject = new GameObject("sliced sprite");
                            gameObject.transform.SetParent(transform, worldPositionStays: false);
                            item = gameObject.AddComponent<Image>();
                            item.raycastTarget = false;
                            items[position] = item;
                        }
                    }
                    ++position;
                    item!.sprite = it.sprite;
                    item.color = color;
                    item.useSpriteMesh = useSpriteMesh;

                    var bottom = it.rect.yMin / spriteSize.y;
                    var top = it.rect.yMax / spriteSize.y;
                    var left = it.rect.xMin / spriteSize.x;
                    var right = it.rect.xMax / spriteSize.x;

                    item.rectTransform.anchorMin = new Vector2(
                        Mathf.Lerp(areaInImage.xMin, areaInImage.xMax, left),
                        Mathf.Lerp(areaInImage.yMin, areaInImage.yMax, bottom));
                    item.rectTransform.anchorMax = new Vector2(
                        Mathf.Lerp(areaInImage.xMin, areaInImage.xMax, right),
                        Mathf.Lerp(areaInImage.yMin, areaInImage.yMax, top));

                    item.rectTransform.offsetMin = new Vector2(0f, 0f);
                    item.rectTransform.offsetMax = new Vector2(0f, 0f);

                    item.gameObject.SetActive(true);
                }
            }
            finally
            {
                for (int i = position; i < items.Count; ++i)
                {
                    var it = items[i];
                    if (it)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(it!.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(it!.gameObject);
                        }
                    }
                }
                items.RemoveRange(position, items.Count - position);
            }
        }

        public void SetNativeSize()
        {
            var size = Sprite.Size;
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            if (preserveAspect)
            {
                Rebuild();
            }
        }
    }
}
