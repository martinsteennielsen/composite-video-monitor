namespace CompositeVideoMonitor {

    public class VideoMonitor : ISignal {
        public readonly Tube Tube;
        readonly ISignal CompositeInput;
        readonly IPeriodic VOsc, HOsc;
        readonly Sync Sync;

        public VideoMonitor(TimingConstants timing, ISignal compositeInput) {
            CompositeInput = compositeInput;
            VOsc = new SawtoothSignal { Frequency = timing.VFreq, Phase = 0 };
            HOsc = new SawtoothSignal { Frequency = timing.HFreq, Phase = 0 };
            Sync = new Sync(timing, compositeInput, VOsc, HOsc);
            Tube = new Tube(timing);
        }

        internal double ElapseTime(double startTime, double endTime) =>
            Tube.ElapseTime(startTime, endTime, compositeSignal: this, hosc: HOsc, vosc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.ElapseTime(time);
            return res;
        }
    }
}