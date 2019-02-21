using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly TimingConstants Timing;
        readonly VideoMonitor CRT;
        readonly Logger Logger;
        readonly Controls Controls;

        readonly double ScaleX, ScaleY;
        readonly double DotWidth, DotHeight;

        public Renderer(Controls controls, VideoMonitor monitor, TimingConstants timing, Logger logger, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            CRT = monitor;
            Timing = timing;
            Logger = logger;
            Controls = controls;

            var hOsc = new SawtoothSignal(frequency: timing.HFreq, phase: 0);
            var vOsc = new SawtoothSignal(frequency: timing.VFreq, phase: 0);
            ScaleX = 2.0 / VideoMonitor.TubeWidth;
            ScaleY = 2.0 / VideoMonitor.TubeHeight;
            DotWidth = 0.5 * ScaleX * (CRT.HPos(hOsc.Get(Timing.DotTime)) - CRT.HPos(hOsc.Get(0)));
            DotHeight = 0.5 * ScaleY * (CRT.VPos(vOsc.Get(Timing.LineTime)) - CRT.VPos(vOsc.Get(0)));

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.OneMinusDstAlpha);
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e) {
            if (!Controls.ProcessKey(e)) {
                Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e) {

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();

            var allDots = CRT.CurrentFrame().SelectMany(x => x.Dots).ToList();
            if (!allDots.Any()) { return; }

            GL.Begin(PrimitiveType.Quads);
            if (Controls.Cursor) {
                PhosphorDot first = allDots.First();
                Controls.ProcessPosition(CRT.HPos(first.HVolt), CRT.VPos(first.VVolt));
                GL.Color4(0d, 01d, 0d, 0.1d);
                RenderDot(first, Controls.Focus * 1.4);

                PhosphorDot last = allDots.Last();
                Controls.ProcessPosition(CRT.HPos(last.HVolt), CRT.VPos(last.VVolt));
                GL.Color4(1d, 0d, 0d, 0.1d);
                RenderDot(last, Controls.Focus * 1.4);
            }
            foreach (var dot in allDots.Skip(1).Take(allDots.Count - 2)) {
                GL.Color3(dot.Brightness * Math.Abs(dot.sync), dot.Brightness, dot.Brightness);
                RenderDot(dot, Controls.Focus);
            }
            GL.End();

            SwapBuffers();

            Logger.DotCount = allDots.Count;
            Logger.FramesPrSecond = 1.0 / e.Time;
        }

        private void RenderDot(PhosphorDot dot, double focus) {
            double z = VideoMonitor.TubeWidth / Controls.TubeViewSize;
            double xPos = z * ScaleX * (Controls.TubeViewX + CRT.HPos(dot.HVolt));
            double yPos = -z * ScaleY * (Controls.TubeViewY + CRT.VPos(dot.VVolt));
            double dotWidth = focus * DotWidth * z;
            double dotHeight = focus * DotHeight * z;
            GL.Vertex2(xPos - dotWidth, yPos + dotHeight);
            GL.Vertex2(xPos + dotWidth, yPos + dotHeight);
            GL.Vertex2(xPos + dotWidth, yPos - dotHeight);
            GL.Vertex2(xPos - dotWidth, yPos - dotHeight);
        }
    }
}