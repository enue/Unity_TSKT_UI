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
                image.Sprite = sprite;
            }
            if (GUILayout.Button("Auto Correct Size and Positions"))
            {
                var image = target as DicingImage;
                var sprite = image.Sprite;
                sprite.AutoCorrectSizeAndPositions();
                image.Sprite = sprite;
            }
            if (GUILayout.Button("Set Native Size"))
            {
                var image = target as DicingImage;
                image.SetNativeSize();
            }
        }
    }
}
