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
        public double Phase = 0;
        public double Frequency = 1;
        readonly double Pi = Math.PI;

        public double Get(double time) => 2.0 / Pi * (Frequency * Pi * ((time + Phase/Frequency/Math.PI) % (1.0 / Frequency)) - (Pi / 2.0));
    }

    public class SquareSignal : ISignal {
        readonly double OnStartTime, OnTime, OffTime, Amplitude;

        public SquareSignal(double frequency, double onTime, double onStartTime = 0, double amplitude = 1.0) {
            OnStartTime = onStartTime;
            OnTime = onTime;
            Amplitude = amplitude;
            OffTime = (1.0 / frequency) - onTime;
        }

        public double Get(double time) => ((OnTime + OffTime + time - OnStartTime) % (OnTime + OffTime)) > OnTime ? 0 : Amplitude;
    }
}