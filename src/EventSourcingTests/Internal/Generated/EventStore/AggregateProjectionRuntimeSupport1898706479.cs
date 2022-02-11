// <auto-generated/>
#pragma warning disable
using Marten;
using Marten.Events.Aggregation;
using Marten.Internal.Storage;
using Marten.Storage;
using System;
using System.Linq;

namespace Marten.Generated.EventStore
{
    // START: AggregateProjectionLiveAggregation1898706479
    public class AggregateProjectionLiveAggregation1898706479 : Marten.Events.Aggregation.SyncLiveAggregatorBase<EventSourcingTests.Projections.QuestParty>
    {
        private readonly Marten.Events.Aggregation.AggregateProjection<EventSourcingTests.Projections.QuestParty> _aggregateProjection;

        public AggregateProjectionLiveAggregation1898706479(Marten.Events.Aggregation.AggregateProjection<EventSourcingTests.Projections.QuestParty> aggregateProjection)
        {
            _aggregateProjection = aggregateProjection;
        }



        public override EventSourcingTests.Projections.QuestParty Build(System.Collections.Generic.IReadOnlyList<Marten.Events.IEvent> events, Marten.IQuerySession session, EventSourcingTests.Projections.QuestParty snapshot)
        {
            if (!events.Any()) return null;
            EventSourcingTests.Projections.QuestParty questParty = null;
            snapshot ??= Create(events[0], session);
            foreach (var @event in events)
            {
                snapshot = Apply(@event, snapshot, session);
            }

            return snapshot;
        }


        public EventSourcingTests.Projections.QuestParty Create(Marten.Events.IEvent @event, Marten.IQuerySession session)
        {
            return new EventSourcingTests.Projections.QuestParty();
        }


        public EventSourcingTests.Projections.QuestParty Apply(Marten.Events.IEvent @event, EventSourcingTests.Projections.QuestParty aggregate, Marten.IQuerySession session)
        {
            switch (@event)
            {
                case Marten.Events.IEvent<EventSourcingTests.MembersJoined> event_MembersJoined25:
                    aggregate.Apply(event_MembersJoined25.Data);
                    break;
                case Marten.Events.IEvent<EventSourcingTests.MembersDeparted> event_MembersDeparted26:
                    aggregate.Apply(event_MembersDeparted26.Data);
                    break;
                case Marten.Events.IEvent<EventSourcingTests.QuestStarted> event_QuestStarted27:
                    aggregate.Apply(event_QuestStarted27.Data);
                    break;
            }

            return aggregate;
        }

    }

    // END: AggregateProjectionLiveAggregation1898706479
    
    
    // START: AggregateProjectionInlineHandler1898706479
    public class AggregateProjectionInlineHandler1898706479 : Marten.Events.Aggregation.AggregationRuntime<EventSourcingTests.Projections.QuestParty, System.Guid>
    {
        private readonly Marten.IDocumentStore _store;
        private readonly Marten.Events.Aggregation.IAggregateProjection _projection;
        private readonly Marten.Events.Aggregation.IEventSlicer<EventSourcingTests.Projections.QuestParty, System.Guid> _slicer;
        private readonly Marten.Storage.ITenancy _tenancy;
        private readonly Marten.Internal.Storage.IDocumentStorage<EventSourcingTests.Projections.QuestParty, System.Guid> _storage;
        private readonly Marten.Events.Aggregation.AggregateProjection<EventSourcingTests.Projections.QuestParty> _aggregateProjection;

        public AggregateProjectionInlineHandler1898706479(Marten.IDocumentStore store, Marten.Events.Aggregation.IAggregateProjection projection, Marten.Events.Aggregation.IEventSlicer<EventSourcingTests.Projections.QuestParty, System.Guid> slicer, Marten.Storage.ITenancy tenancy, Marten.Internal.Storage.IDocumentStorage<EventSourcingTests.Projections.QuestParty, System.Guid> storage, Marten.Events.Aggregation.AggregateProjection<EventSourcingTests.Projections.QuestParty> aggregateProjection) : base(store, projection, slicer, tenancy, storage)
        {
            _store = store;
            _projection = projection;
            _slicer = slicer;
            _tenancy = tenancy;
            _storage = storage;
            _aggregateProjection = aggregateProjection;
        }



        public override async System.Threading.Tasks.ValueTask<EventSourcingTests.Projections.QuestParty> ApplyEvent(Marten.IQuerySession session, Marten.Events.Projections.EventSlice<EventSourcingTests.Projections.QuestParty, System.Guid> slice, Marten.Events.IEvent evt, EventSourcingTests.Projections.QuestParty aggregate, System.Threading.CancellationToken cancellationToken)
        {
            switch (evt)
            {
                case Marten.Events.IEvent<EventSourcingTests.MembersJoined> event_MembersJoined28:
                    aggregate ??= new EventSourcingTests.Projections.QuestParty();
                    aggregate.Apply(event_MembersJoined28.Data);
                    return aggregate;
                case Marten.Events.IEvent<EventSourcingTests.MembersDeparted> event_MembersDeparted29:
                    aggregate ??= new EventSourcingTests.Projections.QuestParty();
                    aggregate.Apply(event_MembersDeparted29.Data);
                    return aggregate;
                case Marten.Events.IEvent<EventSourcingTests.QuestStarted> event_QuestStarted30:
                    aggregate ??= new EventSourcingTests.Projections.QuestParty();
                    aggregate.Apply(event_QuestStarted30.Data);
                    return aggregate;
            }

            return aggregate;
        }


        public EventSourcingTests.Projections.QuestParty Create(Marten.Events.IEvent @event, Marten.IQuerySession session)
        {
            return new EventSourcingTests.Projections.QuestParty();
        }

    }

    // END: AggregateProjectionInlineHandler1898706479
    
    
}
