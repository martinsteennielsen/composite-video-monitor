using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor {
    class Program {
        static void Main(string[] args) {
            var logger = new Logger();
            var timing = new PalTiming(dotSize: 20, framesPrSec: 0.3);
            // var timing = new PalTiming();
            var videoMonitor = new VideoMonitor(timing);
            using (Renderer renderer = new Renderer(videoMonitor, timing, logger, 600, 600, "PAL")) {
                var canceller = new CancellationTokenSource();
                Task.Run(() => { logger.Run(canceller.Token); });
                Task.Run(() => { videoMonitor.Run(canceller.Token, signal: new NoiseSignal(), logger: logger); });
                renderer.Run(25);
                canceller.Cancel();
            }
        }
    }
}