using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    [System.Serializable]
    public struct DicingSprite
    {
        public Vector2 size;
        public Sprite[] sprites;
        public Rect[] spriteRects;

        public DicingSprite(params Sprite[] sprites)
        {
            this.sprites = sprites;
            size = default;
            spriteRects = default;
        }
        public bool Empty => sprites == null || sprites.Length == 0;

        public Vector2 Size
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

                var w = sprites.Max(_ => _.rect.xMax);
                var h = sprites.Max(_ => _.rect.yMax);
                return new Vector2(w, h);
            }
        }

        public Rect[] SpriteRects
        {
            get
            {
                if (spriteRects != null && spriteRects.Length > 0)
                {
                    return spriteRects;
                }

                if (sprites == null)
                {
                    return System.Array.Empty<Rect>();
                }

                return sprites.Select(_ => _.rect).ToArray();
            }
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
