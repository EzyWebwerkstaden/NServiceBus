namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public async Task Should_use_public_return_address_if_specified()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "PublicAddress", null);
            var context = CreateContext();

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("PublicAddress", context.Headers[Headers.ReplyToAddress]);
        }

        static OutgoingLogicalMessageContext CreateContext()
        {
            var context = new OutgoingLogicalMessageContext(
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>(),
                new OutgoingLogicalMessage(new MyMessage()),
                new RoutingStrategy[]
                {
                },
                new RootContext(null, null));
            return context;
        }

        [Test]
        public async Task Should_default_to_setting_the_reply_to_header_to_this_endpoint()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var context = CreateContext();

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("MyEndpoint", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_header_to_this_endpoint_when_requested()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var context = CreateContext();

            context.GetOrCreate<ApplyReplyToAddressBehavior.State>().Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("MyEndpoint", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_header_to_this_instance_when_requested()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var context = CreateContext();

            context.GetOrCreate<ApplyReplyToAddressBehavior.State>().Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("MyInstance", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_distributor_address_when_message_comes_from_a_distributor()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "MyPublicAddress", "MyDistributor");
            var context = CreateContext();

            context.Set(new IncomingMessage("ID", new Dictionary<string, string>
            {
                { LegacyDistributorHeaders.WorkerSessionId, "SessionID" }
            }, new MemoryStream()));

            var state = context.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("MyDistributor", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_throw_when_trying_to_route_replies_to_this_instance_when_no_instance_id_is_used()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", null, null, null);
            var context = CreateContext();

            context.GetOrCreate<ApplyReplyToAddressBehavior.State>().Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;

            try
            {
                await behavior.Invoke(context, () => TaskEx.CompletedTask);
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task Should_throw_when_conflicting_settings_are_specified()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var context = CreateContext();

            try
            {
                context.GetOrCreate<ApplyReplyToAddressBehavior.State>().Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
                context.GetOrCreate<ApplyReplyToAddressBehavior.State>().Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;

                await behavior.Invoke(context, () => TaskEx.CompletedTask);
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        class MyMessage
        {
        }
    }
}