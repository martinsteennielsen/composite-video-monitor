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

        public void SpendTime(double time) {
            if (HPhaseDetect.TryGetPhase(time, out var hphase)) {
                HRef.Phase = -hphase;
            }
            if (VPhaseDetect.TryGetPhase(time, out var vphase)) {
                VRef.Phase = -hphase;
            }
        }

        class PhaseDetector {
            readonly double SyncWidth, BlackLevel, Frequency;
            readonly ISignal Signal;

            public PhaseDetector(double blackLevel, double frequency, double syncWidth, ISignal signal) {
                BlackLevel = blackLevel;
                Signal = signal;
                SyncWidth = syncWidth;
                Frequency = frequency;
            }

            public bool TryGetPhase(double time, out double phase) {
                phase = 0;
                bool isSyncSig = Signal.Get(time) < BlackLevel;
                double? syncTime = SpendTime(time, sync: isSyncSig, min: 0.9 * SyncWidth, max: 1.1 * SyncWidth);
                if (syncTime != null) {
                    SyncStart = SyncEnd = null;
                    phase = Math.PI * syncTime.Value * Frequency;
                    if (phase > Math.PI/2) { phase -= Math.PI; }
                    return true;
                }
                return false;
            }

            public double? SyncStart;
            public double? SyncEnd;

            double? SpendTime(double time, bool sync, double min, double max) {
                if (SyncStart == null && sync) SyncStart = time;
                if (SyncStart == null) { return null; }
                if (SyncEnd == null && !sync) SyncEnd = time;
                if (SyncEnd == null) { return null; }
                var dur = SyncEnd.Value - SyncStart.Value;
                if (dur < 0.9 * SyncWidth || dur > 1.1 * SyncWidth) {
                    SyncStart = SyncEnd = null;
                    return null;
                } else {
                    return SyncStart % (1d / Frequency);
                }

            }
        }
    }
}