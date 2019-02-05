using System.Threading.Tasks;
using System.Threading;
using System;

namespace CompositeVideoMonitor {
    public class Logger {
        readonly VideoMonitor Monitor;
        readonly Renderer Renderer;
        public Logger(Renderer renderer, VideoMonitor monitor) {
            Renderer = renderer;
            Monitor = monitor;
        }

        public void Run(CancellationToken canceller) {
            while (!canceller.IsCancellationRequested) {
                Console.WriteLine($"FPS:{Renderer.FPS,5:F2} SPF:{Monitor.SPF,7:F2} DPS:{Monitor.DPS,6} Dots:{Renderer.DotCount,7}");
                Task.Delay(100).Wait();
            }
        }
    }
}