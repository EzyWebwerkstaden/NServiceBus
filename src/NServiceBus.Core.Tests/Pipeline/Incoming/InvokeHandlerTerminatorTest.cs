﻿namespace NServiceBus.Core.Tests.Pipeline.Incoming
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class InvokeHandlerTerminatorTest
    {
        [Test]
        public async Task When_saga_found_and_handler_is_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();
            var saga = new FakeSaga();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
            var behaviorContext = CreateBehaviorContext(messageHandler);
            AssociateSagaWithMessage(saga, behaviorContext);

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task When_saga_not_found_and_handler_is_saga_should_not_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();
            var saga = new FakeSaga();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
            var behaviorContext = CreateBehaviorContext(messageHandler);
            var sagaInstance = AssociateSagaWithMessage(saga, behaviorContext);
            sagaInstance.MarkAsNotFound();

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.IsFalse(handlerInvoked);
        }

        [Test]
        public async Task When_saga_not_found_and_handler_is_not_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);
            var sagaInstance = AssociateSagaWithMessage(new FakeSaga(), behaviorContext);
            sagaInstance.MarkAsNotFound();

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task When_no_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task Should_invoke_handler_with_current_message()
        {
            object receivedMessage = null;
            var terminator = new InvokeHandlerTerminator();
            var messageHandler = CreateMessageHandler((i, m, ctx) => receivedMessage = m, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.AreSame(behaviorContext.MessageBeingHandled, receivedMessage);
        }

        [Test]
        public async Task Should_indicate_when_no_transaction_scope_is_present()
        {
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => { }, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);

            Assert.IsFalse(behaviorContext.Extensions.Get<InvokeHandlerTerminator.State>().ScopeWasPresent);
        }

        [Test]
        public async Task Should_indicate_when_transaction_scope_is_present()
        {
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => { }, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            using (var scope = new TransactionScope())
            {
                await terminator.Invoke(behaviorContext, _ => TaskEx.CompletedTask);
                scope.Complete();
            }

            Assert.IsTrue(behaviorContext.Extensions.Get<InvokeHandlerTerminator.State>().ScopeWasPresent);
        }

        static ActiveSagaInstance AssociateSagaWithMessage(FakeSaga saga, IInvokeHandlerContext behaviorContext)
        {
            var sagaInstance = new ActiveSagaInstance(saga, SagaMetadata.Create(typeof(FakeSaga), new List<Type>(), new Conventions()));
            behaviorContext.Extensions.Set(sagaInstance);
            return sagaInstance;
        }

        MessageHandler CreateMessageHandler(Action<object, object, IMessageHandlerContext> invocationAction, object handlerInstance)
        {
            var messageHandler = new MessageHandler((instance, message, handlerContext) =>
            {
                invocationAction(instance, message, handlerContext);
                return TaskEx.CompletedTask;
            }, handlerInstance.GetType())
            {
                Instance = handlerInstance
            };
            return messageHandler;
        }

        static IInvokeHandlerContext CreateBehaviorContext(MessageHandler messageHandler)
        {
            var behaviorContext = new InvokeHandlerContext(
                messageHandler,
                "messageId",
                "replyToAddress",
                new Dictionary<string, string>(),
                null,
                null,
                null,
                new RootContext(null, null));

            return behaviorContext;
        }

        class FakeSaga : Saga<FakeSaga.FakeSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FakeSagaData> mapper)
            {
            }

            public class FakeSagaData : ContainSagaData
            {
            }
        }

        class StartMessage
        {
        }

        class FakeMessageHandler
        {
        }
    }
}