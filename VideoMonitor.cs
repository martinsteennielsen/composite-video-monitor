using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {
    public struct PhosphorDot {
        public double VVolt;
        public double HVolt;
        public double Time;
        public double Brightness;
    }

    public class PhosphorDots {
        public double OldestDotTime;
        public double NewestDotTime;
        public List<PhosphorDot> Dots;
    }

    public class VideoMonitor {
        public readonly double TubeWidth = 0.4;
        public readonly double TubeHeight = 0.3;

        readonly object GateKeeper = new object();
        readonly SawtoothSignal VOsc, HOsc;

        readonly double VGain = 150;
        readonly double HGain = 200;
        readonly double FullDeflectionVoltage = 200;
        readonly double TimePrFrame;
        readonly double TimePrDot;
        readonly double PhosphorGlowTime;

        public double HPos(PhosphorDot dot) => 0.5 * dot.HVolt * TubeWidth / FullDeflectionVoltage;
        public double VPos(PhosphorDot dot) => -0.5 * dot.VVolt * TubeHeight / FullDeflectionVoltage;

        List<PhosphorDots> Dots = new List<PhosphorDots>();

        public VideoMonitor(double hFreq, double vFreq, double bandwidthFreq, double freqScaler = 1) {
            FullDeflectionVoltage = 200;
            PhosphorGlowTime = 1.0 / (vFreq * freqScaler);
            VOsc = new SawtoothSignal(vFreq * freqScaler, 0);
            HOsc = new SawtoothSignal(hFreq * freqScaler, 0);
            TimePrDot = 1.0 / (bandwidthFreq * freqScaler);
            TimePrFrame = 1.0 / (vFreq * freqScaler);
        }

        public void Run(CancellationToken canceller, ISignal signal, Logger logger) {
            double simulatedTime = 0;
            var timer = new Stopwatch();
            timer.Start();
            double lastTime= 0;

            while (!canceller.IsCancellationRequested) {

                while(timer.Elapsed.TotalSeconds - lastTime < 100*TimePrDot) {
                        Thread.Yield();
                }

                var tmpTime = timer.Elapsed.TotalSeconds;
                var elapsedTime = (tmpTime - lastTime);
                lastTime = tmpTime;

                if (elapsedTime > 2.0 * TimePrFrame) {
                    simulatedTime += TimePrFrame;
                    logger.SkippedFrames++;
                }

                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;

                var (endOfSimulationTime, dots) = SimulateDots(signal, time: simulatedTime, endTime: endTime);
                simulatedTime = endOfSimulationTime;
                var newDots = RemoveDots(GetDots(), simulatedTime);
                newDots.Add(new PhosphorDots { Dots = dots, OldestDotTime = startTime, NewestDotTime = simulatedTime });
                lock (GateKeeper) {
                    Dots = newDots;
                }
                logger.SimulationsPrFrame = TimePrFrame / elapsedTime;
                logger.DotsPrSimulation = dots.Count;
            }
        }

        private (double, List<PhosphorDot>) SimulateDots(ISignal signal, double time, double endTime) {
            var dots = new List<PhosphorDot>();
            while (time < endTime) {
                dots.Add(new PhosphorDot {
                    VVolt = VGain * VOsc.Get(time),
                    HVolt = HGain * HOsc.Get(time),
                    Brightness = signal.Get(time),
                    Time = time
                });
                time += TimePrDot;
            }
            return (time, dots);
        }

        public List<PhosphorDots> GetDots() {
            lock (GateKeeper) {
                return Dots.ToList();
            }
        }

        private List<PhosphorDots> RemoveDots(List<PhosphorDots> dots, double time) {
            var dimmestDotTime = time - PhosphorGlowTime;

            var allOrSomeDotsGlowing = dots.Where(x => x.NewestDotTime > dimmestDotTime).ToList();
            var someDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime  <= dimmestDotTime);

            var newDots = new List<PhosphorDots>();
            foreach (var dimmingDots in someDotsGlowing) {
                newDots.Add(new PhosphorDots { Dots = dimmingDots.Dots.Where(x => x.Time > dimmestDotTime).ToList(), OldestDotTime = dimmestDotTime, NewestDotTime = dimmingDots.NewestDotTime });
            }
            var allDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime > dimmestDotTime);
            newDots.AddRange(allDotsGlowing);
            return newDots;
        }
    }

    public class PalMonitor : VideoMonitor {
        public PalMonitor() : base(hFreq: 15625, vFreq: 50, bandwidthFreq: 5e6, freqScaler: 1) {
        }
    }
}