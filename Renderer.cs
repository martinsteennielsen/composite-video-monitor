using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace CompositeVideoMonitor
{
    public class Renderer : GameWindow {
        readonly TvNorm TvNorm;
        readonly Tube CRT;
        readonly Controls Controls;
        readonly Func<double, double, bool> ShowCursor;


        readonly double ScaleX, ScaleY;
        readonly double DotWidth, DotHeight, OffSetX, VisibleWidth, VisibleHeight;

        public Renderer(Controls controls, Func<double, double, bool> showCursor, Tube tube, TvNorm tvNorm, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            TvNorm = tvNorm;
            Controls = controls;
            CRT = tube;
            ShowCursor = showCursor;

            var hOsc = new SawtoothSignal { Frequency = tvNorm.Frequencies.Horizontal };
            var vOsc = new SawtoothSignal { Frequency = tvNorm.Frequencies.Vertical };
            double minX = CRT.HPos(hOsc.Get(TvNorm.Sync.LineBlankingTime-TvNorm.Sync.FrontPorchTime));
            double maxX = CRT.HPos(hOsc.Get(TvNorm.Frequencies.LineTime - TvNorm.Sync.FrontPorchTime));
            VisibleWidth = maxX - minX;
            double minY = CRT.VPos(vOsc.Get(25*TvNorm.Frequencies.LineTime));
            double maxY = CRT.VPos(vOsc.Get(TvNorm.Frequencies.FrameTime - 10 * TvNorm.Frequencies.LineTime));
            VisibleHeight = maxY - minY;
            ScaleY = ScaleX = 2.0 / VisibleWidth;
            OffSetX = -(CRT.HPos(hOsc.Get(TvNorm.Sync.LineBlankingTime- TvNorm.Sync.FrontPorchTime))) / ScaleX;
            DotWidth = 0.5 * ScaleX * (CRT.HPos(hOsc.Get(TvNorm.Frequencies.DotTime)) - CRT.HPos(hOsc.Get(0)));
            DotHeight = 0.5 * ScaleY * (CRT.VPos(vOsc.Get(TvNorm.Frequencies.LineTime)) - CRT.VPos(vOsc.Get(0)));
        }

        protected override void OnRenderFrame(FrameEventArgs e) {

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();

            var allDots = CRT.GetPicture();
            if (!allDots.Any()) { return; }

            GL.Begin(PrimitiveType.Quads);

            PhosphorDot last = allDots.Last();
            PhosphorDot first = allDots.First();
            if (ShowCursor(CRT.HPos(last.HVolt), CRT.VPos(last.VVolt))) {
                GL.Color3(1d, 0d, 0d);
                RenderDot(last, Controls.Focus);
                GL.Color3(0d, 01d, 0d);
                RenderDot(first, Controls.Focus);
            } else {
                GL.Color3(first.Brightness, first.Brightness, first.Brightness);
                RenderDot(first, Controls.Focus);
                GL.Color3(last.Brightness, last.Brightness, last.Brightness);
                RenderDot(last, Controls.Focus);
            }

            foreach (var dot in allDots.Skip(1).Take(allDots.Count - 2)) {
                var brightness = dot.Brightness * Controls.Brightness;
                GL.Color3(brightness, brightness, brightness);
                RenderDot(dot, Controls.Focus);
            }
            GL.End();

            SwapBuffers();
        }

        private void RenderDot(PhosphorDot dot, double focus) {
            double xPos = Controls.TubeZoom * ScaleX * (Controls.TubeViewX + CRT.HPos(dot.HVolt) - OffSetX);
            double yPos = -Controls.TubeZoom * ScaleY * (Controls.TubeViewY + CRT.VPos(dot.VVolt));
            double dotWidth = focus * DotWidth * Controls.TubeZoom;
            double dotHeight = focus * DotHeight * Controls.TubeZoom;
            GL.Vertex2(xPos - dotWidth, yPos);
            GL.Vertex2(xPos + dotWidth, yPos);
            GL.Vertex2(xPos + dotWidth, yPos - dotHeight);
            GL.Vertex2(xPos - dotWidth, yPos - dotHeight);
        }
    }
}