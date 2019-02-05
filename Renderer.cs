using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly VideoMonitor CRT;
        readonly Logger Logger;
        readonly double ScaleX, ScaleY;

        public Renderer(VideoMonitor monitor, Logger logger, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            CRT = monitor;
            Logger = logger;
            ScaleX = 2.0 / CRT.TubeWidth;
            ScaleY = 2.0 / CRT.TubeHeight;
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape)) {
                Exit();
            }
            base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            GL.ClearColor(Color.FromArgb(255, 5, 5, 5));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dots = CRT.GetDots();
            foreach (var dot in dots.SelectMany(x => x.Dots)) {
                var brightness = (int)(255 * dot.Brightness);
                GL.Color3(Color.FromArgb(255, brightness, brightness, brightness));
                var vPos = CRT.VPos(dot) * ScaleY;
                var hPos = CRT.HPos(dot) * ScaleX;
                GL.Vertex2(hPos, vPos);
                GL.Vertex2(0.01 + hPos, vPos);
                GL.Vertex2(0.01 + hPos, 0.01 + vPos);
                GL.Vertex2(hPos, 0.01 + vPos);
            }
            GL.End();
            SwapBuffers();

            Logger.DotCount = dots.Sum(x => x.Dots.Count);
            Logger.FramesPrSecond = 1.0 / e.Time;
        }
    }
}