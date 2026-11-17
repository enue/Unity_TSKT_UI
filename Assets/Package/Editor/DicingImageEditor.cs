using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TSKT
{
    [CustomEditor(typeof(DicingImage))]
    public class DicingImageEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Auto Correct"))
            {
                var image = target as DicingImage;
                var sprite = image.Sprite;
                sprite.AutoCorrect();
                Undo.RecordObject(image, "Auto Correct");
                image.SetSprite(sprite, forceRebuild: true);
                EditorUtility.SetDirty(image);
            }
            if (GUILayout.Button("Auto Correct Size and Positions"))
            {
                var image = target as DicingImage;
                var sprite = image.Sprite;
                sprite.AutoCorrectSizeAndPositions();
                // spritesの参照が変わらないのでforceRebuildしないと反映されない
                Undo.RecordObject(image, "Auto Correct Size and Positions");
                image.SetSprite(sprite, forceRebuild: true);
                EditorUtility.SetDirty(image);
            }
            if (GUILayout.Button("Rebuild"))
            {
                var image = target as DicingImage;
                image.Rebuild();
            }
            if (GUILayout.Button("Set Native Size"))
            {
                var image = target as DicingImage;
                image.SetNativeSize();
            }
        }
    }
}
