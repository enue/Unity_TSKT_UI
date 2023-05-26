using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(TMP_Modifier))]
    public abstract class TMP_BaseMeshEffect : MonoBehaviour
    {
        TMP_Modifier? modifier;
        public TMP_Modifier Modifier => modifier ? modifier! : (modifier = GetComponent<TMP_Modifier>());

        public abstract void Modify(TMP_VertexHelper vertexHelper);
    }
}
