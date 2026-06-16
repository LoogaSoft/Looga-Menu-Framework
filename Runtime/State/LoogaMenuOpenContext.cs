using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public sealed class LoogaMenuOpenContext
    {
        public LoogaMenuOpenContext(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null,
            object payload = null)
        {
            Screen = screen;
            Requester = requester;
            Payload = payload;
        }

        public LoogaMenuScreenDefinition Screen { get; }
        public UnityEngine.Object Requester { get; }
        public object Payload { get; }

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            if (Payload is TPayload typedPayload)
            {
                payload = typedPayload;
                return true;
            }

            payload = default;
            return false;
        }

        public bool HasPayload(Type payloadType)
        {
            return payloadType != null && Payload != null && payloadType.IsInstanceOfType(Payload);
        }
    }
}
