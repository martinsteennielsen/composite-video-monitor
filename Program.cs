using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace CompositeVideoMonitor
{

    class Program {
        static void Main(string[] args) {
            var monitor = new PalMonitor();
            using (Renderer renderer = new Renderer(monitor, 800, 600, "PAL")) {
                var cts = new CancellationTokenSource();
                var stats = new Statistics(renderer, monitor);
                Task.Run(() => { stats.Run(cts.Token); } );
                Task.Run(() => { monitor.Run(cts.Token); } );
                renderer.Run(25);
                cts.Cancel();
            }
        }
    }
    public class Statistics {
        VideoMonitor Monitor;
        Renderer Renderer;
        public Statistics(Renderer renderer, VideoMonitor monitor) {
            Renderer = renderer;    
            Monitor = monitor;
        }

        public void Run(CancellationToken cancel) {
            while (!cancel.IsCancellationRequested) {
                Console.WriteLine($"FPS:{Renderer.FPS,5:F2} SPF:{Monitor.SPF,5:F2} DPS:{Monitor.DPS,5} Dots:{Monitor.Tube.Dots.Count,6}");
                Task.Delay(100).Wait();
            }
        }
    }
    public class Renderer : GameWindow {
        VideoMonitor Model;
        public double FPS = 0;
        public Renderer(VideoMonitor model, int width, int height, string title) : base(width, height, GraphicsMode.Default, title ) {
            Model = model;
        }
        protected override void OnUpdateFrame(FrameEventArgs e) {
            FPS = 1.0 / e.Time;
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape)) {
                Exit();
            }
            base.OnUpdateFrame(e);
        }
    }
 }