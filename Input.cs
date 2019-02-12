using NetMQ;
using NetMQ.Sockets;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {
    public class Input {
        readonly TimingConstants Timing;
        readonly int PacketSize;
        readonly BlockingCollection<byte> Queue = new BlockingCollection<byte>();

        public double Get() => (Queue.Take()/255.0);

        public Input(TimingConstants timing) {
            Timing = timing;
            PacketSize = (int)(timing.LineTime / timing.DotTime);
        }

        public async Task Run(CancellationToken canceller) {

            using (var sub = new SubscriberSocket()) {
                sub.Connect("tcp://127.0.0.1:10000");
                sub.Subscribe("");
                while (!canceller.IsCancellationRequested) {
                    var buffer = sub.ReceiveFrameBytes();
                    for (int i = 0; i < buffer.Length; i++) {
                        Queue.Add(buffer[i]);
                    }
                }
            }
        }
    }
}
