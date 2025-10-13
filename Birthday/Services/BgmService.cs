using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birthday.Services
{
    public sealed class BgmService : IDisposable
    {
        private static readonly Lazy<BgmService> _lazy = new(() => new BgmService());
        public static BgmService Instance => _lazy.Value;

        private IWavePlayer? _output;                 // WaveOutEvent
        private AudioFileReader? _reader;             // 讀檔（支援 mp3/wav/m4a 視編解碼）
        private FadeInOutSampleProvider? _fader;      // 淡入淡出
        private bool _isLoop;
        private float _volume = 0.8f;                 // 0~1

        private BgmService() { }

        /// <summary>播放 BGM；若已有音樂在播，先淡出舊音樂再換新歌。</summary>
        public void Play(string filePath, bool loop = true, float targetVolume = 0.8f, double fadeSeconds = 0.8)
        {
            try
            {
                _isLoop = loop;
                _volume = targetVolume;

                // 如果已有在播，先淡出後再切
                if (_output != null)
                {
                    FadeOutThen(() => StartNew(filePath, fadeSeconds));
                }
                else
                {
                    StartNew(filePath, fadeSeconds);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[BGM] Play error: {ex}");
            }
        }

        private void StartNew(string filePath, double fadeSeconds)
        {
            Cleanup(); // 先清

            // 1) AudioFileReader 本身就是 WaveStream
            _reader = new AudioFileReader(filePath) { Volume = 1.0f };

            // 2) 用 WaveStream 來建 LoopStream（這樣型別才正確）
            var loopStream = new LoopStream(_reader);              // ✅

            // 3) 轉成 ISampleProvider，接淡入淡出
            var sample = loopStream.ToSampleProvider();            // ✅
            _fader = new FadeInOutSampleProvider(sample, true);
            _fader.BeginFadeIn(fadeSeconds);

            // 4) 播放
            _output = new WaveOutEvent();
            _output.Init(_fader);
            SetVolume(_volume);
            _output.Play();
        }

        /// <summary>停止播放，可帶淡出。</summary>
        public void Stop(double fadeSeconds = 0.6)
        {
            try
            {
                if (_fader != null && _output != null)
                {
                    _fader.BeginFadeOut(fadeSeconds);
                    // 淡出完成後釋放
                    var timer = new System.Timers.Timer(fadeSeconds * 1000) { AutoReset = false };
                    timer.Elapsed += (s, e) => Cleanup();
                    timer.Start();
                }
                else
                {
                    Cleanup();
                }
            }
            catch { Cleanup(); }
        }

        /// <summary>設定音量（0~1）。</summary>
        public void SetVolume(double v)
        {
            _volume = (float)Math.Clamp(v, 0.0, 1.0);
            if (_output is WaveOutEvent wo)
            {
                wo.Volume = _volume; // WaveOutEvent 直接有 Volume
            }
        }

        private void FadeOutThen(Action next, double fadeSeconds = 0.5)
        {
            if (_fader != null)
            {
                _fader.BeginFadeOut(fadeSeconds);
                var timer = new System.Timers.Timer(fadeSeconds * 1000) { AutoReset = false };
                timer.Elapsed += (s, e) =>
                {
                    Cleanup();
                    next();
                    timer.Dispose();
                };
                timer.Start();
            }
            else
            {
                Cleanup();
                next();
            }
        }

        private void Cleanup()
        {
            try { _output?.Stop(); } catch { }
            try { _output?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }

            _output = null;
            _reader = null;
            _fader = null;
        }

        public void Dispose() => Cleanup();

        // === 迴圈封裝：把來源無限回圈 ===
        private sealed class LoopStream : WaveStream
        {
            private readonly WaveStream _sourceStream;

            public LoopStream(WaveStream sourceStream) => _sourceStream = sourceStream;
            public override WaveFormat WaveFormat => _sourceStream.WaveFormat;
            public override long Length => long.MaxValue; // 模擬無限長
            public override long Position { get => _sourceStream.Position; set => _sourceStream.Position = value; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int totalBytesRead = 0;
                while (totalBytesRead < count)
                {
                    int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        // 到尾端 → 從頭繼續
                        _sourceStream.Position = 0;
                        bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                        if (bytesRead == 0) break;
                    }
                    totalBytesRead += bytesRead;
                }
                return totalBytesRead;
            }
        }
    }
}
