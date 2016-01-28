namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class ApplyDistributorReplyToAddressBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public ApplyDistributorReplyToAddressBehavior(string distributorAddress)
        {
            this.distributorAddress = distributorAddress;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            if (state.FromDistributor)
            {
                context.Headers[Headers.ReplyToAddress] = distributorAddress;
            }
            return next();
        }

        string distributorAddress;

        public class State
        {
            public bool FromDistributor { get; set; }
        }

        public class Registration : RegisterStep
        {
            public Registration(string distributorAddress)
                : base("ApplyDistributorReplyToAddressBehavior", 
                      typeof(ApplyDistributorReplyToAddressBehavior), 
                      "Invokes the encryption logic", 
                      _ => new ApplyDistributorReplyToAddressBehavior(distributorAddress))
            {
                InsertAfter("ApplyReplyToAddress");
            }

        }
    }
}