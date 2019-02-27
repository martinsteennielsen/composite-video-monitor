using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public class VideoMonitor : ISignal {
        readonly Input CompositeInput;
        readonly Logger Logger;
        readonly ISignal VOsc, HOsc;
        readonly TimeKeeper TimeKeeper;
        readonly Sync Sync;
        public readonly Tube Tube;


        public VideoMonitor(Controls controls, TimingConstants timing, Input compositeInput, Logger logger) {
            Logger = logger;
            CompositeInput = compositeInput;
            Sync = new Sync(timing, compositeInput);
            VOsc = new SawtoothSignal(timing.VFreq, Sync.VPhase);
            HOsc = new SawtoothSignal(timing.HFreq, Sync.HPhase);
            Tube = new Tube(timing);
            TimeKeeper = new TimeKeeper(timing, controls);
        }

        public async Task Run(CancellationToken canceller) {
            double simulatedTime = 0;
            while (!canceller.IsCancellationRequested) {
                var (elapsedTime, skippedTime) = await TimeKeeper.GetElapsedTimeAsync();
                CompositeInput.Skip(skippedTime);

                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;
                simulatedTime = Tube.Calculate(startTime, endTime, signal: this, hOsc: HOsc, vOsc: VOsc);
            }
        }

        double ISignal.Get(double time) {
            var res = CompositeInput.Get(time);
            Sync.Calculate(time);
            return res;
        }
    }
}