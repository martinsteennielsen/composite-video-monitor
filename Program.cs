﻿using System.Threading;

namespace CompositeVideoMonitor
{
    class Program {

        static void Main(string[] args) {
            using (var compositeInput = new Input(address: "tcp://127.0.0.1:10001")) {
                new Controller(new PalTiming(), compositeInput).Run(new CancellationTokenSource());
            }
        }
    }
}