using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSKT
{
    [RequireComponent(typeof(Text))]
    public class RubyBodyText : BaseMeshEffect
    {
        public List<UIVertex> OriginalTextVertices { get; } = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            vh.GetUIVertexStream(OriginalTextVertices);
        }
    }
}
