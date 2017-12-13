// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for working with events.
    /// </summary>
    public static class EventExtensions
    {
        private static readonly Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var settings = Serializer.CloneSettings();
            settings.ContractResolver = Serializer.AreDefaultSerializerSettingsConfigured ? new EventContractResolver() : settings.ContractResolver;
            return settings;
        });

        /// <summary>
        /// Creates a <see cref="StorableEvent" /> based on the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <param name="serializationFunc">Optional func to call to serialize the domain event.</param>
        public static StorableEvent ToStorableEvent(this IEvent domainEvent, CustomSerialize serializationFunc = null)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            string eventStreamName = null;
            var aggregateType = domainEvent.AggregateType();
            eventStreamName = aggregateType != null
                                  ? AggregateType.EventStreamName(aggregateType)
                                  : ((dynamic) domainEvent).EventStreamName;

            return new StorableEvent
            {
                Actor = domainEvent.Actor(),
                StreamName = eventStreamName,
                SequenceNumber = domainEvent.SequenceNumber,
                AggregateId = domainEvent.AggregateId,
                Type = domainEvent.EventName(),
                Body = serializationFunc == null ? JsonConvert.SerializeObject(domainEvent, Formatting.None, serializerSettings.Value) : serializationFunc(domainEvent),
                Timestamp = domainEvent.Timestamp,
                ETag = domainEvent.ETag,
                Id = domainEvent.AbsoluteSequenceNumber()
            };
        }

        /// <summary>
        /// Creates a <see cref="StorableEvent" /> based on the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <param name="serializationFunc">Optional func to call to serialize the domain event.</param>
        internal static StorableEvent ToStorableEvent<TAggregate>(this IEvent<TAggregate> domainEvent, CustomSerialize serializationFunc = null)
            where TAggregate : IEventSourced =>
                new StorableEvent
                {
                    Actor = domainEvent.Actor(),
                    StreamName = AggregateType<TAggregate>.EventStreamName,
                    SequenceNumber = domainEvent.SequenceNumber,
                    AggregateId = domainEvent.AggregateId,
                    Type = domainEvent.EventName(),
                    Body = serializationFunc == null ? JsonConvert.SerializeObject(domainEvent, Formatting.None, serializerSettings.Value) : serializationFunc(domainEvent),
                    Timestamp = domainEvent.Timestamp,
                    ETag = domainEvent.ETag
                };

        /// <summary>
        /// Creates a domain event from a <see cref="StorableEvent" />.
        /// </summary>
        /// <param name="storableEvent">The storable event.</param>
        /// <param name="deserializationFunc">Optional func to call to deserialize the storable event.</param>
        /// <returns>A deserialized domain event.</returns>
        public static IEvent ToDomainEvent(this StorableEvent storableEvent, CustomDeserialize deserializationFunc = null) =>
            Serializer.DeserializeEvent(
                storableEvent.StreamName,
                storableEvent.Type,
                storableEvent.AggregateId,
                storableEvent.SequenceNumber,
                storableEvent.Timestamp,
                storableEvent.Body,
                storableEvent.Id,
                serializerSettings.Value,
                deserializationFunc: deserializationFunc,
                etag: storableEvent.ETag);
    }
}