﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    class MessageDrivenUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public MessageDrivenUnsubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, EndpointName endpoint, IDispatchMessages dispatcher, bool legacyMode)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.endpoint = endpoint;
            this.dispatcher = dispatcher;
            this.legacyMode = legacyMode;
        }

        protected override async Task Terminate(IUnsubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = (await subscriptionRouter.GetAddressesForEventType(eventType).ConfigureAwait(false))
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}. Please ensure the configured publisher endpoint has at least one known instance.");

            var unsubscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

                unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                if (legacyMode)
                {
                    unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;
                }
                else
                {
                    unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = replyToAddress;
                    unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = endpoint.ToString();
                }

                unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(publisherAddress, unsubscribeMessage, eventType.AssemblyQualifiedName, context.Extensions));
            }
            await Task.WhenAll(unsubscribeTasks.ToArray()).ConfigureAwait(false);
        }

        async Task SendUnsubscribeMessageWithRetries(string destination, OutgoingMessage unsubscribeMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(unsubscribeMessage, new UnicastAddressTag(destination));
                await dispatcher.Dispatch(new TransportOperations(transportOperation), context).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendUnsubscribeMessageWithRetries(destination, unsubscribeMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to unsubsribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        public class Settings
        {
            public Settings()
            {
                MaxRetries = 10;
                RetryDelay = TimeSpan.FromSeconds(2);
            }

            public TimeSpan RetryDelay { get; set; }
            public int MaxRetries { get; set; }
        }

        SubscriptionRouter subscriptionRouter;
        string replyToAddress;
        readonly EndpointName endpoint;
        IDispatchMessages dispatcher;
        readonly bool legacyMode;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}