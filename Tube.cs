using System.Collections.Generic;
using System.Linq;

namespace CompositeVideoMonitor {

    public struct PhosphorDot {
        public double VVolt;
        public double HVolt;
        public double Time;
        public double Brightness;
    }

    public class Tube {

        struct FrameSection {
            public double OldestDotTime;
            public double NewestDotTime;
            public List<PhosphorDot> Dots;
        }

        List<FrameSection> Frame = new List<FrameSection>();

        public static readonly double TubeWidth = 0.4;
        public static readonly double TubeHeight = 0.3;

        readonly TimingConstants Timing;
        readonly double PhosphorGlowTime;
        readonly object GateKeeper = new object();
        readonly double VGain = 30;
        readonly double HGain = 40;
        readonly double FullDeflectionVoltage = 40;


        public Tube(TimingConstants timing) {
            Timing = timing;
            PhosphorGlowTime = timing.LineTime * 0.5 + timing.FrameTime + 2d * timing.DotTime;
        }

        public double HPos(double volt) =>
            0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;

        public double VPos(double volt) =>
            0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        public List<PhosphorDot> GetPicture() {
            return CurrentSections().SelectMany(x => x.Dots).ToList();
        }

        List<FrameSection> CurrentSections() {
            lock (GateKeeper) {
                return Frame;
            }
        }

        public double SpendTime(double startTime, double endTime, ISignal compositeSignal, ISignal hOsc, ISignal vOsc) {
            var (sections, simulatedEndTime) = CalculateSections(compositeSignal, hOsc, vOsc, time: startTime, endTime: endTime);
            var newFrame = RemoveDots(CurrentSections(), simulatedEndTime);
            newFrame.AddRange(sections);
            lock (GateKeeper) {
                Frame = newFrame;
            }
            return simulatedEndTime;
        }

        (List<FrameSection>, double) CalculateSections(ISignal signal, ISignal hOsc, ISignal vOsc, double time, double endTime) {
            var sections = new List<FrameSection>();
            while (time < endTime) {
                double startTime = time;
                double lineTime = 0;
                var dots = new List<PhosphorDot>();
                while (time < endTime && lineTime < Timing.LineTime) {
                    dots.Add(new PhosphorDot {
                        VVolt = vOsc.Get(time),
                        HVolt = hOsc.Get(time),
                        Brightness = signal.Get(time),
                        Time = time
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