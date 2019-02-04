using System;
using System.Collections.Generic;
using System.Threading;

namespace CompositeVideoMonitor {

    public class VideoMonitor {
        readonly NoiseSignal CompositeSignal;
        readonly SawtoothSignal VOsc, HOsc;
        public readonly Tube Tube;
        readonly double VGain;
        readonly double HGain;
        readonly double FrameTime;
        readonly double DotTime;
        double SimulatedTime = 0;
        public double SPF = 0;
        public int DPS = 0;

        public VideoMonitor(double hFreq, double vFreq,double bandwidthFreq) {
            Tube = new Tube(height:0.3, width:0.4, deflectionVoltage: 200, phosphorGlowTime: 1/vFreq);
            CompositeSignal = new NoiseSignal();
            VOsc = new SawtoothSignal(vFreq, 0);
            HOsc = new SawtoothSignal(hFreq, 0);
            DotTime = 1.0 / bandwidthFreq;
            FrameTime = 1.0 /vFreq;
            VGain = 150;
            HGain = 200;
        }

        public void Run(CancellationToken canceller) {
            var lastTime = DateTime.Now;
            while (!canceller.IsCancellationRequested) {
                var currentTime = DateTime.Now;
                var elapsedTime = (currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;

                double startTime = SimulatedTime;
                double endTime = SimulatedTime + elapsedTime;
                if (endTime - FrameTime > SimulatedTime) {
                    SimulatedTime = endTime - FrameTime;
                } 

                var dots = new List<PhosphorDot>();
                while (SimulatedTime < endTime) {
                    dots.Add( new PhosphorDot { VVolt = VGain * VOsc.Get(SimulatedTime), HVolt = HGain * HOsc.Get(SimulatedTime),
                                                     Brightness = CompositeSignal.Get(SimulatedTime), Time = SimulatedTime } );
                    SimulatedTime+=DotTime;
                }
                Tube.UpdateDots(dots, startTime, SimulatedTime);

                DPS=dots.Count;
                SPF = FrameTime / elapsedTime;
            }
        }
    }

    public class PalMonitor : VideoMonitor {
        public PalMonitor() : base(hFreq: 15625, vFreq: 25, bandwidthFreq: 5e6) {
        }
    }

    // public class SlowMonitor : VideoMonitor {
    //     public SlowMonitor() : base(hFreq: 0.5, vFreq: 0.025, bandwidthFreq: 0.5*300) {
    //     }
    // }
        
}