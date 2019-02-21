using System;

namespace CompositeVideoMonitor {
    public interface ISignal {
        double Get(double time);
    }

    public class NoiseSignal : ISignal {
        Random Randomizer = new Random();
        public double Get(double time) => Randomizer.NextDouble();
    }

    public class SawtoothSignal : ISignal {
        readonly double Frequency;
        readonly double Pi = Math.PI;

        public SawtoothSignal(double frequency, double phase) {
            Frequency = frequency;
        }
        public double Get(double time) => 2.0 / Pi * (Frequency * Pi * (time % (1.0 / Frequency)) - (Pi / 2.0));
    }
    public class SineSignal : ISignal {
        readonly double Frequency;
        readonly double Pi = Math.PI;

        public SineSignal(double frequency, double phase) {
            Frequency = frequency;
        }
        public double Get(double time) => 0.5*(1+Math.Sin(Frequency * 2 * Pi * (time % Frequency)));
    }
}