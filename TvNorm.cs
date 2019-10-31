namespace CompositeVideoMonitor {

    public struct TvNorm {
        public static TvNorm iPal => new TvNorm(frequencies: TvFrequencies.iPal, sync: TvSync.Pal);
        public static TvNorm pPal => new TvNorm(frequencies: TvFrequencies.pPal, sync: TvSync.Pal);

        public readonly TvSync Sync;
        public readonly TvFrequencies Frequencies;

        TvNorm(TvFrequencies frequencies, TvSync sync) {
            Frequencies = frequencies; Sync = sync;
        }
    }

    public struct TvSync {

        public static TvSync Pal => new TvSync(lineBlankingTime: 12.05e-6, lineSyncTime: 4.7e-6, frontPorchTime: 1.65e-6, equPulseTime: 2.3e-6, verticalSerrationTime: 4.7e-6, blackLevel: 0.3);

        public readonly double LineBlankingTime;
        public readonly double LineSyncTime;
        public readonly double FrontPorchTime;
        public readonly double EquPulseTime;
        public readonly double VerticalSerrationTime;
        public readonly double BlackLevel;

        TvSync(double lineBlankingTime, double lineSyncTime, double frontPorchTime, double equPulseTime, double verticalSerrationTime, double blackLevel) {
            LineBlankingTime = lineBlankingTime; LineSyncTime = lineSyncTime; FrontPorchTime = frontPorchTime;
            EquPulseTime = equPulseTime; VerticalSerrationTime = verticalSerrationTime; BlackLevel = blackLevel;
        }
    }

    public struct TvFrequencies {

        public static TvFrequencies iPal => new TvFrequencies(horizontal: 15625, vertical: 50, bandwidth: 5e6, frameTime: 1.0/25);
        public static TvFrequencies pPal => new TvFrequencies(horizontal: 15625, vertical: 25, bandwidth: 5e6);

        public readonly double Horizontal;
        public readonly double Vertical;
        public readonly double Bandwidth;
        public readonly double DotTime;
        public readonly double FrameTime;
        public readonly double LineTime;

        TvFrequencies(double horizontal, double vertical, double bandwidth, double? frameTime = null) {
            Horizontal = horizontal; Vertical = vertical; Bandwidth = bandwidth;
            LineTime = 1d / Horizontal; FrameTime = frameTime ?? (1d / Vertical); DotTime = 1d / Bandwidth;
        }
    }
}
