using NServiceBus;
using System;

namespace Messages
{
    public class OrderStockReserved : IEvent
    {
        public Guid OrderId { get; set; }
    }
}