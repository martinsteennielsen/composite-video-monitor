using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Linq;

namespace CompositeVideoMonitor {
    public class Renderer : GameWindow {
        readonly Timing Timing;
        readonly VideoMonitor CRT;
        readonly Logger Logger;
        readonly double ScaleX, ScaleY;
        readonly double Focus = 1;
        readonly double DeltaXT1, DeltaXT2, DeltaXT3, DeltaYT1, DeltaYT2, DeltaYT3;

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
            double t1 = Timing.DotTime, t2 = Timing.LineTime, t3 = Timing.LineTime + Timing.DotTime;
            DeltaXT1 = Focus*(CRT.HPos(hOsc.Get(t1)) - CRT.HPos(hOsc.Get(0)));
            DeltaXT2 = Focus*(CRT.HPos(hOsc.Get(t2)) - CRT.HPos(hOsc.Get(0)));
            DeltaXT3 = Focus*(CRT.HPos(hOsc.Get(t3)) - CRT.HPos(hOsc.Get(0)));
            DeltaYT1 = Focus*(CRT.VPos(vOsc.Get(t1)) - CRT.VPos(vOsc.Get(0)));
            DeltaYT2 = Focus*(CRT.VPos(vOsc.Get(t2)) - CRT.VPos(vOsc.Get(0)));
            DeltaYT3 = Focus*(CRT.VPos(vOsc.Get(t3)) - CRT.VPos(vOsc.Get(0)));
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();
            GL.Begin(PrimitiveType.Quads);
            var dots = CRT.GetDots();
            var simulationTime = CRT.SimulatedTime;
            foreach (var dot in dots.SelectMany(x => x.Dots)) {
                //var dotLifeTime = 1 - (simulationTime - dot.Time) / CRT.PhosphorGlowTime;
                //if (dotLifeTime < 0) continue;
                //if (dotLifeTime > 1) continue;
                var brightness = dot.Brightness; //* dotLifeTime;
                GL.Color4(brightness, brightness, brightness, 0.3);
                double xPos = CRT.HPos(dot.HVolt);
                double yPos = CRT.VPos(dot.VVolt);
                GL.Vertex2(xPos * ScaleX, yPos * ScaleY);
                GL.Vertex2((DeltaXT1 + xPos) * ScaleX, (DeltaYT1 + yPos) * ScaleY);
                GL.Vertex2((DeltaXT3 + xPos) * ScaleX, (DeltaYT3 + yPos) * ScaleY);
                GL.Vertex2((DeltaXT2 + xPos) * ScaleX, (DeltaYT2 + yPos) * ScaleY);
            }
            GL.End();
            SwapBuffers();

            Logger.DotCount = dots.Sum(x => x.Dots.Count);
            Logger.FramesPrSecond = 1.0 / e.Time;
        }
    }
}