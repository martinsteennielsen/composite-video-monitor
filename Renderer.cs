using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Linq;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly Timing Timing;
        readonly VideoMonitor CRT;
        readonly Logger Logger;
        readonly double ScaleX, ScaleY;
        readonly double Focus = 1;
        readonly double DotWidth, DotHeight;

        public Renderer(VideoMonitor monitor, Timing timing, Logger logger, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            CRT = monitor;
            Timing = timing;
            Logger = logger;
            ScaleX = 2.0 / CRT.TubeWidth;
            ScaleY = -2.0 / CRT.TubeHeight;

            KeyDown += (_, e) => {
                if (e.Key == Key.Escape) {
                    Exit();
                }
            };

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.DstColor);

            var hOsc = new SawtoothSignal(frequency: timing.HFreq, phase: 0);
            var vOsc = new SawtoothSignal(frequency: timing.VFreq, phase: 0);
            DotWidth = ScaleX * CRT.HPos(hOsc.Get(Timing.DotTime)) - CRT.HPos(hOsc.Get(0));
            DotHeight= -ScaleY * CRT.VPos(vOsc.Get(Timing.LineTime)) - CRT.VPos(vOsc.Get(0));
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();

            GL.Begin(PrimitiveType.Quads);
            var dots = CRT.GetDots();
            double simulationTime = CRT.SimulatedTime;
            double dx = 0.5 * Focus * DotWidth;
            double dy = 0.5 * Focus * DotHeight;
            foreach (var dot in dots.SelectMany(x => x.Dots)) {
                double brightness = dot.Brightness;
                GL.Color4(brightness, brightness, brightness, 0.3);
                double xPos = ScaleX*CRT.HPos(dot.HVolt);
                double yPos = ScaleY*CRT.VPos(dot.VVolt);
                GL.Vertex2( xPos - dx, yPos + dy);
                GL.Vertex2( xPos + dx, yPos + dy);
                GL.Vertex2( xPos + dx, yPos - dy);
                GL.Vertex2( xPos - dx, yPos - dy);
            }
            GL.End();

            SwapBuffers();

            Logger.DotCount = dots.Sum(x => x.Dots.Count);
            Logger.FramesPrSecond = 1.0 / e.Time;
        }
    }
}