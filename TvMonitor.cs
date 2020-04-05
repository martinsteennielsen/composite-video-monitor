using System;

namespace CompositeVideoMonitor {

    public class TvMonitor : ISignal {
        readonly Tube Tube;
        readonly Input CompositeInput;
        readonly IPeriodic VOsc, HOsc;
        readonly Sync Sync;
        readonly Controls Controls;
        readonly ISignal Noise = new NoiseSignal();

        public TvMonitor(Controls controls, Tube tube, Input compositeInput) {
            Controls = controls;
            CompositeInput = compositeInput;

            Tube = tube;
            VOsc = new SawtoothSignal(freq : controls.TvNorm.Vertical);
            HOsc = new SawtoothSignal(freq : controls.TvNorm.Horizontal);
            Sync = new Sync(VOsc, HOsc);
        }

        public double ElapseTime(double startTime, double endTime) =>
            Tube.ElapseTime(startTime, endTime, compositeSignal: this, hosc: HOsc, vosc: VOsc);

        double ISignal.Get(double time) {
            if (!CompositeInput.TryGet(time, out var res, out var sampleRate)) {
                res = Noise.Get(time);
                Controls.TvNorm = TvNorm.pPal;
            } else {
                Controls.TvNorm = Controls.TvNorm.WithFrequency(VOsc.Frequency);
                Controls.TvNorm = Controls.TvNorm.WithBandWidth(sampleRate);
            }
            Sync.ElapseTime(time, res);
            return res;
        }

        public Picture GetPicture() => 
            Tube.GetPicture(HOsc, VOsc, string.Format(Controls.Info));
    }
}