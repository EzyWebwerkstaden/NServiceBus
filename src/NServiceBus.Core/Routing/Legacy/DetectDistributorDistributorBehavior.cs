namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class DetectDistributorDistributorBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (context.Headers.ContainsKey("NServiceBus.Distributor.WorkerSessionId"))
            {
                context.Extensions.GetOrCreate<ApplyDistributorReplyToAddressBehavior.State>().FromDistributor = true;
            }
            return next();
        }
    }
}