using System;
using System.Collections.Generic;
using System.Text;


namespace CompositeVideoMonitor {
    /* Digital filter designed by mkfilter/mkshape/gencode   A.J. Fisher
       Command line: /www/usr/fisher/helpers/mkfilter -Bu -Lp -o 1 -a 1.0000000000e-05 0.0000000000e+00 -l */
    public class Filter50Hz {
        const int NZEROS = 1;
        const int NPOLES = 1;
        const double GAIN = 3.183198861e+04;
        double[] xv = new double[NZEROS];
        double[] yv = new double[NPOLES + 1];

        public double Get(double input) {
            xv[0] = xv[1];
            xv[1] = input / GAIN;
            yv[0] = yv[1];
            yv[1] = (xv[0] + xv[1])
                         + (0.9999371701 * yv[0]);
            return yv[1];
        }
    }
}




