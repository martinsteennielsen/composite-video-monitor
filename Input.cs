using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;

namespace CompositeVideoMonitor {

    public class Input : IDisposable {
        readonly ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        readonly SubscriberSocket Subscriber;
        readonly NetMQPoller Poller;
        readonly double MaxFrames;

        double LastSampleTime = 0;
        double LastSampleValue = 0;
        public double LastSampleRate = 5e6;

        public bool TryGet(double time, out double value, out double sampleRate) {
            value=LastSampleValue; sampleRate = LastSampleRate;
            if (Queue.IsEmpty) { return false; }
            if (time <= LastSampleTime) { return true; }
            byte signalValue;
            while (!Queue.TryDequeue(out signalValue)) ;
            LastSampleTime = time;
            value = LastSampleValue = signalValue / 255.0;
            sampleRate = LastSampleRate;
            return true;
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
                LastSampleRate = msg.Pop().ConvertToInt64(); 
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
