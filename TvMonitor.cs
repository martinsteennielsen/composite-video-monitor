namespace CompositeVideoMonitor {

    public class TvMonitor : ISignal {
        readonly Tube Tube;
        readonly ISignal CompositeInput;
        readonly IPeriodic VOsc, HOsc;
        readonly Sync Sync;

        public TvMonitor(TvNorm tvNorm, Tube tube, ISignal compositeInput) {
            CompositeInput = compositeInput;
            Tube = tube;
            VOsc = new SawtoothSignal { Frequency = tvNorm.Frequencies.Vertical, Phase = 0 };
            HOsc = new SawtoothSignal { Frequency = tvNorm.Frequencies.Horizontal, Phase = 0 };
            Sync = new Sync(tvNorm, compositeInput, VOsc, HOsc);
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