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
            if (state.Option == RouteOption.RouteToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route a reply to this specific instance because endpoint instance ID was not provided by either host, a plugin or user. You can specify it via BusConfiguration.EndpointInstanceId, use a specific host or plugin.");
            }
            context.Headers[Headers.ReplyToAddress] = ApplyUserOverride(publicReturnAddress ?? GetDefaultReplyToValue(context), state);
            return next();
        }

        string ApplyUserOverride(string replyTo, State state)
        {
            if (state.Option == RouteOption.RouteToAnyInstanceOfThisEndpoint)
            {
                replyTo = sharedQueue;
            }
            else if (state.Option == RouteOption.RouteToThisInstance)
            {
                replyTo = instanceSpecificQueue;
            }
            else if (state.Option == RouteOption.ExplicitDestination)
            {
                replyTo = state.ExplicitDestination;
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
            RouteOption option;

            public RouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified reply routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            public string ExplicitDestination { get; set; }
        }

        public enum RouteOption
        {
            None,
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
        }
    }
}