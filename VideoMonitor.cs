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
        readonly Timing Timing;
        readonly SawtoothSignal VOsc;
        readonly SawtoothSignal HOsc;
        readonly object GateKeeper = new object();

        public readonly double TubeWidth = 0.4;
        public readonly double TubeHeight = 0.3;
        public readonly double PhosphorGlowTime;

        readonly double VGain = 150;
        readonly double HGain = 200;
        readonly double FullDeflectionVoltage = 200;

        public double HPos(double volt) => 0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;
        public double VPos(double volt) => 0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        List<PhosphorDots> Dots = new List<PhosphorDots>();

        public double SimulatedTime;

        public VideoMonitor(Timing timing) {
            Timing = timing;
            VOsc = new SawtoothSignal(timing.VFreq, 0);
            HOsc = new SawtoothSignal(timing.HFreq, 0);
            FullDeflectionVoltage = 200;
            PhosphorGlowTime = 1.0 / (Timing.VFreq);
            SimulatedTime = 0;
        }

        public void Run(CancellationToken canceller, ISignal signal, Logger logger) {
            double simulatedTime = 0, lastTime = 0;

            var timer = new Stopwatch();
            timer.Start();

            while (!canceller.IsCancellationRequested) {

                double minTime = 2 * Timing.DotTime;
                while (timer.Elapsed.TotalSeconds - lastTime < minTime) {
                    if (minTime > 0.001) {
                        Task.Delay((int)(minTime * 1000)).Wait();
                    } else {
                        Thread.Yield();
                    }
                }

                var tmpTime = timer.Elapsed.TotalSeconds;
                var elapsedTime = (tmpTime - lastTime);
                lastTime = tmpTime;

                if (elapsedTime > 2.0 * Timing.FrameTime) {
                    var skipTime = (elapsedTime - Timing.FrameTime);
                    simulatedTime += skipTime;
                    logger.SkippedFrames+= (int)(skipTime/ Timing.FrameTime);
                    elapsedTime = Timing.FrameTime;
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
                SimulatedTime = simulatedTime;
                logger.SimulationsPrFrame = Timing.FrameTime / elapsedTime;
                logger.DotsPrSimulation = dots.Count;
            }
        }

        private (double, List<PhosphorDot>) SimulateDots(ISignal signal, double time, double endTime) {
            var dots = new List<PhosphorDot>();
            while (time < endTime) {
                dots.Add(new PhosphorDot {
                    VVolt = VOsc.Get(time),
                    HVolt = HOsc.Get(time),
                    Brightness = signal.Get(time),
                    Time = time
                });
                time += Timing.DotTime;
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
            var someDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime <= dimmestDotTime);

            var newDots = new List<PhosphorDots>();
            foreach (var dimmingDots in someDotsGlowing) {
                newDots.Add(new PhosphorDots { Dots = dimmingDots.Dots.Where(x => x.Time > dimmestDotTime).ToList(), OldestDotTime = dimmestDotTime, NewestDotTime = dimmingDots.NewestDotTime });
            }
            var allDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime > dimmestDotTime);
            newDots.AddRange(allDotsGlowing);
            return newDots;
        }
    }
}