using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class TMP_Modifier : MonoBehaviour
    {
        [SerializeField]
        bool forceRefresh = false;

        TMP_Text text;
        public TMP_Text Text => text ?? (text = GetComponent<TMP_Text>());

        TMP_BaseMeshEffect [] effects = null;
        TMP_BaseMeshEffect[] Effects => effects ?? (effects = GetComponents<TMP_BaseMeshEffect>());

        List<Color> colors = new List<Color>();
        List<Vector3> vertices = new List<Vector3>();

        void LateUpdate()
        {
            if (forceRefresh)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (Effects.Length == 0)
            {
                return;
            }

            Text.ForceMeshUpdate();
            var mesh = Text.mesh;
            mesh.GetVertices(vertices);
            mesh.GetColors(colors);

            foreach (var it in Effects)
            {
                it.Modify(ref vertices, ref colors);
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);

            Text.UpdateGeometry(mesh, 0);
        }
    }
}