﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class TopModal : MonoBehaviour
    {
        public readonly struct Handler
        {
            readonly TopModal owner;
            public int Id { get; }

            public Handler(TopModal topModal)
            {
                owner = topModal;
                Id = topModal.nextHandlerId;
            }

            public void Disable()
            {
                owner.handlerIds.Remove(Id);
                if (owner.handlerIds.Count == 0)
                {
                    owner.gameObject.SetActive(false);
                }
            }
        }

        public static TopModal Instance { get; private set; }

        int nextHandlerId;
        readonly HashSet<int> handlerIds = new HashSet<int>();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (handlerIds.Count == 0)
            {
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Handler Enable()
        {
            var token = new Handler(this);
            handlerIds.Add(token.Id);
            ++nextHandlerId;

            gameObject.SetActive(true);
            return token;
        }
    }
}
