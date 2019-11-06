using System;

namespace CompositeVideoMonitor {

    public class TvMonitor : ISignal {
        readonly Tube Tube;
        readonly Input CompositeInput;
        readonly IPeriodic VOsc, HOsc;
        readonly Sync Sync;
        readonly Controls Controls;

        public TvMonitor(Controls controls, Tube tube, Input compositeInput) {
            Controls = controls;
            CompositeInput = compositeInput;
            Tube = tube;
            VOsc = new SawtoothSignal(freq : controls.TvNorm.Vertical);
            HOsc = new SawtoothSignal(freq : controls.TvNorm.Horizontal);
            Sync = new Sync(compositeInput, VOsc, HOsc);
        }

        public double ElapseTime(double startTime, double endTime) =>
            Tube.ElapseTime(startTime, endTime, compositeSignal: this, hosc: HOsc, vosc: VOsc);

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.ElapseTime(time);
            Controls.TvNorm = Controls.TvNorm.WithBandWidth(CompositeInput.LastSampleRate);
            return res;
        }

        public Picture GetPicture() => 
            Tube.GetPicture(HOsc, VOsc, string.Format(Controls.Info));
    }
}