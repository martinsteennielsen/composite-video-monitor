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

    public class FrameSection {
        public double OldestDotTime;
        public double NewestDotTime;
        public List<PhosphorDot> Dots;
    }

    public class VideoMonitor {
        public readonly double TubeWidth = 0.4;
        public readonly double TubeHeight = 0.3;
        public readonly double PhosphorGlowTime;

        readonly object GateKeeper = new object();
        readonly Timing Timing;
        readonly ISignal VOsc, HOsc;
        readonly double VGain = 30;
        readonly double HGain = 40;
        readonly double FullDeflectionVoltage = 40;

        List<FrameSection> Frame = new List<FrameSection>();
        private double SimulatedTime;

        public VideoMonitor(Timing timing) {
            Timing = timing;
            VOsc = new SawtoothSignal(timing.VFreq, 0);
            HOsc = new SawtoothSignal(timing.HFreq, 0);
            PhosphorGlowTime = 1.0 / (Timing.VFreq);
            SimulatedTime = 0;
        }

        public double HPos(double volt) => 
            0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;

        public double VPos(double volt) => 
            0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        public (double, List<FrameSection>) GetFrame() => 
            (SimulatedTime, CurrentFrame());

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

                var section  = CalculateSection(signal, time: simulatedTime, endTime: endTime);
                simulatedTime = SimulatedTime = section.NewestDotTime + Timing.DotTime;
                var newFrame = RemoveDots(CurrentFrame(), SimulatedTime);
                newFrame.Add(section);
                lock (GateKeeper) {
                    Frame = newFrame;
                }
                logger.SimulationsPrFrame = Timing.FrameTime / elapsedTime;
                logger.DotsPrSimulation = section.Dots.Count;
            }
        }

        FrameSection CalculateSection(ISignal signal, double time, double endTime) {
            var startTime = time;
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
            return new FrameSection { Dots = dots, OldestDotTime = startTime, NewestDotTime = time - Timing.DotTime };
        }

        List<FrameSection> CurrentFrame() {
            lock (GateKeeper) {
                return Frame.ToList();
            }
        }

        List<FrameSection> RemoveDots(List<FrameSection> dots, double time) {
            var dimmestDotTime = time - PhosphorGlowTime;

            var allOrSomeDotsGlowing = dots.Where(x => x.NewestDotTime > dimmestDotTime).ToList();
            var someDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime <= dimmestDotTime);

            var newDots = new List<FrameSection>();
            foreach (var dimmingDots in someDotsGlowing) {
                newDots.Add(new FrameSection { Dots = dimmingDots.Dots.Where(x => x.Time > dimmestDotTime).ToList(), OldestDotTime = dimmestDotTime, NewestDotTime = dimmingDots.NewestDotTime });
            }
            var allDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime > dimmestDotTime);
            newDots.AddRange(allDotsGlowing);
            return newDots;
        }
    }
}