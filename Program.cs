using System.Threading.Tasks;
using System.Threading;
using NetMQ;

namespace CompositeVideoMonitor {
    class Program {

        static void Main(string[] args) {
            var canceller = new CancellationTokenSource();
            var logger = new Logger();

            //var timing = new PalTiming(dotSize: 1, framesPrSec: 20);
            var timing = new PalTiming();

            using (var compositeInput = new Input(address: "tcp://127.0.0.1:10001")) {
                var monitor = new VideoMonitor(timing, signal: compositeInput, logger: logger);
                Task.Run(() => logger.Run(canceller.Token));
                Task.Run(() => monitor.Run(canceller.Token));
                using (Renderer renderer = new Renderer(monitor, timing, logger, 600, 600, "PAL")) {
                    renderer.Run();
                }
                canceller.Cancel();
            }

            NetMQConfig.Cleanup(block: false);
        }
    }
}