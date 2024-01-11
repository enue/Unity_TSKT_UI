#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TSKT;
using R3;
using UnityEngine;

public class ExclusiveTextSubscriberSample : MonoBehaviour
{
    [SerializeField]
    TMP_Text text = default!;


    async void Start()
    {
        var subject = new ReactiveProperty<string>("a");
        TextSubscriber.Subscribe(text, subject);
        subject.Value = "b";


        var subject2  = new ReactiveProperty<string>("1");
        var subject3 = subject2.ToReadOnlyReactiveProperty();
        subject2.OnCompleted();

        TextSubscriber.Subscribe(text, subject3);
        Destroy(text.gameObject);
        await UniTask.Yield();
    }
}

