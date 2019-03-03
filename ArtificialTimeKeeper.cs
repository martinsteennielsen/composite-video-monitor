using System;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public class ArtificialTimeKeeper {

        private const int TaskWaitTimeMs = 5;
        readonly TimingConstants Timing;
        readonly Controls Controls;

        public ArtificialTimeKeeper(TimingConstants timing, Controls controls) {
            Timing = timing;
            Controls = controls;
        }

        double lastTime = 0;
        public async Task<(double, double)> GetElapsedTimeAsync(Func<int> singleStep) {
            double startTime = lastTime;
            double simulatedDotTime = 1d / Timing.BandwidthFreq;
            if (Controls.ZoomT == 0) {
                await Task.Delay(200);
                var step = singleStep();
                return (step * simulatedDotTime, 0);
            } else {
                double realDotTime = simulatedDotTime / Controls.ZoomT;
                double dotsPrSimulation = TaskWaitTimeMs*0.001 / realDotTime;
                if (dotsPrSimulation < 1) {
                    await Task.Delay((int)(TaskWaitTimeMs / dotsPrSimulation));
                    return (simulatedDotTime, 0);
                } else {
                    await Task.Delay(TaskWaitTimeMs);
                    return (dotsPrSimulation * simulatedDotTime, 0);
                }
            }
        }
    }

}