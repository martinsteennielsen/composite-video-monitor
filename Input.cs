using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;

namespace CompositeVideoMonitor {

    public class Input : ISignal, IDisposable {
        readonly ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        readonly SubscriberSocket Subscriber;
        readonly NetMQPoller Poller;
        readonly ISignal Noise = new NoiseSignal();
        readonly double MaxFrames;

        double LastSampleTime = 0;
        double LastSampleValue = 0;

        public double Get(double time) {
            if (time <= LastSampleTime) { return LastSampleValue; }
            if (Queue.IsEmpty) { return Noise.Get(time); }
            byte signalValue;
            while (!Queue.TryDequeue(out signalValue)) ;
            LastSampleTime = time;
            LastSampleValue = signalValue;
            return signalValue / 255.0;
        }

        public Input(string address, int maxFrames) {
            MaxFrames = maxFrames;
            Poller = new NetMQPoller();
            Subscriber = new SubscriberSocket();
            Poller.Add(Subscriber);
            Poller.RunAsync();

            Subscriber.SubscribeToAnyTopic();
            Subscriber.ReceiveReady += ReceiveReady;
            Subscriber.Connect(address);
        }

        private void ReceiveReady(object _, NetMQSocketEventArgs __) {
            NetMQMessage msg = new NetMQMessage();
            while (Subscriber.TryReceiveMultipartMessage(ref msg)) {
                var sampleRate = msg.Pop().ConvertToInt32();
                var buffer = msg.Pop().ToByteArray();
                if (Queue.Count > buffer.Length * MaxFrames) { continue; }
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
