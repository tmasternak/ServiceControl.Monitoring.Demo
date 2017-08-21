﻿using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSaga
{
    public class OrderSaga : Saga<OrderSagaData>,
        IAmStartedByMessages<PlaceOrder>,
        IHandleMessages<PaymentReceived>,
        IHandleTimeouts<PaymentTimedOut>
    {
        TimeSpan processingDelay;
        bool failRandomly;

        public OrderSaga()
        {
            processingDelay = TimeSpan.Parse(ConfigurationManager.AppSettings["processingDelay"]);
            failRandomly = bool.Parse(ConfigurationManager.AppSettings["failRandomly"]);
        }

        public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
        {
            if (failRandomly && new Random().Next(5) == 1)
            {
                throw new Exception("Boom!");
            }

            await Task.Delay(processingDelay);

            await context.Publish(new OrderPlaced
            {
                OrderId = message.Id
            });

            await RequestTimeout(context, TimeSpan.FromMinutes(2), new PaymentTimedOut { OrderId = message.Id });
        }

        public async Task Handle(PaymentReceived message, IMessageHandlerContext context)
        {
            await context.Publish(new OrderCompleted { OrderId = message.OrderId });

            MarkAsComplete();
        }

        public async Task Timeout(PaymentTimedOut state, IMessageHandlerContext context)
        {
            await context.Publish(new OrderTimedOut { OrderId = state.OrderId });

            MarkAsComplete();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
        {
            mapper.ConfigureMapping<PlaceOrder>(order => order.Id).ToSaga(saga => saga.Id);
            mapper.ConfigureMapping<PaymentReceived>(payment => payment.OrderId).ToSaga(saga => saga.Id);
        }
    }
}