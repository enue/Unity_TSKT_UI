#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace TSKT
{
    public static class TextSubscriber
    {
        static readonly Dictionary<Component, System.IDisposable> subscriptions = new();

        public static void Subscribe(Text text, System.IObservable<string> source)
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

#if TSKT_UI_SUPPORT_TEXTMESHPRO
        public static void Subscribe(TMPro.TMP_Text text, System.IObservable<string> source)
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

            subscriptions[text] = source.SubscribeWithState(text, (_, _text) => TextMeshProUtil.SetText(_text, _));
        }
#endif
    }
}
