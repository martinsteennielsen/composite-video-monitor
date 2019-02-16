using System;
using System.Threading;
using OpenTK.Input;

namespace CompositeVideoMonitor {

    public class Controls {
        public double TubeViewX = 0, TubeViewY = 0, TubeViewSize = VideoMonitor.TubeWidth, Focus = 1, ZoomT = 0.001;

        public bool ProcessKey(KeyboardKeyEventArgs e) {
            double ds = TubeViewSize * 0.05;
            if (e.Key == Key.Escape) {
                return false;
            } else if (e.Key == Key.X && e.Shift) {
                TubeViewX -= ds;
            } else if (e.Key == Key.X && !e.Shift) {
                TubeViewX += ds;
            } else if (e.Key == Key.Y && e.Shift) {
                TubeViewY -= ds;
            } else if (e.Key == Key.Y && !e.Shift) {
                TubeViewY += ds;
            } else if (e.Key == Key.Z && e.Shift) {
                TubeViewSize *= 1.05;
            } else if (e.Key == Key.Z && !e.Shift) {
                TubeViewSize /= 1.05;
            } else if (e.Key == Key.F && e.Shift) {
                Focus *= 1.05;
            } else if (e.Key == Key.F && !e.Shift) {
                Focus /= 1.05;
            } else if (e.Key == Key.T && e.Shift) {
                ZoomT *= (ZoomT == 1d ? 1 : 10d);
            } else if (e.Key == Key.T && !e.Shift) {
                ZoomT /= 10d;
            }
            return true;
        }
    }
}