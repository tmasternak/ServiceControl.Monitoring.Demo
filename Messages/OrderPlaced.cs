using NServiceBus;
using System;

namespace Messages
{
    public class OrderPlaced : IEvent
    {
        public Guid OrderId { get; set; }
    }
}