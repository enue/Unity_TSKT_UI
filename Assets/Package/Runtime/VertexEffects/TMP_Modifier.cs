using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class TMP_Modifier : MonoBehaviour
    {
        [SerializeField]
        bool forceRefresh = false;

        TextMeshProUGUI? text;
        public TextMeshProUGUI Text => text ? text! : (text = GetComponent<TextMeshProUGUI>());

        TMP_BaseMeshEffect[]? effects = null;
        TMP_BaseMeshEffect[] Effects => effects ??= GetComponents<TMP_BaseMeshEffect>();

        TMP_VertexHelper? vertexHelper;
        TMP_VertexHelper VertexHelper => vertexHelper ??= new(Text);

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

            VertexHelper.Consume();
        }
    }
}
