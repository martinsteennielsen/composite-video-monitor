using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {
    public class Input : IDisposable {
        readonly TimingConstants Timing;
        readonly int PacketSize;
        readonly ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        readonly SubscriberSocket Subscriber;
        readonly NetMQPoller Poller;
        public double Get() {
            if (Queue.IsEmpty) { return 0.4; }
            byte val;
            while (!Queue.TryDequeue(out val));
            return val / 255.0;
        }

        public Input(TimingConstants timing) {
            Poller = new NetMQPoller();
            Timing = timing;
            PacketSize = (int)(timing.LineTime / timing.DotTime);
            Subscriber = new SubscriberSocket();
            Subscriber.SubscribeToAnyTopic();
            Subscriber.ReceiveReady += Subscriber_ReceiveReady;
            Subscriber.Connect("tcp://127.0.0.1:10001");
            Poller.Add(Subscriber);
            Poller.RunAsync();
        }

        private void Subscriber_ReceiveReady(object sender, NetMQSocketEventArgs e) {
            while (Subscriber.TryReceiveFrameBytes(out var buffer)) {
                for (int i = 0; i < buffer.Length; i++) {
                    Queue.Enqueue(buffer[i]);
                }
            }
        }

        public void Dispose() {
            Subscriber.ReceiveReady -= Subscriber_ReceiveReady;
            Poller.Remove(Subscriber);
            Poller.Dispose();
            Subscriber.Dispose();
        }
    }
}
