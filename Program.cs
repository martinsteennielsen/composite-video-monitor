using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
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
        readonly VideoMonitor Monitor;
        readonly Renderer Renderer;
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
        readonly VideoMonitor Model;
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
        protected override void OnRenderFrame(FrameEventArgs e) {
            GL.ClearColor(Color.FromArgb(255,5,5,5));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dots = Model.Tube.Dots.ToArray();
            foreach(var dot in dots) {
                var brightness = (int)(255*dot.Brightness);
                GL.Color3(Color.FromArgb(255, brightness, brightness,brightness));
                var vPos=dot.VPos*20;
                var hPos=dot.HPos*20;
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