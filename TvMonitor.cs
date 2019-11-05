using System;

namespace CompositeVideoMonitor {

    public class TvMonitor : ISignal {
        readonly Tube Tube;
        readonly ISignal CompositeInput;
        readonly IPeriodic VOsc, HOsc;
        readonly Sync Sync;
        readonly Controls Controls;

        public TvMonitor(Controls controls, Tube tube, ISignal compositeInput) {
            Controls = controls;
            CompositeInput = compositeInput;
            Tube = tube;
            VOsc = new SawtoothSignal { Frequency = controls.TvNorm.Vertical, Phase = 0 };
            HOsc = new SawtoothSignal { Frequency = controls.TvNorm.Horizontal, Phase = 0 };
            Sync = new Sync(compositeInput, VOsc, HOsc);
        }

        public double ElapseTime(double startTime, double endTime) =>
            Tube.ElapseTime(startTime, endTime, compositeSignal: this, hosc: HOsc, vosc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.ElapseTime(time);
            return res;
        }

        public Picture GetPicture() => 
            Tube.GetPicture(HOsc, VOsc, string.Format(Controls.Info));
    }
}