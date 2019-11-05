using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public class RealTimeKeeper {

        private const int TaskWaitTimeMs = 5;
        readonly Controls Controls;
        readonly Stopwatch Watch = Stopwatch.StartNew();
        
        public RealTimeKeeper(Controls controls) {
            Controls = controls;
        }

        double lastTime = 0;

        public async Task<(double,double)> GetElapsedTimeAsync() {
            (double, double) el(double elapsed) {
                double skiptime = 0;
                while (elapsed > Controls.TvNorm.FrameTime) {
                    skiptime += Controls.TvNorm.FrameTime;
                    elapsed -=  Controls.TvNorm.FrameTime;
                }
                return (elapsed, skiptime);
            }
            double startTime = lastTime;
                double dotsPrSimulation = (TaskWaitTimeMs * 0.001) / Controls.TvNorm.DotTime;
                if (dotsPrSimulation < 1) {
                    await Task.Delay((int)(TaskWaitTimeMs / dotsPrSimulation));
                    lastTime = Watch.Elapsed.TotalSeconds;
                    return el(lastTime - startTime);
                } else {
                    await Task.Delay(TaskWaitTimeMs);
                    lastTime = Watch.Elapsed.TotalSeconds;
                    return el(lastTime - startTime);
                }
            }
        }
}