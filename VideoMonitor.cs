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
                int dps=0;

                var dots = new List<Tube.PhosphorDot>();
                while (SimulatedTime < endTime) {
                    dots.Add( new Tube.PhosphorDot { VVolt = VGain * VOsc.Get(SimulatedTime), HVolt = HGain * HOsc.Get(SimulatedTime),
                                                     Brightness = CompositeSignal.Get(SimulatedTime), Time = SimulatedTime } );
                    SimulatedTime+=DotTime;
                    dps++;
                }
                Tube.UpdateDots(dots, startTime, SimulatedTime);
                DPS=dps;
                SPF = FrameTime / elapsedTime;
            }
        }
    }

    public class Tube {
        readonly object GateKeeper = new object();
        readonly double TubeWidth;
        readonly double TubeHeight;

        public double HPos(PhosphorDot dot) => dot.HVolt*TubeWidth/FullDeflectionVoltage;
        public double VPos(PhosphorDot dot) => dot.VVolt*TubeHeight/FullDeflectionVoltage;

        readonly double PhosphorGlowTime;
        readonly double FullDeflectionVoltage;
        List<PhosphorDots> Dots = new List<PhosphorDots>();

        internal void UpdateDots(List<PhosphorDot> dots, double startTime, double endTime) {
            var newDots = RemoveDots(GetDots(),endTime);
            newDots.Add(new PhosphorDots { Dots = dots, StartTime = startTime, EndTime = endTime } );
            lock(GateKeeper) {
                Dots = newDots;
            }
        }

        List<PhosphorDots> RemoveDots(List<PhosphorDots> dots, double time) {
            var glowStartTime = time - PhosphorGlowTime;
            var glowingDots = dots.Where(x => x.StartTime > glowStartTime).ToList();
            
            var newDots = new List<PhosphorDots>();
            foreach (var dimmingDots in glowingDots.Where(x=> x.EndTime < glowStartTime)) {
                newDots.Add(new PhosphorDots { Dots = dimmingDots.Dots.Where(x=>x.Time > glowStartTime).ToList(), StartTime = glowStartTime, EndTime = dimmingDots.EndTime });
            }
            newDots.AddRange(glowingDots);
            return newDots;
        }

        public List<PhosphorDots> GetDots() {
            lock (GateKeeper) {
                return Dots.ToList();
            }
        } 

        public Tube(double height, double width, double deflectionVoltage, double phosphorGlowTime) {
            FullDeflectionVoltage = deflectionVoltage;
            PhosphorGlowTime = phosphorGlowTime;
            TubeHeight = height;
            TubeWidth = width;
        }

        public class PhosphorDots {
            public double StartTime;
            public double EndTime;
            public List<PhosphorDot> Dots;
        }

        public struct PhosphorDot {
            public double VVolt;
            public double HVolt;
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