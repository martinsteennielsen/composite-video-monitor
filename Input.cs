using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;

namespace CompositeVideoMonitor {

    public class Input : ISignal, IDisposable {
        readonly ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        readonly SubscriberSocket Subscriber;
        readonly NetMQPoller Poller;

        public double Get(double _) {
            if (Queue.IsEmpty) { return 0.4; }
            byte signalValue;
            while (!Queue.TryDequeue(out signalValue));
            return signalValue / 255.0;
        }

        public Input(string address) {
            Poller = new NetMQPoller();

            Subscriber = new SubscriberSocket();
            Poller.Add(Subscriber);
            Poller.RunAsync();

            Subscriber.SubscribeToAnyTopic();
            Subscriber.ReceiveReady += ReceiveReady;
            Subscriber.Connect(address);
        }

        private void ReceiveReady(object _, NetMQSocketEventArgs __) {
            while (Subscriber.TryReceiveFrameBytes(out var buffer)) {
                for (int i = 0; i < buffer.Length; i++) {
                    Queue.Enqueue(buffer[i]);
                }
            }
        }

        public void Dispose() {
            Subscriber.ReceiveReady -= ReceiveReady;
            Poller.Remove(Subscriber);
            Poller.Dispose();
            Subscriber.Dispose();
            NetMQConfig.Cleanup(block: false);
        }
    }
}
