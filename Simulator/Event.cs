using System;
using VerticalHandoverPrediction.CallSession;

namespace VerticalHandoverPrediction.Simulator
{
    public interface IEvent
    {
        public Guid EventId { get; }  
        public DateTime Time { get; } //obtained from the call object injected via constructor
    }
}

/*
    Describe the Event interface => What is an event
    - Two types of events can be fired in the system 1. CallInitiated Event 2. Call Ended Event
    - Implement a priority queue, store the events inorder of Time they will be triggered[or priority delay queue]
    - 
 */