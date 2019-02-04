using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace CompositeVideoMonitor {
    
    public class SawtoothSignal {
        readonly double Frequency;
        readonly double Pi = Math.PI;

        public SawtoothSignal(double frequency, double phase) {
            Frequency = frequency;
        }
        public double Get(double time) => 2.0/Pi * (Frequency * Pi * (time % (1.0 /Frequency)) - (Pi / 2.0));
    }

    public class NoiseSignal {
        Random Randomizer = new Random();
        public double Get(double time) => Randomizer.NextDouble();
    }
}