using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly Tube CRT;
        public double FPS = 0;
        public int DotCount;
        double ScaleX, ScaleY;


        public Renderer(Tube tube, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            CRT = tube;
            ScaleX = 2.0 / tube.TubeWidth;
            ScaleY = 2.0 / tube.TubeHeight;
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
            GL.ClearColor(Color.FromArgb(255, 5, 5, 5));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dots = CRT.GetDots();
            DotCount = dots.Sum(x => x.Dots.Count);
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
        }
    }
}