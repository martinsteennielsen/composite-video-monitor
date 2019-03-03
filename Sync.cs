using System;
using System.Linq;

namespace CompositeVideoMonitor {

    public class Sync {
        readonly IPeriodic HRef, VRef;
        readonly PhaseDetector HPhaseDetect, VPhaseDetect;
        readonly TimingConstants Timing;

        public Sync(TimingConstants timing, ISignal compositeInput, IPeriodic vref, IPeriodic href) {
            HRef = href;
            VRef = vref;
            Timing = timing;
            HPhaseDetect = new PhaseDetector(blackLevel: timing.SyncTimes.BlackLevel, frequency: timing.HFreq, syncWidth: timing.SyncTimes.LineSyncTime, signal: compositeInput);
            double syncWidth = 0.5 * timing.LineTime - timing.SyncTimes.LineSyncTime;
            VPhaseDetect = new PhaseDetector(blackLevel: timing.SyncTimes.BlackLevel, frequency: timing.VFreq, syncWidth: syncWidth, signal: compositeInput);
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
            readonly double SyncWidth, BlackLevel, Frequency;
            readonly ISignal Signal;

            double? SyncStartTime, SyncEndTime;

            public PhaseDetector(double blackLevel, double frequency, double syncWidth, ISignal signal) {
                BlackLevel = blackLevel;
                Signal = signal;
                SyncWidth = syncWidth;
                Frequency = frequency;
            }

            public bool ElapseTimeAndTryGetPhase(double time, out double phase) {
                phase =0;
                bool sync = Signal.Get(time) < BlackLevel;                
                if (SyncStartTime == null && sync) SyncStartTime = time;
                if (SyncStartTime == null) { return false; }
                if (SyncEndTime == null && !sync) SyncEndTime = time;
                if (SyncEndTime == null) { return false; }
                var syncDuration = SyncEndTime.Value - SyncStartTime.Value;
                if (syncDuration < 0.9 * SyncWidth || syncDuration > 1.1 * SyncWidth) {
                    SyncStartTime = SyncEndTime = null;
                    return false;
                } 
                double syncTime = SyncStartTime.Value % (1d / Frequency);
                phase = Math.PI * syncTime * Frequency;
                if (phase > Math.PI/2) { phase -= Math.PI; }
                SyncStartTime = SyncEndTime = null;
                return true;
            }
        }
    }
}