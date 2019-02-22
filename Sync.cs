using System;

namespace CompositeVideoMonitor {

    public class HSync {
        TimingConstants Timing;
        double SyncTime, SyncRefTime = 0;
        SquareSignal HSyncRef;

        public HSync(TimingConstants timing) {
            Timing = timing;
            HSyncRef = new SquareSignal(frequency: timing.HFreq, onTime: timing.LineTime / 32d);
        }

        public double GetPhase(double time, double value) {
            bool syncRef = HSyncRef.Get(time) == 1;
            bool syncSig = value < Timing.BlackLevel;
            if (syncSig) {
                SyncTime = time;
            }
            if (syncRef) {
                SyncRefTime = time;
            }
            return Math.PI * (SyncRefTime - SyncTime) / Timing.LineTime;
        }
    }
}