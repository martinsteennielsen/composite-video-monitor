using System.Threading.Tasks;
using System.Threading;
using System;

namespace CompositeVideoMonitor {

    public class Logger {
        public double SimulationsPrFrame { get; set; }
        public double FramesPrSecond { get; set; }
        public int DotsPrSimulation { get; set; }
        public int DotCount { get; set; }
        public int SkippedFrames { get; set; }

        public void Run(CancellationToken canceller) {
            while (!canceller.IsCancellationRequested) {
                Console.WriteLine($"FPS:{FramesPrSecond,5:F2} SPF:{SimulationsPrFrame,7:F2} DPS:{DotsPrSimulation,6} Dots:{DotCount,7}, Skipped frames:{SkippedFrames}");
                Task.Delay(100).Wait();
            }
        }
    }
}