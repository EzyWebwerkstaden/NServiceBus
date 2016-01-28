namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;

    class ApplyReplyToAddressBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public ApplyReplyToAddressBehavior(string sharedQueue, string instanceSpecificQueue, string publicReturnAddress)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.publicReturnAddress = publicReturnAddress;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            if (state.RouteReplyToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route a reply to this specific instance because endpoint instance ID was not provided by either host, a plugin or user. You can specify it via BusConfiguration.EndpointInstanceId, use a specific host or plugin.");
            }
            if (state.RouteReplyToThisInstance && state.RouteReplyToAnyInstance)
            {
                throw new InvalidOperationException("Cannot specify RouteReplyToThisInstance and RouteReplyToAnyInstance at the same time.");
            }
            context.Headers[Headers.ReplyToAddress] = publicReturnAddress ?? ApplyUserOverride(GetDefaultReplyToValue(context), state);
            return next();
        }

        string ApplyUserOverride(string replyTo, State state)
        {
            if (state.RouteReplyToAnyInstance)
            {
                replyTo = sharedQueue;
            }
            else if (state.RouteReplyToThisInstance)
            {
                replyTo = instanceSpecificQueue;
            }
            return replyTo;
        }

        string GetDefaultReplyToValue(IExtendable context)
        {
            string receivedFrom;
            return context.Extensions.TryGet("ReceivedFrom", out receivedFrom) 
                ? receivedFrom 
                : sharedQueue;
        }

        string sharedQueue;
        string instanceSpecificQueue;
        string publicReturnAddress;


        public class State
        {
            public bool RouteReplyToThisInstance { get; set; }
            public bool RouteReplyToAnyInstance { get; set; }
        }
    }
}