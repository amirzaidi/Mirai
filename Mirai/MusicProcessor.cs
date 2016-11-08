using System;
using System.Collections.Concurrent;
using System.Diagnostics;
//using System.IO;
//using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class MusicProcessor
    {
        public static BufferPool Buffers;
        private static int MaxBuffer = (int)Math.Pow(2, 15);
        //private const byte MaxBuffer = byte.MaxValue;

        public SongData Song;
        public CancellationTokenSource Skip = new CancellationTokenSource();
        public ConcurrentQueue<byte[]> QueuedBuffers = new ConcurrentQueue<byte[]>();

        public Semaphore Waiter = new Semaphore(MaxBuffer, MaxBuffer + 2);
        private Process Ffmpeg;
        private string Filter;

        public MusicProcessor(SongData PlaySong, string Filter)
        {
            Song = PlaySong;
            this.Filter = Filter == string.Empty ? Filter : $"-af \"{Filter}\"";

            Task.Run((Action)MainLoop);
        }

        private async void MainLoop()
        {
            var ReadBuffer = new byte[0];

            try
            {
                Ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    //Arguments = "-re -i pipe:0 -f s16le -ar 48000 -ac 2 -v quiet pipe:1",
                    Arguments = $"-i \"{Song.StreamUrl}\" {Filter} -f s16le -ar 48000 -ac 2 -v quiet pipe:1",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                });
                //Task.Run((Action)StreamToFfmpeg);

                Ffmpeg.EnableRaisingEvents = true;
                Ffmpeg.Exited += (s, e) => Skip.Cancel();
                
                int ReadBufferUsed = 0;
                while (!Skip.IsCancellationRequested)
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
                    
                    ReadBufferUsed += await Ffmpeg.StandardOutput.BaseStream.ReadAsync(ReadBuffer, ReadBufferUsed, ReadBuffer.Length - ReadBufferUsed, Skip.Token);
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }

            await Ffmpeg.StandardInput.WriteAsync("q");

            Ffmpeg.Close();
            Ffmpeg.Dispose();

            Bot.Log("Stopped saving " + Song.Title);

            if (ReadBuffer.Length != 0)
            {
                Buffers.Return(ReadBuffer);
            }
        }

        public void Stop()
        {
            Task.Run(() =>
            {
                try
                {
                    Skip.Cancel();
                    var OldBuffers = QueuedBuffers;
                    QueuedBuffers = new ConcurrentQueue<byte[]>();

                    while (OldBuffers.Count > 0)
                    {
                        Buffers.Return(OldBuffers.Dequeue());
                    }
                }
                catch (Exception Ex)
                {
                    Bot.Log(Ex);
                }
            });
        }

        /*private async void StreamToFfmpeg()
        {
            var Buffer = new byte[256 * 1024];
            int Read = 0;
            Stream In;

            try
            {
                if (Song.Type == SongType.Local)
                {
                    In = File.OpenRead(Song.StreamUrl);
                }
                else
                {
                    In = await new HttpClient(new HttpClientHandler())
                    {
                        Timeout = TimeSpan.FromMilliseconds(int.MaxValue)
                    }.GetStreamAsync(Song.StreamUrl);
                }

                while (!Skip.IsCancellationRequested && (Read = await In.ReadAsync(Buffer, 0, Buffer.Length, Skip.Token)) != 0)
                {
                    await Ffmpeg.StandardInput.BaseStream.WriteAsync(Buffer, 0, Read, Skip.Token);
                }
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
                //Bot.Log(System.Text.Encoding.ASCII.GetString(Buffer, 0, Read));
            }

            Bot.Log("Finished buffering " + Song.Title);
            if (!Skip.IsCancellationRequested)
            {
                Skip.Cancel();
            }
        }*/

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
