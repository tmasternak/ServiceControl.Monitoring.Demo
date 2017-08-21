using NServiceBus;
using System;

namespace Messages
{
    public class OrderCompleted : IEvent
    {
        public Guid OrderId { get; set; }
    }
}