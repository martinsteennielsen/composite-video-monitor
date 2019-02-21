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
        public double hs;
        public double hs2;
        public double sync;
    }

    public class FrameSection {
        public double OldestDotTime;
        public double NewestDotTime;
        public List<PhosphorDot> Dots;
    }

    public class VideoMonitor {
        public static readonly double TubeWidth = 0.4;
        public static readonly double TubeHeight = 0.3;
        public readonly double PhosphorGlowTime;

        readonly object GateKeeper = new object();
        readonly Input Input;
        readonly Logger Logger;
        readonly Controls Controls;
        readonly TimingConstants Timing;
        readonly ISignal VOsc, HOsc;
        readonly TimeKeeper TimeKeeper;
        readonly double VGain = 30;
        readonly double HGain = 40;
        readonly double FullDeflectionVoltage = 40;

        List<FrameSection> Frame = new List<FrameSection>();

        ISignal HS;
        FilterLowPass1Hz DC = new FilterLowPass1Hz();
        public VideoMonitor(Controls controls, TimingConstants timing, Input input, Logger logger) {
            Logger = logger;
            Timing = timing;
            Input=input;
            Controls = controls;
            VOsc = new SawtoothSignal(timing.VFreq, 0);
            HOsc = new SawtoothSignal(timing.HFreq, 0);
            PhosphorGlowTime = timing.LineTime * 0.5 + timing.FrameTime + 2d * timing.DotTime;
            TimeKeeper = new TimeKeeper(Timing, Controls);
            HS = new SineSignal(timing.HFreq, 0);
        }

        public double HPos(double volt) =>
            0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;

        public double VPos(double volt) =>
            0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        public List<FrameSection> CurrentFrame() {
            lock (GateKeeper) {
                return Frame.ToList();
            }
        }

        public async Task Run(CancellationToken canceller) {
            double simulatedTime = 0;
            double lastZoomT = Controls.ZoomT;

            while (!canceller.IsCancellationRequested) {
                var elapsedTime = await TimeKeeper.GetElapsedTimeAsync();
                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;

                var (sections, simulatedEndTime) = CalculateSections(Input, time: simulatedTime, endTime: endTime);
                simulatedTime = simulatedEndTime;
                var newFrame = RemoveDots(CurrentFrame(), simulatedTime);
                newFrame.AddRange(sections);
                lock (GateKeeper) {
                    Frame = newFrame;
                }
                Logger.SimulationsPrFrame = Timing.FrameTime / elapsedTime;
                Logger.DotsPrSimulation = sections.Count;
            }
        }

        (List<FrameSection>, double) CalculateSections(Input signal, double time, double endTime) {
            var sections = new List<FrameSection>();
            while (time < endTime) {
                double startTime = time;
                double lineTime = 0;
                var dots = new List<PhosphorDot>();
                while (time < endTime && lineTime < Timing.LineTime) {
                    var (brightness, vsync, hsync) = signal.Get(time);
                    var hs = HS.Get(time);
                    dots.Add(new PhosphorDot {
                        VVolt = VOsc.Get(time),
                        HVolt = HOsc.Get(time),
                        Brightness = brightness, // * (80 * hsync),//4*vsync, ,
                        Time = time,
                        hs = hs,
                        hs2 = hsync * 80d,
                        sync = DC.Get(80d *hsync - 10*hs) 
                    });
                    time += Timing.DotTime;
                    lineTime += Timing.DotTime;
                }
                sections.Add(new FrameSection { Dots = dots, OldestDotTime = startTime, NewestDotTime = time - Timing.DotTime });
            }
            return (sections, time);
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