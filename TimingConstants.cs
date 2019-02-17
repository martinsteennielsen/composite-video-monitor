namespace CompositeVideoMonitor {
    public class TimingConstants {
        public readonly double HFreq;
        public readonly double VFreq;
        public readonly double BandwidthFreq;
        public readonly double DotTime;
        public readonly double FrameTime;
        public readonly double LineTime;

        public TimingConstants(double hFreq, double vFreq, double bandwidthFreq) {
            BandwidthFreq = bandwidthFreq;
            VFreq = vFreq;
            HFreq = hFreq;
            DotTime = 1.0 / (bandwidthFreq);
            FrameTime = 1.0 / vFreq;
            LineTime = 1.0 / (hFreq);
        }
    }

    public class PalTiming : TimingConstants {
        public PalTiming() : base(hFreq: 15625, vFreq: 50, bandwidthFreq: 5e6) {
        }
    }
}
