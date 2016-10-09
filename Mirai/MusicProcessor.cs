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
            Ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-i \"" + Song.StreamUrl + "\" -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            MainLoop();
        }

        private async Task MainLoop()
        {
            int Read = 0;

            byte[] ReadBuffer = new byte[0];
            int ReadBufferUsed = 0;

            int Fails = 0;

            try
            {
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
                        if (++Fails == 10)
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
                Bot.Log((Fails == 10 ? "Finished" : "Stopped") + " buffering " + Song.Title);
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }

            try
            {
                if (Ffmpeg != null)
                {
                    Ffmpeg.Close();
                    Ffmpeg.Dispose();
                }
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }

            Ffmpeg = null;
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
