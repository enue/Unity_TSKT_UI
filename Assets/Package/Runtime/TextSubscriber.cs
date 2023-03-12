#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace TSKT
{
    public class TextSubscriber : MonoBehaviour
    {
        System.IDisposable? subscription;

        public static void Subscribe(Text text, System.IObservable<string> source)
        {
            if (!text.TryGetComponent<TextSubscriber>(out var component))
            {
                component = text.gameObject.AddComponent<TextSubscriber>();
            }
            component.subscription?.Dispose();
            component.subscription = source.SubscribeToText(text);
            component.subscription.AddTo(text.gameObject.GetCancellationTokenOnDestroy());
        }

#if TSKT_UI_SUPPORT_TEXTMESHPRO
        public static void Subscribe(TMPro.TMP_Text text, System.IObservable<string> source)
        {
            if (!text.TryGetComponent<TextSubscriber>(out var component))
            {
                component = text.gameObject.AddComponent<TextSubscriber>();
            }
            component.subscription?.Dispose();
            component.subscription = source.Subscribe(_ => TextMeshProUtil.SetText(text, _));
            component.subscription.AddTo(text.gameObject.GetCancellationTokenOnDestroy());
        }
#endif
    }
}
