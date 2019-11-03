using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace CompositeVideoMonitor {
    
    public class Renderer : GameWindow {
        readonly Func<Picture> GetPicture;
        readonly Controls Controls;
        readonly Func<double, double, bool> ShowCursor;

        public Renderer(Controls controls, Func<Picture> getPicture, Func<double, double, bool> showCursor, int width, int height, string title) : base(width, height, GraphicsMode.Default, title) {
            GetPicture = getPicture;
            Controls = controls;
            ShowCursor = showCursor;
        }

        protected override void OnRenderFrame(FrameEventArgs e) {

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Flush();

            var picture = GetPicture();
            if (!picture.Dots.Any()) { return; }
            Title = picture.Info;
            GL.Begin(PrimitiveType.Quads);

            void render(PhosphorDot dot) =>
                RenderDot(dot, Controls.Focus, picture.DotWidth, picture.DotHeight,scale: 2.0/picture.Height);

            PhosphorDot last = picture.Dots.Last();
            PhosphorDot first = picture.Dots.First();
            if (ShowCursor(last.HPos, last.VPos)) {
                GL.Color3(1d, 0d, 0d);
                render(last);
                GL.Color3(0d, 01d, 0d);
                render(first);
            } else {
                GL.Color3(first.Brightness, first.Brightness, first.Brightness);
                render(first);
                GL.Color3(last.Brightness, last.Brightness, last.Brightness);
                render(last);
            }

            foreach (var dot in picture.Dots.Skip(1).Take(picture.Dots.Count - 2)) {
                var brightness = dot.Brightness * Controls.Brightness;
                GL.Color3(brightness, brightness, brightness);
                render(dot);
            }
            GL.End();

            SwapBuffers();
        }

        private void RenderDot(PhosphorDot dot, double focus, double w, double h, double scale) {
            double xPos = scale * Controls.TubeZoom * (Controls.TubeViewX + dot.HPos);
            double yPos = scale * -Controls.TubeZoom * (Controls.TubeViewY + dot.VPos);
            double halfDotWidth = scale * 0.5 * focus * w * Controls.TubeZoom;
            double halfDotHeight = scale * 0.5 * focus * h * Controls.TubeZoom;
            GL.Vertex2(xPos - halfDotWidth, yPos - halfDotHeight);
            GL.Vertex2(xPos + halfDotWidth, yPos - halfDotHeight);
            GL.Vertex2(xPos + halfDotWidth, yPos + halfDotHeight);
            GL.Vertex2(xPos - halfDotWidth, yPos + halfDotHeight);
        }
    }
}