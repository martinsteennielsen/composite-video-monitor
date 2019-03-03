using System;

namespace CompositeVideoMonitor {

    public interface ISignal {
        double Get(double time);
    }

    public interface IPeriodic : ISignal {
        double Frequency { get; set; }
        double Phase { get; set; }
    }

    public class NoiseSignal : ISignal {
        Random Randomizer = new Random();
        public double Get(double time) => Randomizer.NextDouble();
    }

    public class SawtoothSignal : IPeriodic {
        readonly double Pi = Math.PI;
        public double Frequency { get; set; }
        public double Phase { get; set; }
        public double Get(double time) => 2.0 / Pi * (Frequency * Pi * ((time + Phase/Frequency/Math.PI) % (1.0 / Frequency)) - (Pi / 2.0));
    }
}