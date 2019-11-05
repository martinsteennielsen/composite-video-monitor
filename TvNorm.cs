namespace CompositeVideoMonitor {

    public struct TvSync {

        public static readonly double LineBlankingTim = 12.05e-6;
        public static readonly double LineSyncTime = 4.7e-6;
        public static readonly double FrontPorchTime = 1.65e-6;
        public static readonly double EquPulseTime = 2.3e-6;
        public static readonly double VerticalSerrationTime = 4.7e-6;
        public static readonly double BlackLevel = 0.3;
    }

    public struct TvNorm {

        public static TvNorm iPal => new TvNorm(horizontal: 15625, vertical: 50, bandwidth: 5e6, frameTime: 1.0/25);
        public static TvNorm pPal => new TvNorm(horizontal: 15625, vertical: 25, bandwidth: 5e6);

        public readonly double Horizontal;
        public readonly double Vertical;
        public readonly double Bandwidth;
        public readonly double DotTime;
        public readonly double FrameTime;
        public readonly double LineTime;

        TvNorm(double horizontal, double vertical, double bandwidth, double? frameTime = null) {
            Horizontal = horizontal; Vertical = vertical; Bandwidth = bandwidth;
            LineTime = 1d / Horizontal; FrameTime = frameTime ?? (1d / Vertical); DotTime = 1d / Bandwidth;
        }
    }
}
