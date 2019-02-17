using System.Diagnostics;
using System.Threading.Tasks;

namespace CompositeVideoMonitor {

    public interface ITimeKeeper {
        Task<(double elapsedTime, double skippedTime)> GetElapsedTimeAsync();
    }

    public class StoppedTime : ITimeKeeper {
        public async Task<(double elapsedTime, double skippedTime)> GetElapsedTimeAsync() {
            await Task.Delay(300);
            return (0, 0);
        }
    }

    public class TimeKeeper : ITimeKeeper {
        public readonly double ZoomTime;
        readonly Stopwatch StopWatch;
        readonly double MinTime, MaxTime;

        public TimeKeeper(double zoomTime, double minTime, double maxTime) {
            MinTime = minTime * zoomTime;
            MaxTime = maxTime * zoomTime;
            StopWatch = new Stopwatch();
            ZoomTime = zoomTime;
            StopWatch.Start();
        }

        async Task Sleep() {
            if (MinTime / ZoomTime > 0.001) {
                await Task.Delay((int)(1000d * MinTime / ZoomTime));
            } else {
                await Task.Yield();
            }
        }

        double SimulatedTime = 0;
        public async Task<(double elapsedTime, double skippedTime)> GetElapsedTimeAsync() {
            while (ZoomTime * StopWatch.Elapsed.TotalSeconds - SimulatedTime < MinTime) {
                await Sleep();
            }

            var currentTime = ZoomTime * StopWatch.Elapsed.TotalSeconds;
            var elapsedTime = (currentTime - SimulatedTime);
            SimulatedTime = currentTime;

            if (elapsedTime > MaxTime) {
                return (elapsedTime: MaxTime, skippedTime: (elapsedTime - MaxTime));
            } else {
                return (elapsedTime, 0);
            }
        }
    }
}
