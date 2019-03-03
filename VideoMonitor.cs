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

        internal double SpendTime(double startTime, double endTime) =>
            Tube.SpendTime(startTime, endTime, compositeSignal: this, hOsc: HOsc, vOsc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.SpendTime(time);
            return res;
        }
    }
}