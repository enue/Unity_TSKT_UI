#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace TSKT
{
    public static class TextSubscriber
    {
        static readonly Dictionary<Component, System.IDisposable> subscriptions = new();

        public static void Subscribe(Text text, Observable<string> source)
        {
            if (subscriptions.TryGetValue(text, out var oldSubscription))
            {
                oldSubscription.Dispose();
            }
            else
            {
                text.destroyCancellationToken.Register(() =>
                {
                    if (subscriptions.TryGetValue(text, out var value))
                    {
                        subscriptions.Remove(text);
                        value.Dispose();
                    }
                });
            }

            subscriptions[text] = source.SubscribeToText(text);
        }
        public static void Subscribe(TMPro.TMP_Text text, Observable<string> source)
        {
            if (subscriptions.TryGetValue(text, out var oldSubscription))
            {
                oldSubscription.Dispose();
            }
            else
            {
                text.destroyCancellationToken.Register(() =>
                {
                    if (subscriptions.TryGetValue(text, out var value))
                    {
                        subscriptions.Remove(text);
                        value.Dispose();
                    }
                });
            }

            subscriptions[text] = source.Subscribe(text, (_, _text) => _text.text = _);
        }
    }
}
