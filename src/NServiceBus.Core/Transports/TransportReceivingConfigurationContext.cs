namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Extensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides context for configuring the transport.
    /// </summary>
    public class TransportReceivingConfigurationContext
    {
        Func<CriticalError, IPushMessages> messagePumpFactory;
        Func<ICreateQueues> queueCreatorFactory;
        string connectionString;

        /// <summary>
        /// Extension settings for this transport.
        /// </summary>
        public ContextBag ExtensionSettings { get; }

        /// <summary>
        /// Global Settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        /// Connection string or null if a given transport does not require it.
        /// </summary>
        public string ConnectionString { get; }
        internal TransportReceivingConfigurationContext(ContextBag extensionSettings, ReadOnlySettings settings, string connectionString)
        {
            ExtensionSettings = extensionSettings;
            Settings = settings;
            this.connectionString = connectionString;
        }

        internal Func<CriticalError, IPushMessages> MessagePumpFactory => messagePumpFactory;
        internal Func<ICreateQueues> QueueCreatorFactory => queueCreatorFactory;

        /// <summary>
        /// Configures the message pump factory.
        /// </summary>
        /// <param name="messagePumpFactory">Message pump factory.</param>
        public void SetMessagePumpFactory(Func<CriticalError, IPushMessages> messagePumpFactory)
        {
            this.messagePumpFactory = messagePumpFactory;
        }
        
        /// <summary>
        /// Configures the queue creator.
        /// </summary>
        /// <param name="queueCreatorFactory">Queue creator.</param>
        public void SetQueueCreatorFactory(Func<ICreateQueues> queueCreatorFactory)
        {
            this.queueCreatorFactory = queueCreatorFactory;
        }

    }
}