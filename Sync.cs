using System;

namespace CompositeVideoMonitor {

    public class Sync {
        readonly IPeriodic HRef, VRef;
        readonly PhaseDetector HPhaseDetect, VPhaseDetect;

        public Sync( ISignal compositeInput, IPeriodic vref, IPeriodic href) {
            HRef = href;
            VRef = vref;
            HPhaseDetect = new PhaseDetector(blackLevel: TvSync.BlackLevel, reference: href, syncWidth: TvSync.LineSyncTime, signal: compositeInput);
            double vSyncWidth = 0.5 * (1d / href.Frequency) - TvSync.LineSyncTime;
            VPhaseDetect = new PhaseDetector(blackLevel: TvSync.BlackLevel, reference: vref, syncWidth: vSyncWidth, signal: compositeInput);
        }

        public void ElapseTime(double time) {
            if (HPhaseDetect.ElapseTimeAndTryGetPhase(time, out var hphase)) {
                HRef.Phase = -hphase;
            }
            if (VPhaseDetect.ElapseTimeAndTryGetPhase(time, out var vphase)) {
                VRef.Phase = -vphase;
            }
        }

        class PhaseDetector {
            readonly double SyncWidth, BlackLevel;
            readonly IPeriodic Reference;
            readonly ISignal Signal;

            double? SyncStartTime, SyncEndTime;

            public PhaseDetector(double blackLevel, IPeriodic reference, double syncWidth, ISignal signal) {
                BlackLevel = blackLevel;
                Signal = signal;
                SyncWidth = syncWidth;
                Reference = reference;
            }

            public bool ElapseTimeAndTryGetPhase(double time, out double phase) {
                phase =0;
                bool sync = Signal.Get(time) < 0.5*BlackLevel;                
                if (SyncStartTime == null && sync) SyncStartTime = time;
                if (SyncStartTime == null) { return false; }
                if (SyncEndTime == null && !sync) SyncEndTime = time;
                if (SyncEndTime == null) { return false; }
                var syncDuration = SyncEndTime.Value - SyncStartTime.Value;
                if (syncDuration < 0.9 * SyncWidth || syncDuration > 1.1 * SyncWidth) {
                    SyncStartTime = SyncEndTime = null;
                    return false;
                } 
                double syncTime = SyncStartTime.Value % (1d / Reference.Frequency);
                phase = Math.PI * syncTime * Reference.Frequency;
                if (phase > Math.PI/2) { phase -= Math.PI; }
                SyncStartTime = SyncEndTime = null;
                return true;
            }
        }
    }
}