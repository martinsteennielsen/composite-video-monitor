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
            KeyDown += (_, e) => {
                if (e.Key == Key.Escape) {
                    Exit();
                }
            };
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.DstColor);
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dotRadius = CRT.TubeDotSize / 2;
            var dots = CRT.GetDots();
            var simulationTime = CRT.SimulatedTime;
            foreach (var dot in dots.SelectMany(x => x.Dots)) {
                var dotLifeTime = 1 - (simulationTime - dot.Time) / CRT.PhosphorGlowTime;
                if (dotLifeTime < 0) continue;
                if (dotLifeTime > 1) continue;
                var brightness = dot.Brightness * dotLifeTime;
                GL.Color4(brightness, brightness, brightness, 0.3);
                var vPos = -CRT.VPos(dot);
                var hPos = CRT.HPos(dot);
                GL.Vertex2((hPos - dotRadius) * ScaleX, (vPos + dotRadius) * ScaleY);
                GL.Vertex2((hPos + dotRadius) * ScaleX, (vPos + dotRadius) * ScaleY);
                GL.Vertex2((hPos + dotRadius) * ScaleX, (vPos - dotRadius) * ScaleY);
                GL.Vertex2((hPos - dotRadius) * ScaleX, (vPos - dotRadius) * ScaleY);
            }
            GL.End();
            SwapBuffers();

            Logger.DotCount = dots.Sum(x => x.Dots.Count);
            Logger.FramesPrSecond = 1.0 / e.Time;
        }
    }
}