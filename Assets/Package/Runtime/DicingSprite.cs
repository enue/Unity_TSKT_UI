using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#nullable enable

namespace TSKT
{
    [System.Serializable]
    public struct DicingSprite
    {
        [SerializeField]
        Vector2 size;
        public readonly Vector2 Size
        {
            get
            {
                if (size.sqrMagnitude > 0)
                {
                    return size;
                }

                if (sprites == null)
                {
                    return Vector2.zero;
                }
                if (sprites.Any(_ => !_))
                {
                    return Vector2.zero;
                }
                Span<Rect> rects = stackalloc Rect[SpriteRectCount];
                GetSpriteRects(rects);

                float w = 0f;
                float h = 0f;
                foreach (var it in rects)
                {
                    w = Math.Max(w, it.xMax);
                    h = Math.Max(h, it.yMax);
                }
                return new Vector2(w, h);
            }
        }

        public Sprite[] sprites;

        [SerializeField]
        Rect[]? spriteRects;
        public readonly int SpriteRectCount
        {
            get
            {
                if (spriteRects != null && spriteRects.Length > 0)
                {
                    return spriteRects.Length;
                }
                if (sprites == null)
                {
                    return 0;
                }
                return sprites.Length;
            }
        }
        public readonly void GetSpriteRects(System.Span<Rect> dest)
        {
            if (spriteRects != null && spriteRects.Length > 0)
            {
                spriteRects.CopyTo(dest);
                return;
            }
            if (sprites == null)
            {
                return;
            }
            for (int i = 0; i < sprites.Length; i++)
            {
                dest[i] = sprites[i].rect;
            }
        }

        public DicingSprite(params Sprite[] sprites)
        {
            this.sprites = sprites;
            size = default;
            spriteRects = default;
        }
        public readonly bool Empty => sprites == null || sprites.Length == 0;

        public static DicingSprite Combine(in ReadOnlySpan<DicingSprite> items)
        {
            int capacity = 0;
            foreach(var it in items)
            {
                capacity += it.sprites.Length;
            }

            var result = new DicingSprite(new Sprite[capacity]);
            var sprites = result.sprites.AsSpan();
            foreach (var item in items)
            {
                item.sprites.CopyTo(sprites);
                sprites = sprites[item.sprites.Length..];
            }

            {
                var hasRects = false;
                foreach (var it in items)
                {
                    if (it.spriteRects != null && it.spriteRects.Length > 0)
                    {
                        hasRects = true;
                        break;
                    }
                }
                if (!hasRects)
                {
                    return result;
                }
            }

            result.spriteRects = new Rect[capacity];
            var rects = result.spriteRects.AsSpan();
            foreach (var item in items)
            {
                var _rects = rects[..item.SpriteRectCount];
                item.GetSpriteRects(_rects);
                rects = rects[_rects.Length..];
            }
            return result;
        }

#if UNITY_EDITOR
        public void AutoCorrect()
        {
            if (sprites == null)
            {
                size = default;
                spriteRects = System.Array.Empty<Rect>();
                return;
            }
            if (sprites.Length == 0)
            {
                size = default;
                spriteRects = System.Array.Empty<Rect>();
                return;
            }
            var mainSprite = sprites[0];
            if (!mainSprite)
            {
                size = default;
                spriteRects = System.Array.Empty<Rect>();
                return;
            }

            var path = UnityEditor.AssetDatabase.GetAssetPath(mainSprite);
            sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToArray();

            AutoCorrectSizeAndPositions();
        }

        public void AutoCorrectSizeAndPositions()
        {
            if (sprites == null || sprites.Length == 0)
            {
                size = default;
                spriteRects = System.Array.Empty<Rect>();
            }

            var w = sprites.Max(_ => _.rect.xMax);
            var h = sprites.Max(_ => _.rect.yMax);
            size = new Vector2(w, h);

            spriteRects = sprites.Select(_ => _.rect).ToArray();
        }
#endif
    }
}
