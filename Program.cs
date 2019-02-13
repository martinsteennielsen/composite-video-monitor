using System.Threading.Tasks;
using System.Threading;
using NetMQ;

namespace CompositeVideoMonitor {
    class Program {
        static void Main(string[] args) {
            var logger = new Logger();
            //var timing = new PalTiming(dotSize: 1, framesPrSec: 20);
            var timing = new PalTiming();
            using (var input = new Input(timing)) {
                var monitor = new VideoMonitor(timing, signal: new InputSignal(input), logger: logger);
                using (Renderer renderer = new Renderer(monitor, timing, logger, 600, 600, "PAL")) {
                    var canceller = new CancellationTokenSource();
                    Task.Run(() => logger.Run(canceller.Token));
                    Task.Run(() => monitor.Run(canceller.Token));
                    renderer.Run();
                    canceller.Cancel();
                }
            }
            NetMQ.NetMQConfig.Cleanup(block: false);
        }
    }
}