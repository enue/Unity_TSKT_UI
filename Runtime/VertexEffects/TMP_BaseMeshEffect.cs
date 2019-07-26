﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(TMP_Modifier))]
    public abstract class TMP_BaseMeshEffect : MonoBehaviour
    {
        TMP_Modifier modifier;
        public TMP_Modifier Modifier => modifier ?? (modifier = GetComponent<TMP_Modifier>());

        public abstract void Modify(ref List<Vector3> vertices, ref List<Color> colors);
    }
}