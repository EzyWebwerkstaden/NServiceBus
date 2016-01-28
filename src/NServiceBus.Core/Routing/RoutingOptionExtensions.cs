namespace NServiceBus
{
    /// <summary>
    /// Gives users fine grained control over routing via extension methods.
    /// </summary>
    public static class RoutingOptionExtensions
    {
        /// <summary>
        /// Allows a specific physical address to be used to route this message.
        /// </summary>
        /// <param name="option">Option being extended.</param>
        /// <param name="destination">The destination address.</param>
        public static void SetDestination(this SendOptions option, string destination)
        {
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            var state = option.Context.GetOrCreate<UnicastSendRouterConnector.State>();
            state.Option = UnicastSendRouterConnector.RouteOption.ExplicitDestination;
            state.ExplicitDestination = destination;
        }

        /// <summary>
        /// Allows the target endpoint instance for this reply to set. If not used the reply will be sent to the `ReplyToAddress` of the incoming message.
        /// </summary>
        /// <param name="option">Option being extended.</param>
        /// <param name="destination">The new target address.</param>
        public static void OverrideReplyToAddressOfIncomingMessage(this ReplyOptions option, string destination)
        {
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            option.Context.GetOrCreate<UnicastReplyRouterConnector.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Routes this message to any instance of this endpoint.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteToThisEndpoint(this SendOptions option)
        {
            option.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .Option = UnicastSendRouterConnector.RouteOption.RouteToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Routes this message to this endpoint instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteToThisInstance(this SendOptions option)
        {
            option.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .Option = UnicastSendRouterConnector.RouteOption.RouteToThisInstance;
        }

        /// <summary>
        /// Routes this message to a specific instance of a destination endpoint.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        /// <param name="instanceId">ID of destination instance.</param>
        public static void RouteToThisSpecificInstance(this SendOptions option, string instanceId)
        {
            Guard.AgainstNull(nameof(instanceId), instanceId);
            var state = option.Context.GetOrCreate<UnicastSendRouterConnector.State>();
            state.Option = UnicastSendRouterConnector.RouteOption.RouteToSpecificInstance;
            state.SpecificInstance = instanceId;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteReplyToThisInstance(this SendOptions option)
        {
            option.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .RouteReplyToThisInstance = true;
        }
        
        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteReplyToAnyInstance(this SendOptions option)
        {
            option.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .RouteReplyToAnyInstance = true;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteReplyToThisInstance(this ReplyOptions option)
        {
            option.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .RouteReplyToThisInstance = true;
        }
        
        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteReplyToAnyInstance(this ReplyOptions option)
        {
            option.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .RouteReplyToAnyInstance = true;
        }
    }
}