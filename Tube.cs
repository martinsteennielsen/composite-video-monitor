using System.Linq;
using System.Collections.Generic;

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

    public class Tube {
        readonly object GateKeeper = new object();
        public readonly double TubeWidth;
        public readonly double TubeHeight;
        public double HPos(PhosphorDot dot) =>  0.5*dot.HVolt*TubeWidth/FullDeflectionVoltage;
        public double VPos(PhosphorDot dot) => -0.5*dot.VVolt*TubeHeight/FullDeflectionVoltage;

        readonly double PhosphorGlowTime;
        readonly double FullDeflectionVoltage;
        List<PhosphorDots> Dots = new List<PhosphorDots>();

        public Tube(double height, double width, double deflectionVoltage, double phosphorGlowTime) {
            FullDeflectionVoltage = deflectionVoltage;
            PhosphorGlowTime = phosphorGlowTime;
            TubeHeight = height;
            TubeWidth = width;
        }

        public List<PhosphorDots> GetDots() {
            lock (GateKeeper) {
                return Dots.ToList();
            }
        } 

        public void UpdateDots(List<PhosphorDot> dots, double startTime, double endTime) {
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
    }
}