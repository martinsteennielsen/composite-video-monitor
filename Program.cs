using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor {
    class Program {
        static void Main(string[] args) {
            var logger = new Logger();
            var videoMonitor = new DebugPalMonitor();
            using (Renderer renderer = new Renderer(videoMonitor, logger, 600, 400, "PAL")) {
                var canceller = new CancellationTokenSource();
                Task.Run(() => { logger.Run(canceller.Token); });
                Task.Run(() => { videoMonitor.Run(canceller.Token, signal: new NoiseSignal(), logger: logger); });
                renderer.Run(25);
                canceller.Cancel();
            }
        }
    }
}