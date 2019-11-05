using System.Collections.Generic;
using System.Linq;

namespace CompositeVideoMonitor {

    public struct PhosphorDot {
        public double VPos;
        public double HPos;
        public double Time;     
        public double Brightness;
    }

    public class Picture {
        public string  Info;
        public double  Width;
        public  double Height;          
        public double  DotWidth;
        public  double DotHeight;          
        public List<PhosphorDot> Dots = new List<PhosphorDot>();
    }

    public class Tube {

        struct FrameSection {
            public double OldestDotTime;
            public double NewestDotTime;
            public List<PhosphorDot> Dots;
        }

        List<FrameSection> Frame = new List<FrameSection>();

        readonly Controls Controls;
        readonly object GateKeeper = new object();
        readonly double VGain = 40;
        readonly double HGain = 40;
        readonly double FullDeflectionVoltage = 40;
        readonly double TubeWidth = 0.4;
        readonly double TubeHeight = 0.34;


        public Tube(Controls controls) {
            Controls = controls;
        }

        double HPos(double volt) =>
            0.5 * volt * HGain * TubeWidth / FullDeflectionVoltage;

        double VPos(double volt) =>
            0.5 * volt * VGain * TubeHeight / FullDeflectionVoltage;

        public Picture GetPicture(ISignal hOsc, ISignal vOsc, string info) {
            return new Picture {
                Info = info,
                Width = TubeWidth, Height = TubeHeight, 
                Dots =  CurrentSections().SelectMany(x => x.Dots).ToList(),
                DotWidth = HPos(hOsc.Get(Controls.TvNorm.DotTime)) - HPos(hOsc.Get(0)),
                DotHeight = VPos(vOsc.Get(Controls.TvNorm.LineTime)) - VPos(vOsc.Get(0)),
            };
        }

        List<FrameSection> CurrentSections() {
            lock (GateKeeper) {
                return Frame;
            }
        }

        public double ElapseTime(double startTime, double endTime, ISignal compositeSignal, ISignal hosc, ISignal vosc) {
            var (sections, simulatedEndTime) = CalculateSections(compositeSignal, hosc, vosc, time: startTime, endTime: endTime);
            var newFrame = RemoveDots(CurrentSections(), simulatedEndTime-Controls.TvNorm.DotTime);
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
                while (time < endTime && lineTime < Controls.TvNorm.LineTime) {
                    dots.Add(new PhosphorDot {
                        VPos = VPos(vOsc.Get(time)),
                        HPos = HPos(hOsc.Get(time)),
                        Brightness = signal.Get(time),
                        Time = time
                    });
                    time += Controls.TvNorm.DotTime;
                    lineTime += Controls.TvNorm.DotTime;
                }
                sections.Add(new FrameSection { Dots = dots, OldestDotTime = startTime, NewestDotTime = time - Controls.TvNorm.DotTime });
            }
            return (sections, time);
        }

        List<FrameSection> RemoveDots(List<FrameSection> dots, double time) {
            var dimmestDotTime = time - Controls.TvNorm.FrameTime;

            var allOrSomeDotsGlowing = dots.Where(x => x.NewestDotTime >= dimmestDotTime).ToList();
            var someDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime < dimmestDotTime);

            var newDots = new List<FrameSection>();
            foreach (var dimmingDots in someDotsGlowing) {
                newDots.Add(new FrameSection { Dots = dimmingDots.Dots.Where(x => x.Time >= dimmestDotTime).ToList(), OldestDotTime = dimmestDotTime, NewestDotTime = dimmingDots.NewestDotTime });
            }
            var allDotsGlowing = allOrSomeDotsGlowing.Where(x => x.OldestDotTime >= dimmestDotTime);
            newDots.AddRange(allDotsGlowing);
            return newDots;
        }
    }

}