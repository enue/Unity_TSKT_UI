using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class UIVerticesPool
    {
        static List<UIVertex> buffer;

        public static List<UIVertex> Get()
        {
            var result = buffer ?? new List<UIVertex>();
            buffer = null;
            return result;
        }

        public static void Release(List<UIVertex> vertices)
        {
            buffer = vertices;
        }
    }
}
