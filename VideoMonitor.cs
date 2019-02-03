using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor {

    class PalMonitor : VideoMonitor {
        public PalMonitor() : base(hSyncFreq: 15625, vSyncFreq: 25, bandwidthFreq: 5e6) {
        }
    }
        
    class VideoMonitor {
        readonly NoiseSignal CompositeSignal;
        readonly SawtoothSignal VSync, HSync;
        readonly CRT Tube;
        readonly double VSyncFreq;
        readonly double HSyncFreq;
        readonly double NoOfLines;
        readonly double SampleTime;
        readonly double VGain;
        readonly double HGain;
        readonly double FrameTime;
        int loopCount=0;
        double SimulatedTime = 0;

        public VideoMonitor(double hSyncFreq, double vSyncFreq,double bandwidthFreq) {
            Tube = new CRT(height:0.03, width:0.04, deflectionVoltage: 400);
            CompositeSignal = new NoiseSignal();
            VSync = new SawtoothSignal(vSyncFreq, 0);
            HSync = new SawtoothSignal(hSyncFreq, 0);
            VSyncFreq = vSyncFreq;
            HSyncFreq = hSyncFreq;
            NoOfLines = hSyncFreq/vSyncFreq;
            SampleTime = 1 / bandwidthFreq;
            VGain = 300;
            HGain = 400;
            FrameTime = 1/vSyncFreq;

        }

        public void Run(CancellationToken cancel) {
            var lastTime = DateTime.Now;
            while (!cancel.IsCancellationRequested) {
                var elapsedTime = (DateTime.Now - lastTime).TotalSeconds;
                lastTime = DateTime.Now;

                double endTime = SimulatedTime + elapsedTime;

                if (endTime - FrameTime > SimulatedTime) {
                    SimulatedTime=endTime-FrameTime;
                } 
                while (SimulatedTime<endTime) {
                    Tube.AddDot(SimulatedTime, VGain * VSync.Get(SimulatedTime), HGain * HSync.Get(SimulatedTime), CompositeSignal.Get(SimulatedTime));
                    SimulatedTime+=SampleTime;
                }
                Tube.RemoveWeakDots(SimulatedTime);
            }
        }
    }

    class CRT {
        readonly double Width;
        readonly double Height;
        readonly double PhosphorPersistenseTime = 0.001;
        readonly double PhosphorBleedDistance = 0.001;
        public List<PhosphorDot> Dots = new List<PhosphorDot>();
        public double HPos(double hVoltage) => hVoltage*FullDeflectionVoltage;
        public double VPos(double vVoltage) => vVoltage*FullDeflectionVoltage;
        double FullDeflectionVoltage;

        public void RemoveWeakDots(double time) {
            Dots = Dots.Where(x => x.Time + PhosphorPersistenseTime > time).ToList();
        }

        internal void AddDot(double time, double vSyncVoltage, double hSyncVoltage, double brightness) {
            var excitation = new PhosphorDot{ 
                VPos = vSyncVoltage*FullDeflectionVoltage, 
                HPos = hSyncVoltage*FullDeflectionVoltage, 
                Time = time, Brightness = brightness};
            Dots.Add(excitation);
        }

        public CRT(double height, double width, double deflectionVoltage) {
            FullDeflectionVoltage = deflectionVoltage;
        }

        public class  PhosphorDot {
            public bool IsWeak;
            public double VPos;
            public double HPos;
            public double Time;
            public double Brightness;
        }
    }

    class SawtoothSignal {
        double Frequency;
        double Pi = Math.PI;

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