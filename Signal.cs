using System;

namespace CompositeVideoMonitor {

    public interface ISignal {
        double Get(double time);
    }

    public class NoiseSignal : ISignal {
        Random Randomizer = new Random();
        public double Get(double time) => Randomizer.NextDouble();
    }

    public interface IPeriodic : ISignal {
        double Frequency { get; set; }
        double Phase { set; }
    }

    public class SawtoothSignal : IPeriodic {
        double angularShift, phase;

        public double Frequency { get; set; }


        public double Phase {
            set { phase = value; angularShift = value / Frequency / Math.PI; }
        }
        
        public SawtoothSignal(double freq) { 
            Frequency = freq;
        }

        public double Get(double time) => -1 + 2 * Frequency * ((time + angularShift) % (1.0/Frequency));
    }
}