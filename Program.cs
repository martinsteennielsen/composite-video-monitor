using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor {
    class Program {
        static void Main(string[] args) {
            var monitor = new PalMonitor();
            using (Renderer renderer = new Renderer(monitor.Tube, 600, 400, "PAL")) {
                var canceller = new CancellationTokenSource();
                var stats = new Logger(renderer, monitor);
                Task.Run(() => { stats.Run(canceller.Token); });
                Task.Run(() => { monitor.Run(canceller.Token, signal: new NoiseSignal()); });
                renderer.Run(25);
                canceller.Cancel();
            }
        }
    }
}