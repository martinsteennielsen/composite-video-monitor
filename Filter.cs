using System;
using System.Collections.Generic;
using System.Text;


namespace CompositeVideoMonitor {
    
    /* Digital filter designed by mkfilter/mkshape/gencode   A.J. Fisher*/

    public class FilterLowPass50Hz {
        double[] xv = new double[1];
        double[] yv = new double[2];

        public double Get(double input) {
            xv[0] = xv[1];
            xv[1] = input / 3.183198861e+04;
            yv[0] = yv[1];
            yv[1] = (xv[0] + xv[1])
                         + (0.9999371701 * yv[0]);
            return yv[1];
        }
    }

    public class FilterBandPass15625Hz {
        double[] xv = new double[4];
        double[] yv = new double[5];

        public double Get(double input) {
            xv[0] = xv[1]; xv[1] = xv[2]; xv[2] = xv[3]; xv[3] = xv[4];
            xv[4] = input / 1.013816588e+09;
            yv[0] = yv[1]; yv[1] = yv[2]; yv[2] = yv[3]; yv[3] = yv[4];
            yv[4] = (xv[0] + xv[4]) - 2 * xv[2]
                         + (-0.9999111463 * yv[0]) + (3.9989624502 * yv[1])
                         + (-5.9981915759 * yv[2]) + (3.9991401234 * yv[3]);
            return yv[4];
        }
    }
}



