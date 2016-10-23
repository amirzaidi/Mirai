using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class MusicProcessor
    {
        public static BufferPool Buffers;
        private static int MaxBuffer = (int)Math.Pow(2, 15);

        public SongData Song;
        public CancellationTokenSource Skip = new CancellationTokenSource();
        public ConcurrentQueue<byte[]> QueuedBuffers = new ConcurrentQueue<byte[]>();
        private Process Ffmpeg;

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
                    Arguments = "-re -i pipe:0 -f s16le -ar 48000 -ac 2 -v quiet pipe:1",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                });
                
                var Response = await WebRequest.Create(Song.StreamUrl).GetResponseAsync();

                Task.Run(async delegate
                {
                    try
                    {
                        await Response.GetResponseStream().CopyToAsync(Ffmpeg.StandardInput.BaseStream, 81920, Skip.Token);
                        Bot.Log("Finished buffering " + Song.Title);
                        Skip.Cancel();
                    }
                    catch (TaskCanceledException)
                    {
                        Bot.Log("Cancelled buffering " + Song.Title);
                    }
                });

                int Read = 0;

                var ReadBuffer = new byte[0];
                int ReadBufferUsed = 0;

                while (true)
                {
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

                    try
                    {
                        Read = await Ffmpeg.StandardOutput.BaseStream.ReadAsync(ReadBuffer, ReadBufferUsed, ReadBuffer.Length - ReadBufferUsed, Skip.Token);
                        ReadBufferUsed += Read;
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                if (ReadBuffer.Length != 0)
                {
                    Buffers.Return(ReadBuffer);
                }

                if (Ffmpeg != null)
                {
                    Ffmpeg.Close();
                    Ffmpeg.Dispose();
                }

                Ffmpeg = null;
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
