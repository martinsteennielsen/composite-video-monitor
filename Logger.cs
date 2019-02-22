using System.Threading.Tasks;
using System.Threading;
using System;

namespace CompositeVideoMonitor {

    public class Logger {
        public double HPhase { get; set; }
        public double SimulationsPrFrame { get; set; }
        public double FramesPrSecond { get; set; }
        public int DotsPrSimulation { get; set; }
        public int DotCount { get; set; }
        public double SkippedTime { get; set; }

        public async Task Run(CancellationToken canceller) {
            while (!canceller.IsCancellationRequested) {
                Console.WriteLine($"HPA:{HPhase} FPS:{FramesPrSecond,5:F2} SPF:{SimulationsPrFrame,7:F2} DPS:{DotsPrSimulation,6} Dots:{DotCount,7}, Skipped time:{SkippedTime}");
                await Task.Delay(100);
            }
        }
    }
}