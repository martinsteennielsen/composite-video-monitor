using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace CompositeVideoMonitor {
    public interface ISignal {
        double Get(double time);
    }

    public class SawtoothSignal : ISignal {
        readonly double Frequency;
        readonly double Pi = Math.PI;

        public SawtoothSignal(double frequency, double phase) {
            Frequency = frequency;
        }
        public double Get(double time) => 2.0 / Pi * (Frequency * Pi * (time % (1.0 / Frequency)) - (Pi / 2.0));
    }
}