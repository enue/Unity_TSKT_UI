#nullable enable
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    static class ArrayBufferWriterExtensions
    {
        public static void Add<T>(this ArrayBufferWriter<T> writer, T item)
        {
            writer.GetSpan(1)[0] = item;
            writer.Advance(1);
        }

    }
}
