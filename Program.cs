using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Threading.Tasks;
using System.Threading;

namespace CompositeVideoMonitor
{

    class Program {
        static void Main(string[] args) {
            var monitor = new PalMonitor();
            using (Renderer renderer = new Renderer(monitor, 800, 600, "PAL")) {
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => { monitor.Run(cts.Token); } );
                renderer.Run(25);
                cts.Cancel();
            }
        }

   }
    class Renderer : GameWindow {
        VideoMonitor Model;
        public Renderer(VideoMonitor model, int width, int height, string title) : base(width, height, GraphicsMode.Default, title ) {
            Model = model;
        }
        protected override void OnUpdateFrame(FrameEventArgs e) {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape)) {
                Exit();
            }
            base.OnUpdateFrame(e);
        }
    }
 }