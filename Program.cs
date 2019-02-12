﻿using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor {
    class Program {
        static void Main(string[] args) {
            var logger = new Logger();
            var timing = new PalTiming(dotSize: 20, framesPrSec: 0.3);
            // var timing = new PalTiming();
            var monitor = new VideoMonitor(timing, signal: new NoiseSignal(), logger: logger);
            using (Renderer renderer = new Renderer(monitor, timing, logger, 600, 600, "PAL")) {
                var canceller = new CancellationTokenSource();
                Task.Run(() => logger.Run(canceller.Token) );
                Task.Run(() => monitor.Run(canceller.Token) );
                renderer.Run(25);
                canceller.Cancel();
            }
        }
    }
}