using System.Diagnostics;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public class TimeKeeper {
        private const int MinimumTaskWaitTimeMs = 5;
        readonly TimingConstants Timing;
        readonly Controls Controls;

        public TimeKeeper(TimingConstants timing, Controls controls) {
            Timing = timing;
            Controls = controls;
        }

        public async Task<double> GetElapsedTimeAsync() {
            double simulatedDotTime = 1d / Timing.BandwidthFreq;
            if (Controls.ZoomT == 0) {
                await Task.Delay(200);
                var step = Controls.SingleStep();
                return step * simulatedDotTime;
            } else {
                double realDotTime = simulatedDotTime / Controls.ZoomT;
                double dotsPrSimulation = 0.005 / realDotTime;
                if (dotsPrSimulation < 1) {
                    await Task.Delay((int)(MinimumTaskWaitTimeMs / dotsPrSimulation));
                    return simulatedDotTime;
                } else {
                    await Task.Delay(MinimumTaskWaitTimeMs);
                    return dotsPrSimulation * simulatedDotTime;
                }
            }
        }
    }
}
