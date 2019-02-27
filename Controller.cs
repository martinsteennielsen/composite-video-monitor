using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Input;

namespace CompositeVideoMonitor {

    public class Controller {
        readonly TimeKeeper TimeKeeper;
        readonly Input CompositeInput;
        readonly VideoMonitor Monitor;
        readonly Renderer Renderer;
        readonly Logger Logger;
        readonly Controls Controls;

        double? ZoomTStop;
        bool FollowCursor = false;
        int CurrentSingleStep = 0;
        bool CursorOn = false;

        public Controller(TimingConstants timing, Input compositeSignal) {
            Controls = new Controls();
            TimeKeeper = new TimeKeeper(timing, Controls);
            CompositeInput = compositeSignal;
            Monitor = new VideoMonitor(timing, CompositeInput);
            Logger = new Logger();
            Renderer = new Renderer(Controls, ProcessPosition, Monitor.Tube, timing, Logger, 600, 600, "PAL");
            Renderer.KeyDown +=  (_, e) => ProcessKey(e);
        }

        public void Run(CancellationTokenSource canceller) {
            Task.Run(() => Logger.Run(canceller.Token));
            Task.Run(() => Run(canceller.Token));
            Renderer.Run();
            canceller.Cancel();
        }

        async Task Run(CancellationToken canceller) {
            double simulatedTime = 0;
            while (!canceller.IsCancellationRequested) {
                var (elapsedTime, skippedTime) = await TimeKeeper.GetElapsedTimeAsync(SingleStep);
                CompositeInput.Skip(skippedTime);

                double startTime = simulatedTime;
                double endTime = simulatedTime + elapsedTime;
                simulatedTime = Monitor.Calculate(startTime, endTime);
            }
        }

        bool ProcessKey(KeyboardKeyEventArgs e) {
            double ds = Controls.TubeViewSize * 0.05;
            if (e.Key == Key.Escape) {
                return false;
            } else if (e.Key == Key.X && e.Shift) {
                Controls.TubeViewX -= ds;
            } else if (e.Key == Key.Escape) {
                Renderer.Exit();
            } else if (e.Key == Key.X && !e.Shift) {
                Controls.TubeViewX += ds;
            } else if (e.Key == Key.Y && e.Shift) {
                Controls.TubeViewY -= ds;
            } else if (e.Key == Key.Y && !e.Shift) {
                Controls.TubeViewY += ds;
            } else if (e.Key == Key.Z && e.Shift) {
                Controls.TubeViewSize *= 1.05;
            } else if (e.Key == Key.Z && !e.Shift) {
                Controls.TubeViewSize /= 1.05;
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
                Controls.TubeViewSize = Tube.TubeWidth;
                Controls.TubeViewX = 0;
                Controls.TubeViewY = 0;
                Controls.ZoomT = 1;
            }
            return true;
        }

        int SingleStep() {
            int step = CurrentSingleStep;
            CurrentSingleStep = 0;
            return step;
        }

        bool ProcessPosition(double cursorX, double cursorY) {
            if (FollowCursor) {
                (Controls.TubeViewX, Controls.TubeViewY) = (-cursorX, -cursorY);
            }
            return CursorOn;
        }
    }
}