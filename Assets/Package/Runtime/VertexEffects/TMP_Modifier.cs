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

        TMP_Text? text;
        public TMP_Text Text => text ? text! : (text = GetComponent<TMP_Text>());

        TMP_BaseMeshEffect[]? effects = null;
        TMP_BaseMeshEffect[] Effects => effects ??= GetComponents<TMP_BaseMeshEffect>();


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

            var helper = new TMP_VertexHelper(Text);
            foreach (var it in Effects)
            {
                it.Modify(helper);
            }
            helper.Consume();
        }
    }
}
