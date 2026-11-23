using OpenCvSharp;

namespace FaceRecognition.Vision;

/// <summary>
/// Kameradan arka planda kare okur ve her kareyi FrameReady olayıyla bildirir.
/// Kareler arka plan thread'inden geliyor; form bunları UI thread'ine aktarmalı.
/// </summary>
public sealed class CameraService : IDisposable
{
    private VideoCapture? capture;
    private CancellationTokenSource? cts;
    private Task? loop;

    public bool IsRunning => loop is { IsCompleted: false };

    /// <summary>Yeni kare. Olayı dinleyen kareyi kendisi Dispose etmeli.</summary>
    public event EventHandler<Mat>? FrameReady;

    /// <summary>Kamera beklenmedik şekilde kapandığında.</summary>
    public event EventHandler<string>? Failed;

    public void Start(int cameraIndex = 0)
    {
        if (IsRunning)
            return;

        var cap = new VideoCapture();
        try
        {
            cap.Open(cameraIndex, VideoCaptureAPIs.DSHOW);
            if (!cap.IsOpened())
                cap.Open(cameraIndex, VideoCaptureAPIs.ANY);
        }
        catch (Exception ex) when (ex is OpenCVException or OpenCvSharpException)
        {
            cap.Dispose();
            throw new VisionException("Kamera açılamadı: " + ex.Message, ex);
        }

        if (!cap.IsOpened())
        {
            cap.Dispose();
            throw new VisionException("Kamera bulunamadı veya başka bir uygulama tarafından kullanılıyor. " +
                                      "Kamerayı kontrol edin ya da \"Görüntü seç\" ile bir fotoğraf kullanın.");
        }

        capture = cap;
        cts = new CancellationTokenSource();
        var token = cts.Token;
        loop = Task.Run(() => ReadLoop(cap, token), token);
    }

    private void ReadLoop(VideoCapture cap, CancellationToken token)
    {
        int emptyFrames = 0;
        while (!token.IsCancellationRequested)
        {
            var frame = new Mat();
            if (!cap.Read(frame) || frame.Empty())
            {
                frame.Dispose();
                if (++emptyFrames > 50)
                {
                    Failed?.Invoke(this, "Kameradan görüntü alınamıyor (bağlantı kopmuş olabilir).");
                    return;
                }
                Thread.Sleep(20);
                continue;
            }

            emptyFrames = 0;
            var handler = FrameReady;
            if (handler is null)
                frame.Dispose();
            else
                handler(this, frame);

            Thread.Sleep(30);   // ~25 fps yeterli, işlemciyi yormasın
        }
    }

    public void Stop()
    {
        cts?.Cancel();
        try { loop?.Wait(1000); } catch (AggregateException) { }
        capture?.Release();
        capture?.Dispose();
        capture = null;
        loop = null;
    }

    public void Dispose() => Stop();
}
