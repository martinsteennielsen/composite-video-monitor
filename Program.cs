using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace CompositeVideoMonitor {

    class Program {
        static void Main(string[] args) {
            using (VideoMonitor monitor = new PalMonitor(800, 600, "PAL")) {
                monitor.Run(60.0);
            }
        }
        class PalMonitor : VideoMonitor {
            public PalMonitor(int width, int height, string title) : base(width, height, title, hSyncFreq: 15625, vSyncFreq: 25, bandwidthFreq: 5e6) {
            }
        }
        
        class VideoMonitor : GameWindow {
            CRT Screen;
            double VSyncFreq;
            double HSyncFreq;
            double NoOfLines;
            double SampleTime;

            public VideoMonitor(int width, int height, string title, double hSyncFreq, double vSyncFreq,double bandwidthFreq) : base(width, height, GraphicsMode.Default, title) {
                Screen = new CRT(height:0.03, width:0.04, deflectionVoltage: 400);
                VSyncFreq = vSyncFreq;
                HSyncFreq = hSyncFreq;
                NoOfLines = hSyncFreq/vSyncFreq;
                SampleTime = 1 / bandwidthFreq;
            }

            protected override void OnUpdateFrame(FrameEventArgs e) {
                KeyboardState input = Keyboard.GetState();
                if (input.IsKeyDown(Key.Escape)) {
                    Exit();
                }
                base.OnUpdateFrame(e);
            }
        }
        class CRT {
            readonly double Width;
            readonly double Height;
            readonly double HDeflection;
            readonly double VDeflection;
            readonly double PhosphorPersistenseTime = 0.1;
            readonly double PhosphorBleedDistance = 0.001;
            readonly HashSet<PhosphorExcitation> Excitations;
            double HPos(double hVoltage) => hVoltage*HDeflection;
            double VPos(double vVoltage) => vVoltage*VDeflection;

            public CRT(double height, double width, double deflectionVoltage) {
                VDeflection = height/deflectionVoltage;
                HDeflection = width/deflectionVoltage;
                Excitations = new HashSet<PhosphorExcitation>();
            }

            class PhosphorExcitation {
                double VSyncVoltage;
                double HSyncVoltage;
                double StartTime;
                double StartLevel;
            }
        }
    }
}