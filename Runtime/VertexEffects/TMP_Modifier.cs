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

        TMP_VertexHelper vertexHelper;
        TMP_VertexHelper VertexHelper => vertexHelper ?? (vertexHelper = new TMP_VertexHelper(Text));

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

            foreach (var it in Effects)
            {
                it.Modify(VertexHelper);
            }

            vertexHelper.Consume();
        }
    }
}