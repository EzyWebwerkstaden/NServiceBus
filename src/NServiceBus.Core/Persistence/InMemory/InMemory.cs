﻿namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Persistence;

    /// <summary>
    /// Used to enable InMemory persistence.
    /// </summary>
    public class InMemoryPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Ctor for InMemoryPersistence
        /// </summary>
        protected InMemoryPersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<InMemorySagaPersistence>());
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<InMemoryTimeoutPersistence>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<InMemorySubscriptionPersistence>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<InMemoryOutboxPersistence>());
            Supports<StorageType.GatewayDeduplication>(s => s.EnableFeatureByDefault<InMemoryGatewayPersistence>());
        }
    }
}