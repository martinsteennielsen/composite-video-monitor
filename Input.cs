using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;

namespace CompositeVideoMonitor {

    public class Input : IDisposable {
        readonly ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        readonly SubscriberSocket Subscriber;
        readonly NetMQPoller Poller;
        readonly Controls Controls;
        readonly ISignal Noise = new NoiseSignal();
        readonly FilterLowPass50Hz VSync = new FilterLowPass50Hz();
        readonly FilterBandPass15625Hz HSync = new FilterBandPass15625Hz();

        public (double, double, double) Get(double time) {
            if (Queue.IsEmpty) { return (Noise.Get(time),0,0); }
            byte signalValue;
            while (!Queue.TryDequeue(out signalValue)) ;
            double value = signalValue / 255.0;
            return (value, VSync.Get(value), HSync.Get(value));
        }

        public Input(Controls controls, string address) {
            Poller = new NetMQPoller();
            Controls = controls;

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
