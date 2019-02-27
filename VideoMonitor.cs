using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public class VideoMonitor : ISignal {
        readonly Input CompositeInput;
        readonly ISignal VOsc, HOsc;
        readonly Sync Sync;
        public readonly Tube Tube;

        public VideoMonitor(TimingConstants timing, Input compositeInput) {
            CompositeInput = compositeInput;
            Sync = new Sync(timing, compositeInput);
            VOsc = new SawtoothSignal(timing.VFreq, Sync.VPhase);
            HOsc = new SawtoothSignal(timing.HFreq, Sync.HPhase);
            Tube = new Tube(timing);
        }

        internal double Calculate(double startTime, double endTime) =>
            Tube.Calculate(startTime, endTime, compositeSignal: this, hOsc: HOsc, vOsc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.Calculate(time);
            return res;
        }
    }
}