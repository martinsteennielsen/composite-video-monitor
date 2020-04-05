using System;

namespace CompositeVideoMonitor {

    public class Sync {
        readonly IPeriodic HRef, VRef;
        readonly PhaseDetector HPhaseDetect, VPhaseDetect;

        public Sync(IPeriodic vref, IPeriodic href) {
            HRef = href;
            VRef = vref;
            HPhaseDetect = new PhaseDetector(blackLevel: TvSync.BlackLevel, reference: href, syncWidth: TvSync.LineSyncTime);
            double vSyncWidth = 0.5 * (1d / href.Frequency) - TvSync.LineSyncTime;
            VPhaseDetect = new PhaseDetector(blackLevel: TvSync.BlackLevel, reference: vref, syncWidth: vSyncWidth);
        }

        public void ElapseTime(double time, double signalValue) {
            if (HPhaseDetect.ElapseTimeAndTryGetPhase(time, signalValue, out var hphase, out var _)) {
                HRef.Phase = -hphase;
            }
            if (VPhaseDetect.ElapseTimeAndTryGetPhase(time, signalValue, out var vphase, out var syncFreq)) {
                if (syncFreq > 0.2 * VRef.Frequency && syncFreq < 5*VRef.Frequency) {
                    VRef.Phase = -vphase;
                    VRef.Frequency = syncFreq;
                }
            }
        }

        class PhaseDetector {
            readonly double SyncWidth, BlackLevel;
            readonly IPeriodic Reference;

            double? SyncStartTime, SyncEndTime;
            double LastSyncTime = 0;

            public PhaseDetector(double blackLevel, IPeriodic reference, double syncWidth) {
                BlackLevel = blackLevel;
                SyncWidth = syncWidth;
                Reference = reference;
            }

            public bool ElapseTimeAndTryGetPhase(double time, double signalValue, out double phase, out double syncFreq) {
                phase =0;
                syncFreq = Reference.Frequency;
                bool sync = signalValue < 0.5*BlackLevel;                
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
                syncFreq = 1.0 / (time - LastSyncTime);
                LastSyncTime = time;
                phase = Math.PI * syncTime * Reference.Frequency;
                if (phase > Math.PI/2) { phase -= Math.PI; }
                SyncStartTime = SyncEndTime = null;
                return true;
            }
        }
    }
}