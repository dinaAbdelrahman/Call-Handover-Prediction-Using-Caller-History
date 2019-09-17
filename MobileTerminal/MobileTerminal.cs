namespace VerticalHandoverPrediction.Mobile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VerticalHandoverPrediction.Cac;
    using VerticalHandoverPrediction.CallSession;
    using VerticalHandoverPrediction.Network;
    using VerticalHandoverPrediction.Simulator;

    public class MobileTerminal : IMobileTerminal
    {
        public Guid MobileTerminalId { get; set; }
        public Guid SessionId { get; private set; }
        public Modality Modality { get; private set; }
        public MobileTerminalState State { get; private set; }
        private readonly List<CallLog> _callLogs;
        public IReadOnlyCollection<CallLog> CallLogs => _callLogs;
        public bool IsActive { get; private set; }

        public MobileTerminal(Modality modality)
        {
            MobileTerminalId = Guid.NewGuid();
            Modality = modality;
            _callLogs = new List<CallLog>();
            State = MobileTerminalState.Idle;
            IsActive = false;
        }

        public void AddCallLog(CallLog log)
        {
            if (log is null)
            {
                throw new VerticalHandoverPredictionException($"{nameof(log)} is invalid");
            }

            _callLogs.Add(log);
        }

        public void SetState(MobileTerminalState state) => State = state;

        public void SetSessionId(Guid sessionId) => SessionId = sessionId;

        public void EndCall(CallEndedEvent evt)
        {
            var session = HetNet._HetNet.Rats
                .SelectMany(x => x.OngoingSessions)
                .FirstOrDefault(x => x.SessionId == this.SessionId);

            var rat = HetNet._HetNet.Rats
                .FirstOrDefault(x => x.RatId == session.RatId);

            var call = session.ActiveCalls.FirstOrDefault(x => x.CallId == evt.CallId);

            rat.RealeaseNetworkResources(call.Service.ComputeRequiredNetworkResources());

            session.ActiveCalls.Remove(call);

            var state = UpdateMobileTerminalState(session);

            session.SessionSequence.Add(state);

            if (state == MobileTerminalState.Idle)
            {
                EndSession(session, evt.Time, rat);
                return;
            }

            call = null;
            session = null;
        }

        public MobileTerminalState UpdateMobileTerminalState(ISession session)
        {
            var state = MobileTerminalState.Idle;
            var ongoingServices = session.ActiveCalls.Select(x => x.Service);
            if (ongoingServices.Count() == 1)
            {
                switch (ongoingServices.ElementAt(0))
                {
                    case Service.Voice:
                        state = MobileTerminalState.Voice;
                        SetState(state);
                        return state;
                    case Service.Data:
                        state = MobileTerminalState.Data;
                        SetState(state);
                        return state;
                    case Service.Video:
                        state = MobileTerminalState.Video;
                        SetState(state);
                        return state;
                }
            }

            if (ongoingServices.Count() == 3)
            {
                state = MobileTerminalState.VoiceDataVideo;
                SetState(state);
                return state;
            }

            if (ongoingServices.Count() == 2)
            {
                if (ongoingServices.Intersect(new List<Service> { Service.Voice, Service.Data }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VoiceData;
                    SetState(state);
                    return state;
                }
                if (ongoingServices.Intersect(new List<Service> { Service.Voice, Service.Video }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VoiceVideo;
                    SetState(state);
                    return state;
                }
                if (ongoingServices.Intersect(new List<Service> { Service.Video, Service.Data }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VideoData;
                    SetState(state);
                    return state;
                }
            }

            SetState(state);
            return state;
        }

        private void EndSession(ISession session, DateTime end, IRat rat)
        {
            rat.RemoveSession(session);
            session.SetEndTime(end);

            SessionId = Guid.Empty;

            var callLog = new CallLog
            {
                UserId = MobileTerminalId,
                SessionId = session.SessionId,
                Duration = session.End.Subtract(session.Start),

                SessionSequence = String.Join("", session.SessionSequence.Select(x => (int)x))
            };

            IsActive = false;

            AddCallLog(callLog);

            if (Simulator.NetworkSimulator._NetworkSimulator.UseCallLogs)
            {
                Utils.CsvUtils._Instance.Write<CallLogMap, CallLog>(callLog, $"{Environment.CurrentDirectory}/calllogs.csv");
            }

            HetNet._HetNet.TotalSessions++;
        }

        public MobileTerminalState UpdateMobileTerminalStateWhenAdmitingNewCallToOngoingSession(IList<ICall> activeCalls)
        {

            if (activeCalls.Count() == 1 || activeCalls.Count() == 0 || activeCalls.Count() > 3)
            {
                throw new VerticalHandoverPredictionException($"{nameof(activeCalls)} can not have {activeCalls.Count()} calls");
            }

            var ongoingServices = activeCalls.Select(x => x.Service);

            MobileTerminalState state = MobileTerminalState.VoiceDataVideo;
            if (activeCalls.Count() == 2)
            {
                if (ongoingServices.Intersect(new List<Service> { Service.Voice, Service.Data }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VoiceData;
                    SetState(state);
                }
                if (ongoingServices.Intersect(new List<Service> { Service.Voice, Service.Video }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VoiceVideo;
                    SetState(state);
                }
                if (ongoingServices.Intersect(new List<Service> { Service.Video, Service.Data }).Count() == ongoingServices.Count())
                {
                    state = MobileTerminalState.VideoData;
                    SetState(MobileTerminalState.VideoData);
                }
            }
            else
            {
                SetState(MobileTerminalState.VoiceDataVideo);
            }
            return state;
        }
    }
}