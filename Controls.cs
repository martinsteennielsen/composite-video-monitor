using System.Collections;

namespace CompositeVideoMonitor {
    public class Controls {
        public int FrameCount;
        public string Info => string.Format($"{TvNorm.Bandwidth/1e6}Mhz ({TvNorm.Horizontal}/{TvNorm.Vertical:N0}) - Frame {FrameCount.ToString()}");
        public TvNorm TvNorm = TvNorm.pPal;
        public double TubeViewX = 0, TubeViewY = 0, TubeZoom = 1, Focus = 1.01, ZoomT = 1, Brightness = 1;
    }
}