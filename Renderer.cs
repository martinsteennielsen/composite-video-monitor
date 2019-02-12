using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Linq;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly TimingConstants Timing;
        readonly VideoMonitor CRT;
        readonly Logger Logger;
        readonly double ScaleX, ScaleY;
        double Focus = 1;
        readonly double DotWidth, DotHeight;

        public Renderer(VideoMonitor monitor, TimingConstants timing, Logger logger, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            CRT = monitor;
            Timing = timing;
            Logger = logger;
            KeyDown += OnKeyDown;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.DstColor);

            var hOsc  = new SawtoothSignal(frequency: timing.HFreq, phase: 0);
            var vOsc  = new SawtoothSignal(frequency: timing.VFreq, phase: 0);
            ScaleX    = 2.0 / CRT.TubeWidth;
            ScaleY    = 2.0 / CRT.TubeHeight;
            DotWidth  = ScaleX * ( CRT.HPos(hOsc.Get(Timing.DotTime)) - CRT.HPos(hOsc.Get(0)) );
            DotHeight = ScaleY * ( CRT.VPos(vOsc.Get(Timing.LineTime)) - CRT.VPos(vOsc.Get(0)) );
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e) {
            if (e.Key == Key.Escape) {
                Exit();
            }
            if (e.Key == Key.F) {
                Focus *= 1.05;
            }
            if (e.Key == Key.D) {
                Focus /= 1.05;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            var (simulationTime, frame) = CRT.GetFrame();

            double dx = 0.5 * Focus * DotWidth;
            double dy = 0.5 * Focus * DotHeight;
            var dots = CRT.GetFrame();

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);

            foreach (var dot in frame.SelectMany(x => x.Dots)) {
                double brightness = dot.Brightness / Focus;
                GL.Color3(brightness, brightness, brightness);
                double xPos =   ScaleX * CRT.HPos(dot.HVolt);
                double yPos = - ScaleY  *CRT.VPos(dot.VVolt);
                GL.Vertex2( xPos - dx, yPos + dy);
                GL.Vertex2( xPos + dx, yPos + dy);
                GL.Vertex2( xPos + dx, yPos - dy);
                GL.Vertex2( xPos - dx, yPos - dy);
            }
            GL.End();

            SwapBuffers();

            Logger.DotCount = frame.Sum(x => x.Dots.Count);
            Logger.FramesPrSecond = 1.0 / e.Time;
        }
    }
}