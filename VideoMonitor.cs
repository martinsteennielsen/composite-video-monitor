using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace CompositeVideoMonitor
{

    public class PalMonitor : VideoMonitor {
        public PalMonitor() : base(hFreq: 15625, vFreq: 25, bandwidthFreq: 5e6) {
        }
    }
        
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
            Tube = new Tube(height:0.03, width:0.04, deflectionVoltage: 400, phosphorGlowTime: 0.02);
            CompositeSignal = new NoiseSignal();
            VOsc = new SawtoothSignal(vFreq, 0);
            HOsc = new SawtoothSignal(hFreq, 0);
            DotTime = 1 / bandwidthFreq;
            FrameTime = 1/vFreq;
            VGain = 300;
            HGain = 400;
        }

        public void Run(CancellationToken cancel) {
            var lastTime = DateTime.Now;
            while (!cancel.IsCancellationRequested) {
                var currentTime = DateTime.Now;
                var elapsedTime = (currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;

                double endTime = SimulatedTime + elapsedTime;
                if (endTime - FrameTime > SimulatedTime) {
                    SimulatedTime = endTime - FrameTime;
                } 
                int dps=0;
                while (SimulatedTime < endTime) {
                    Tube.AddDot(SimulatedTime, VGain * VOsc.Get(SimulatedTime), HGain * HOsc.Get(SimulatedTime), CompositeSignal.Get(SimulatedTime));
                    SimulatedTime+=DotTime;
                    dps++;
                }
                Tube.RemoveWeakDots(SimulatedTime);
                DPS=dps;
                SPF = FrameTime / elapsedTime;
            }
        }
    }

    public class Tube {
        readonly double TubeWidth;
        readonly double TubeHeight;
        readonly double PhosphorGlowTime;
        readonly double FullDeflectionVoltage;
        public List<PhosphorDot> Dots = new List<PhosphorDot>();

        public void RemoveWeakDots(double time) {
            Dots = Dots.Where(x => x.Time + PhosphorGlowTime > time).ToList();
        }

        internal void AddDot(double time, double vCoilVoltage, double hCoilVoltage, double brightness) {
            var excitation = new PhosphorDot{ 
                VPos = vCoilVoltage*TubeHeight/FullDeflectionVoltage, 
                HPos = hCoilVoltage*TubeWidth/FullDeflectionVoltage, 
                Time = time, Brightness = brightness};
            Dots.Add(excitation);
        }

        public Tube(double height, double width, double deflectionVoltage, double phosphorGlowTime) {
            FullDeflectionVoltage = deflectionVoltage;
            PhosphorGlowTime = phosphorGlowTime;
            TubeHeight = height;
            TubeWidth = width;
        }

        public struct PhosphorDot {
            public double VPos;
            public double HPos;
            public double Time;
            public double Brightness;
        }
    }

    class SawtoothSignal {
        readonly double Frequency;
        readonly double Pi = Math.PI;

        public SawtoothSignal(double frequency, double phase) {
            Frequency = frequency;
        }
        public double Get(double time) => 2.0/Pi * (Frequency * Pi * (time % (1.0 /Frequency)) - (Pi / 2.0));
    }

    class NoiseSignal {
        Random Randomizer = new Random();
        public double Get(double time) => Randomizer.NextDouble();
    }
}