using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {
    class Program {

        static void Main(string[] args) {
            var canceller = new CancellationTokenSource();
            var logger = new Logger();
            var control = new Controls();

            //var timing = new PalTiming(dotSize: 1, framesPrSec: 20);
            var timing = new PalTiming();

            using (var compositeInput = new Input(control, address: "tcp://127.0.0.1:10001")) {
                Task.Run(() => logger.Run(canceller.Token));
                var monitor = new VideoMonitor(timing, signal: compositeInput, logger: logger);
                Task.Run(() => monitor.Run(canceller.Token));
                using (var renderer = new Renderer(control, monitor, timing, logger, 600, 600, "PAL")) {
                    renderer.Run();
                }
                canceller.Cancel();
            }
        }
    }
}