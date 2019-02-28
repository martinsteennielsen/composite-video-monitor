namespace CompositeVideoMonitor {

    public class VideoMonitor : ISignal {
        readonly ISignal CompositeInput;
        readonly SawtoothSignal VOsc, HOsc;
        readonly Sync Sync;
        public readonly Tube Tube;

        public VideoMonitor(TimingConstants timing, ISignal compositeInput) {
            CompositeInput = compositeInput;
            Sync = new Sync(timing, compositeInput);
            VOsc = new SawtoothSignal { Frequency = timing.VFreq };
            HOsc = new SawtoothSignal { Frequency = timing.HFreq };
            Tube = new Tube(timing);
        }

        internal double SpendTime(double startTime, double endTime) =>
            Tube.SpendTime(startTime, endTime, compositeSignal: this, hOsc: HOsc, vOsc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.SpendTime(time);
            VOsc.Phase = Sync.VPhase;
            HOsc.Phase = Sync.HPhase;
            return res;
        }
    }
}