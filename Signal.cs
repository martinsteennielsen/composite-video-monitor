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
        double Frequency { get; }
        double Phase { set; }
    }

    public class SawtoothSignal : IPeriodic {
        readonly double frequency;
        double angularShift, phase;

        public double Frequency {
            get => frequency;
        }

        public double Phase {
            set { phase = value; angularShift = value / frequency / Math.PI; }
        }
        
        public SawtoothSignal(double freq) { 
            frequency = freq;
        }

        public double Get(double time) => -1 + 2 * frequency * ((time + angularShift) % (1.0/frequency));
    }
}