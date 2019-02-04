using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace CompositeVideoMonitor
{

    class Program {
        static void Main(string[] args) {
            var monitor = new PalMonitor();
            using (Renderer renderer = new Renderer(monitor.Tube, 800, 600, "PAL")) {
                var canceller = new CancellationTokenSource();
                var stats = new Statistics(renderer, monitor);
                Task.Run(() => { stats.Run(canceller.Token); } );
                Task.Run(() => { monitor.Run(canceller.Token); } );
                renderer.Run(25);
                canceller.Cancel();
            }
        }
    }
    public class Statistics {
        readonly VideoMonitor Monitor;
        readonly Renderer Renderer;
        public Statistics(Renderer renderer, VideoMonitor monitor) {
            Renderer = renderer;    
            Monitor = monitor;
        }

        public void Run(CancellationToken canceller) {
            while (!canceller.IsCancellationRequested) {
                Console.WriteLine($"FPS:{Renderer.FPS,5:F2} SPF:{Monitor.SPF,7:F2} DPS:{Monitor.DPS,6} Dots:{Renderer.DotCount,7}");
                Task.Delay(100).Wait();
            }
        }
    }
    public class Renderer : GameWindow {
        readonly Tube CRT;
        public double FPS = 0;
        internal int DotCount;

        public Renderer(Tube tube, int width, int height, string title) : base(width, height, GraphicsMode.Default, title ) {
            CRT = tube;
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape)) {
                Exit();
            }
            base.OnUpdateFrame(e);
        }
        protected override void OnRenderFrame(FrameEventArgs e) {
            FPS = 1.0 / e.Time;
            GL.ClearColor(Color.FromArgb(255,5,5,5));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dots = CRT.GetDots();
            DotCount = dots.Sum(x=>x.Dots.Count);
            foreach(var dot in dots.SelectMany(x=>x.Dots)) {
                var brightness = (int)(255*dot.Brightness);
                GL.Color3(Color.FromArgb(255, brightness, brightness,brightness));
                var vPos=CRT.VPos(dot)*20;
                var hPos=CRT.HPos(dot)*20;
                GL.Vertex2(hPos,vPos);
                GL.Vertex2(0.01+hPos,vPos);
                GL.Vertex2(0.01+hPos,0.01+vPos);
                GL.Vertex2(hPos,0.01+vPos);
            }
            GL.End();
            SwapBuffers();
        }
    }
 }