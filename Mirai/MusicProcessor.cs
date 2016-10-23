using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class MusicProcessor
    {
        public static BufferPool Buffers;
        private static int MaxBuffer = (int)Math.Pow(2, 15);

        public SongData Song;
        public bool Skip = false;
        public ConcurrentQueue<byte[]> QueuedBuffers = new ConcurrentQueue<byte[]>();
        private Process Ffmpeg;

        public bool FinishedBuffer = false;

        public Semaphore Waiter = new Semaphore(MaxBuffer, MaxBuffer + 2);

        public MusicProcessor(SongData PlaySong)
        {
            Song = PlaySong;
            Task.Run((Action)MainLoop);
        }

        private async void MainLoop()
        {
            try
            {
                Ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    //Arguments = "-i \"" + Song.StreamUrl + "\" -f s16le -ar 48000 -ac 2 -v quiet pipe:1",
                    Arguments = "-i pipe:0 -f s16le -ar 48000 -ac 2 -v quiet pipe:1",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                });

                var Cancel = new CancellationTokenSource();
                var Response = await System.Net.WebRequest.Create(Song.StreamUrl).GetResponseAsync();
                var Copying = Response.GetResponseStream().CopyToAsync(Ffmpeg.StandardInput.BaseStream, 81920, Cancel.Token);

                int Read = 0;

                var ReadBuffer = new byte[0];
                int ReadBufferUsed = 0;

                int Fails = 0;

                while (true)
                {
                    if (Skip)
                    {
                        if (ReadBuffer.Length != 0)
                        {
                            Buffers.Return(ReadBuffer);
                        }

                        break;
                    }

                    if (ReadBufferUsed == ReadBuffer.Length)
                    {
                        if (ReadBufferUsed != 0)
                        {
                            QueuedBuffers.Enqueue(ReadBuffer);
                            Waiter.WaitOne();

                            ReadBufferUsed = 0;
                        }

                        ReadBuffer = Buffers.Take();
                    }

                    Read = await Ffmpeg.StandardOutput.BaseStream.ReadAsync(ReadBuffer, ReadBufferUsed, ReadBuffer.Length - ReadBufferUsed);

                    if (Read == 0)
                    {
                        if (++Fails == 15)
                        {
                            QueuedBuffers.Enqueue(ReadBuffer);
                            break;
                        }

                        await Task.Delay(50);
                    }
                    else
                    {
                        ReadBufferUsed += Read;
                        Fails = 0;
                    }
                }

                FinishedBuffer = true;
                Bot.Log((Fails == 15 ? "Finished" : "Stopped") + " buffering " + Song.Title);

                if (Ffmpeg != null)
                {
                    Ffmpeg.Close();
                    Ffmpeg.Dispose();
                }

                Ffmpeg = null;
                Cancel.Cancel();
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }
        }

        public void Dispose()
        {
            Task.Run(() =>
            {
                try
                {
                    while (QueuedBuffers.Count > 0)
                    {
                        Buffers.Return(QueuedBuffers.Dequeue());
                    }
                }
                catch (Exception Ex)
                {
                    Bot.Log(Ex);
                }
            });
        }
    }
}
