namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class DetectDistributorDistributorBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (context.Headers.ContainsKey(LegacyDistributorHeaders.WorkerSessionId))
            {
                context.Extensions.GetOrCreate<ApplyReplyToAddressBehavior.State>().FromDistributor = true;
            }
            return next();
        }
    }
}