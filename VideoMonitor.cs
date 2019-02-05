using System;
using System.Collections.Generic;
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
        public double StartTime;
        public double EndTime;
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
            var lastTime = DateTime.Now;
            while (!canceller.IsCancellationRequested) {
                var currentTime = DateTime.Now;
                var elapsedTime = (currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;

                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;

                // Too slow or too fast
                if (endTime - TimePrFrame > simulatedTime) {
                    simulatedTime = endTime - TimePrFrame;
                    logger.SkippedFrames++;
                } else if (elapsedTime < 4*TimePrDot) {
                    Thread.Yield();
                    continue;
                }

                var (endOfSimulationTime, dots) = SimulateDots(signal, time: simulatedTime, endTime: endTime);
                simulatedTime = endOfSimulationTime;
                var newDots = RemoveDots(GetDots(), simulatedTime);
                newDots.Add(new PhosphorDots { Dots = dots, StartTime = startTime, EndTime = simulatedTime });
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
            var glowStartTime = time - PhosphorGlowTime;
            var glowingDots = dots.Where(x => x.StartTime > glowStartTime).ToList();

            var newDots = new List<PhosphorDots>();
            foreach (var dimmingDots in glowingDots.Where(x => x.EndTime < glowStartTime)) {
                newDots.Add(new PhosphorDots { Dots = dimmingDots.Dots.Where(x => x.Time > glowStartTime).ToList(), StartTime = glowStartTime, EndTime = dimmingDots.EndTime });
            }
            newDots.AddRange(glowingDots);
            return newDots;
        }
    }

    public class PalMonitor : VideoMonitor {
        public PalMonitor() : base(hFreq: 15625, vFreq: 25, bandwidthFreq: 5e6, freqScaler: 1.0) {
        }
    }
}