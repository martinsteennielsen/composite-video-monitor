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
        public double OldestDotTime;
        public double NewestDotTime;
        public List<PhosphorDot> Dots;
    }

    public class VideoMonitor {
        readonly TimingConstants Timing;
        readonly ISignal Signal;
        readonly Logger Logger;
        readonly TimeKeeper TimeKeeper;
        readonly SawtoothSignal VOsc;
        readonly SawtoothSignal HOsc;

        readonly object GateKeeper = new object();

        public readonly double TubeWidth = 0.4;
        public readonly double TubeHeight = 0.3;
        public readonly double PhosphorGlowTime;

        readonly double VGain = 30;
        readonly double HGain = 40;
        readonly double FullDeflectionVoltage = 40;

        public double HPos(double volt) => 0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;
        public double VPos(double volt) => 0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        List<PhosphorDots> Dots = new List<PhosphorDots>();

        public double SimulatedTime;

        public VideoMonitor(TimingConstants timing, ISignal signal, Logger logger) {
            Logger = logger;
            Timing = timing;
            Signal = signal;
            TimeKeeper = new TimeKeeper(minTime: 200 * timing.DotTime, maxTime: timing.FrameTime);

            VOsc = new SawtoothSignal(timing.VFreq, 0);
            HOsc = new SawtoothSignal(timing.HFreq, 0);
            PhosphorGlowTime = 1.0 / (Timing.VFreq);
            SimulatedTime = 0;
        }

        public async Task Run(CancellationToken canceller) {
            double simulatedTime = 0;
            while (!canceller.IsCancellationRequested) {
                var elapsedTime = await TimeKeeper.GetElapsedTimeAsync();
                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;

                var (endOfSimulationTime, dots) = SimulateDots(Signal, time: simulatedTime, endTime: endTime);
                simulatedTime = endOfSimulationTime;
                var newDots = RemoveDots(GetDots(), simulatedTime);
                newDots.Add(new PhosphorDots { Dots = dots, OldestDotTime = startTime, NewestDotTime = simulatedTime });
                lock (GateKeeper) {
                    Dots = newDots;
                }
                SimulatedTime = simulatedTime;
                Logger.SimulationsPrFrame = Timing.FrameTime / elapsedTime;
                Logger.DotsPrSimulation = dots.Count;
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