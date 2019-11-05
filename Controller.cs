using System.Threading;
using System.Threading.Tasks;
using OpenTK.Input;

namespace CompositeVideoMonitor {

    public class Controller {
        readonly Input CompositeInput;
        readonly TvMonitor TvMonitor;
        readonly Renderer Renderer;
        readonly Controls Controls;

        double? ZoomTStop;
        bool FollowCursor = false;
        int CurrentSingleStep = 0;
        bool CursorOn = false;

        public Controller(TvNorm tvNorm, Input compositeSignal) {
            CompositeInput = compositeSignal;
            Controls = new Controls() { TvNorm = tvNorm };
            var tube = new Tube(Controls);
            TvMonitor = new TvMonitor(Controls, tube, CompositeInput);
            Renderer = new Renderer(Controls, TvMonitor.GetPicture, ShowCursor, 800, 600, "PAL");
            Renderer.KeyDown +=  (_, e) => ProcessKey(e);
        }

        public void Run(CancellationTokenSource canceller) {
            Task.Run(() => Run(canceller.Token));
            Renderer.Run();
            canceller.Cancel();
        }

        async Task Run(CancellationToken canceller) {
            var realTime = new RealTimeKeeper( Controls);
            var artificialTime = new ArtificialTimeKeeper(Controls);

            async Task<(double elapsedTime, double skippedTime)> relax() => 
                Controls.ZoomT == 1 
                    ? await realTime.GetElapsedTimeAsync() 
                    : await artificialTime.GetElapsedTimeAsync(SingleStep);

            double startTime = 0;
            while (!canceller.IsCancellationRequested) {
                var (elapsedTime, _) = await relax();
                startTime = TvMonitor.ElapseTime(startTime, startTime + elapsedTime);
                Controls.FrameCount = (int)(startTime / Controls.TvNorm.FrameTime);
            }
        }

        void ProcessKey(KeyboardKeyEventArgs e) {
            double ds = 0.01;
            if (e.Key == Key.Escape) {
                Renderer.Exit();
            } else if (e.Key == Key.B && e.Shift) {
                Controls.Brightness *= 1.05;
            } else if (e.Key == Key.B && !e.Shift) {
                Controls.Brightness /= 1.05;
            } else if (e.Key == Key.X && e.Shift) {
                Controls.TubeViewX -= ds;
            } else if (e.Key == Key.X && !e.Shift) {
                Controls.TubeViewX += ds;
            } else if (e.Key == Key.Y && e.Shift) {
                Controls.TubeViewY -= ds;
            } else if (e.Key == Key.Y && !e.Shift) {
                Controls.TubeViewY += ds;
            } else if (e.Key == Key.Z && e.Shift) {
                Controls.TubeZoom *= 1.05;
            } else if (e.Key == Key.Z && !e.Shift) {
                Controls.TubeZoom /= 1.05;
            } else if (e.Key == Key.F && e.Shift) {
                Controls.Focus *= 1.05;
            } else if (e.Key == Key.F && !e.Shift) {
                Controls.Focus /= 1.05;
            } else if (e.Key == Key.T && e.Shift) {
                Controls.ZoomT *= (Controls.ZoomT == 1d ? 1 : 10d);
            } else if (e.Key == Key.T && !e.Shift) {
                Controls.ZoomT /= 10d;
            } else if (e.Key == Key.S) {
                (Controls.ZoomT, ZoomTStop) = ZoomTStop == null ? (0d, (double?)Controls.ZoomT) : (ZoomTStop.Value, null);
            } else if (e.Key == Key.C) {
                CursorOn = true;
                FollowCursor = !FollowCursor;
            } else if (e.Key == Key.Space) {
                CursorOn = true;
                CurrentSingleStep = 1;
            } else if (e.Key == Key.End) {
                CursorOn = false;
                FollowCursor = false;
                Controls.TubeZoom = 1;
                Controls.TubeViewX = 0;
                Controls.TubeViewY = 0;
                Controls.ZoomT = 1;
                Controls.Brightness = 1;
                Controls.Focus = 1.01;
            }
        }

        int SingleStep() {
            int step = CurrentSingleStep;
            CurrentSingleStep = 0;
            return step;
        }

        bool ShowCursor(double cursorX, double cursorY) {
            if (FollowCursor) {
                (Controls.TubeViewX, Controls.TubeViewY) = (-cursorX, -cursorY);
            }
            return CursorOn;
        }
    }
}