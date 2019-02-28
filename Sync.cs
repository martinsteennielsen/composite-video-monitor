using System;
using System.Linq;

namespace CompositeVideoMonitor {

    public class Sync {
        readonly PhaseDetector HPhaseDetect, VPhaseDetect;

        public Sync(TimingConstants timing, ISignal compositeInput) {
            var hReference = new SquareSignal(frequency: timing.HFreq, onTime: timing.SyncTimes.LineSyncTime);
            HPhaseDetect = new PhaseDetector(frequency: timing.HFreq, blackLevel: timing.SyncTimes.BlackLevel, syncWidth: timing.SyncTimes.LineSyncTime, reference: hReference, signal: compositeInput);
            double syncWidth = 0.5 * timing.LineTime - timing.SyncTimes.LineSyncTime;
            var vReference = new SquareSignal(frequency: timing.VFreq, onTime: syncWidth);
            VPhaseDetect = new PhaseDetector(frequency: timing.VFreq, blackLevel: timing.SyncTimes.BlackLevel, syncWidth: syncWidth, reference: vReference, signal: compositeInput);
        }

        public double HPhase = 0;
        public double VPhase = 0;

        public void SpendTime(double time) {
            HPhaseDetect.SpendTime(time);
            VPhaseDetect.SpendTime(time);
            if (HPhaseDetect.TryGetPhase(out var hphase)) {
                HPhase = -hphase;
            }
            if (VPhaseDetect.TryGetPhase(out var vphase)) {
                VPhase = -vphase;
            }
        }

        class PhaseDetector {

            readonly double SyncWidth, Frequency, BlackLevel;
            readonly ISignal Reference;
            readonly ISignal Signal;
            readonly SyncPulse[] SyncSignalPulses = new SyncPulse[3];
            readonly SyncPulse[] SyncReferencePulses = new SyncPulse[3];


            public PhaseDetector(double frequency, double blackLevel, double syncWidth, ISignal reference, ISignal signal) {
                Frequency = frequency;
                BlackLevel = blackLevel;
                Signal = signal;
                Reference = reference;
                SyncWidth = syncWidth;
            }

            public void SpendTime(double time) {
                bool isSyncRef = Reference.Get(time) == 1;
                bool isSyncSig = Signal.Get(time) < BlackLevel;
                CurrentPulse(SyncSignalPulses).SpendTime(time, sync: isSyncSig, min: 0.9 * SyncWidth, max: 1.1 * SyncWidth);
                CurrentPulse(SyncReferencePulses).SpendTime(time, sync: isSyncRef, min: 0.9 * SyncWidth, max: 1.1 * SyncWidth);
            }

            public bool TryGetPhase(out double phase) {
                double? timeDiff = TimeDifference(SyncSignalPulses, SyncReferencePulses);
                phase = 0;
                if (timeDiff == null) { return false; }
                phase = Math.PI * (timeDiff.Value) * Frequency;
                if (phase > Math.PI || phase < -Math.PI) { return false; }
                return true;
            }

            private double? TimeDifference(SyncPulse[] signalSync, SyncPulse[] refSync) {
                int c = 0; double dif = 0;
                foreach (var (si, rf) in signalSync.Zip(refSync, (x, y) => (x, y))) {
                    if (si == null || rf == null) { continue; }
                    if (si.Start == null || rf.Start == null) { continue; }
                    if (si.End == null || rf.End == null) { continue; }
                    dif += si.Start.Value - rf.Start.Value;
                    if (si.Start.Value < rf.Start.Value) {
                        dif += (1d / Frequency);
                    }
                    c++;
                }
                return c != 0 ? (double?)dif / c : null;
            }

            private SyncPulse CurrentPulse(SyncPulse[] pulses) {
                if (pulses[0] == null || (pulses[0].End != null && (pulses[0].Start != null))) {
                    pulses[2] = pulses[1];
                    pulses[1] = pulses[0];
                    pulses[0] = new SyncPulse();
                }
                return pulses[0];
            }

            class SyncPulse {
                public double? Start;
                public double? End;
                public void SpendTime(double time, bool sync, double min, double max) {
                    if (Start == null && sync) {
                        Start = time;
                    } else if (End == null && Start != null && !sync) {
                        End = time;
                        if (End - Start > max) {
                            Start = null; End = null;
                        }
                        if (End - Start < min) {
                            Start = null; End = null;
                        }
                    }
                }
            }
        }
    }
}