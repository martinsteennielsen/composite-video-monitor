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
        double Phase { get; set; }
    }

    public class SawtoothSignal : IPeriodic {
        double period, frequency, angularShift, phase;

        public double Frequency {
            get => frequency;
            set { frequency = value; period = 1d / value; }
        }

        public double Phase {
            get => phase;
            set { phase = value; angularShift = value / frequency / Math.PI; }
        }

        public double Get(double time) => -1 + 2 * frequency * ((time + angularShift) % period);
    }
}