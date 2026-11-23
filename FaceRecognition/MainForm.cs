using System.Diagnostics;
using FaceRecognition.Data;
using FaceRecognition.Ui;
using FaceRecognition.Vision;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FaceRecognition;

public partial class MainForm : Form
{
    private readonly FaceRecognizerService recognizer = new();
    private readonly SampleLibrary library = new(SampleLibrary.DefaultFolder);
    private readonly CameraService camera = new();
    private readonly object visionLock = new();
    private FaceDetector? detector;
    private FaceAnalyzer? analyzer;

    private Mat? currentImage;
    private IReadOnlyList<FaceMatch> currentMatches = Array.Empty<FaceMatch>();
    private volatile bool frameBusy;

    public MainForm()
    {
        InitializeComponent();
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch (Exception) { /* varsayılan ikon */ }

        camera.FrameReady += Camera_FrameReady;
        camera.Failed += (_, msg) => BeginInvoke(() =>
        {
            StopCamera();
            ShowAlert(msg, error: true);
        });

        toolTip.SetToolTip(trkThreshold, "Yüz, en yakın kişiye bu mesafeden daha uzaksa \"Bilinmeyen\" sayılır. Küçük değer = daha seçici.");
        toolTip.SetToolTip(trkMargin, "En yakın iki kişi arasındaki fark bundan küçükse \"Bilinmeyen\" sayılır. Büyük değer = daha seçici.");
        toolTip.SetToolTip(btnSaveFaces, "Ekrandaki yüzleri ayrı ayrı .png dosyası olarak bir klasöre kaydeder");
    }

    // ===================== Açılış =====================

    private void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            detector = new FaceDetector(FaceDetector.DefaultCascadePath);
        }
        catch (VisionException ex)
        {
            // Model dosyası yoksa uygulama kapanmıyor, ne olduğunu söylüyor
            ShowAlert(ex.Message, error: true);
            btnCamera.Enabled = btnImage.Enabled = btnSample.Enabled = btnTraining.Enabled = false;
            chipModel.Text = "Algılama modeli yok";
            chipModel.Dot = Palette.Danger;
            return;
        }

        analyzer = new FaceAnalyzer(detector, recognizer);
        TrainFromLibrary();
        UpdateSensitivityLabels();
        UpdateSourceChip();
    }

    internal bool TrainFromLibrary()
    {
        if (detector is null) return false;
        UseWaitCursor = true;
        try
        {
            var (samples, skipped) = library.LoadAll();
            try
            {
                lock (visionLock)
                    recognizer.Train(samples, detector);
            }
            finally
            {
                foreach (var s in samples) s.Face.Dispose();
            }

            UpdateModelChip();
            lblStatus.Text = $"Model eğitildi: {recognizer.Labels.Count} kişi, {recognizer.SampleCount} örnek, {recognizer.LastTrainingTime.TotalMilliseconds:0} ms" +
                             (skipped > 0 ? $"  ·  {skipped} dosya okunamadı" : "");
            return true;
        }
        catch (VisionException ex)
        {
            ShowAlert(ex.Message + "  Eğitim penceresinden örnek ekleyebilirsiniz.", error: true);
            UpdateModelChip();
            return false;
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    // ===================== Görüntü kaynakları =====================

    private void btnImage_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Görüntü seç",
            Filter = "Resim dosyaları (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tüm dosyalar (*.*)|*.*"
        };
        string deneme = Path.Combine(AppContext.BaseDirectory, "DenemeGorselleri");
        if (Directory.Exists(deneme))
            dialog.InitialDirectory = deneme;

        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadImage(dialog.FileName);
    }

    private void btnSample_Click(object? sender, EventArgs e) =>
        LoadImage(Path.Combine(AppContext.BaseDirectory, "DenemeGorselleri", "grup-fotografi.png"));

    internal bool LoadImage(string path)
    {
        if (analyzer is null) return false;
        StopCamera();

        Mat image;
        try
        {
            image = ImageIO.Read(path);
        }
        catch (VisionException ex)
        {
            ShowAlert(ex.Message, error: true);
            return false;
        }

        if (image.Empty())
        {
            image.Dispose();
            ShowAlert($"\"{Path.GetFileName(path)}\" bir resim dosyası olarak okunamadı.", error: true);
            return false;
        }

        HideAlert();
        var sw = Stopwatch.StartNew();
        IReadOnlyList<FaceMatch> matches;
        lock (visionLock)
            matches = analyzer.Analyze(image);
        ShowFrame(image, matches, sw.Elapsed);
        lblSourceInfo.Text = Path.GetFileName(path);
        UpdateSourceChip(Path.GetFileName(path));
        return true;
    }

    private void btnCamera_Click(object? sender, EventArgs e)
    {
        if (camera.IsRunning)
            StopCamera();
        else
            StartCamera();
    }

    internal bool StartCamera()
    {
        if (analyzer is null) return false;
        try
        {
            camera.Start(0);
        }
        catch (VisionException ex)
        {
            ShowAlert(ex.Message, error: true);
            UpdateSourceChip();
            return false;
        }

        HideAlert();
        btnCamera.Text = "Kamerayı durdur";
        btnCamera.Kind = ScanButtonKind.Danger;
        lblSourceInfo.Text = "Canlı kamera görüntüsü";
        UpdateSourceChip();
        return true;
    }

    private void StopCamera()
    {
        if (!camera.IsRunning)
            return;
        camera.Stop();
        btnCamera.Text = "Kamerayı başlat";
        btnCamera.Kind = ScanButtonKind.Primary;
        lblSourceInfo.Text = "Kamera durduruldu (son kare ekranda)";
        UpdateSourceChip();
    }

    /// <summary>Kameradan gelen kare (arka plan thread'i). Analiz burada yapılıp ekrana UI thread'inde basılıyor.</summary>
    private void Camera_FrameReady(object? sender, Mat frame)
    {
        if (frameBusy || analyzer is null || IsDisposed)
        {
            frame.Dispose();
            return;
        }

        frameBusy = true;
        var sw = Stopwatch.StartNew();
        IReadOnlyList<FaceMatch> matches;
        lock (visionLock)
            matches = analyzer.Analyze(frame);
        var elapsed = sw.Elapsed;

        try
        {
            BeginInvoke(() =>
            {
                ShowFrame(frame, matches, elapsed);
                frameBusy = false;
            });
        }
        catch (InvalidOperationException)
        {
            frame.Dispose();     // form kapanıyor
        }
    }

    /// <summary>Görüntüyü ve sonuçları ekrana basar. Mat'in sahipliği bu metoda geçiyor.</summary>
    private void ShowFrame(Mat image, IReadOnlyList<FaceMatch> matches, TimeSpan elapsed)
    {
        var oldBitmap = picView.Image;
        picView.Image = image.ToBitmap();
        picView.Matches = matches;
        oldBitmap?.Dispose();

        currentImage?.Dispose();
        currentImage = image;
        currentMatches = matches;

        lstResults.SetItems(matches.Select(m =>
        {
            using var crop = new Mat(image, m.Area);
            using var small = crop.Resize(new OpenCvSharp.Size(88, 88));
            return new FaceResultList.Item(small.ToBitmap(), m);
        }).ToList());

        int known = matches.Count(m => m.IsKnown);
        lblStatus.Text = matches.Count == 0
            ? $"Yüz bulunamadı  ·  {image.Width}x{image.Height}  ·  {elapsed.TotalMilliseconds:0} ms"
            : $"{matches.Count} yüz bulundu  ·  {known} tanındı  ·  {matches.Count - known} bilinmeyen  ·  {elapsed.TotalMilliseconds:0} ms";
        btnSaveFaces.Enabled = matches.Count > 0;
    }

    // ===================== Hassasiyet =====================

    private void sensitivity_ValueChanged(object? sender, EventArgs e)
    {
        lock (visionLock)
        {
            recognizer.UnknownThreshold = trkThreshold.Value;
            recognizer.MinMargin = trkMargin.Value;
        }
        UpdateSensitivityLabels();
        ReanalyzeCurrent();
    }

    private void UpdateSensitivityLabels()
    {
        lblThreshold.Text = $"En fazla mesafe: {trkThreshold.Value}";
        lblMargin.Text = $"Ayırt etme farkı: {trkMargin.Value}";
    }

    /// <summary>Kamera kapalıyken ekrandaki görüntüyü yeni ayarlarla tekrar analiz eder.</summary>
    private void ReanalyzeCurrent()
    {
        if (camera.IsRunning || currentImage is null || analyzer is null)
            return;
        var sw = Stopwatch.StartNew();
        IReadOnlyList<FaceMatch> matches;
        lock (visionLock)
            matches = analyzer.Analyze(currentImage);
        ShowFrame(currentImage.Clone(), matches, sw.Elapsed);
    }

    // ===================== Yüzleri kaydet / eğitim =====================

    private void btnSaveFaces_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Yüzlerin kaydedileceği klasörü seçin", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SaveFacesTo(dialog.SelectedPath);
    }

    internal int SaveFacesTo(string folder)
    {
        if (currentImage is null || currentMatches.Count == 0)
        {
            ShowAlert("Kaydedilecek yüz yok.", error: true);
            return 0;
        }
        try
        {
            var saved = SampleLibrary.SaveCrops(currentImage, currentMatches.Select(m => m.Area), folder);
            ShowAlert($"{saved.Count} yüz kaydedildi: {folder}", error: false);
            return saved.Count;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowAlert("Yüzler kaydedilemedi: " + ex.Message, error: true);
            return 0;
        }
    }

    private void btnTraining_Click(object? sender, EventArgs e)
    {
        if (detector is null) return;
        bool wasRunning = camera.IsRunning;
        StopCamera();

        using (var form = new TrainingForm(library, recognizer, detector, visionLock, currentImage, currentMatches))
            form.ShowDialog(this);

        UpdateModelChip();
        if (wasRunning)
            StartCamera();
        else
            ReanalyzeCurrent();
    }

    // ===================== Ekran yardımcıları =====================

    private void UpdateModelChip()
    {
        chipModel.Text = recognizer.IsTrained
            ? $"Model: {recognizer.Labels.Count} kişi · {recognizer.SampleCount} örnek"
            : "Model eğitilmedi";
        chipModel.Dot = recognizer.IsTrained ? Palette.Accent : Palette.Unknown;
    }

    private void UpdateSourceChip(string? imageName = null)
    {
        if (camera.IsRunning)
        {
            chipSource.Text = "Kamera açık";
            chipSource.Dot = Palette.Danger;
        }
        else if (imageName != null || currentImage != null)
        {
            chipSource.Text = "Görüntü analizi";
            chipSource.Dot = Palette.Accent;
        }
        else
        {
            chipSource.Text = "Kaynak seçilmedi";
            chipSource.Dot = Palette.TextDim;
        }
    }

    private void ShowAlert(string message, bool error)
    {
        lblAlert.Text = (error ? "⚠  " : "✓  ") + message;
        pnlAlert.BackColor = error ? Color.FromArgb(78, 28, 40) : Color.FromArgb(16, 70, 52);
        pnlAlert.Visible = true;
    }

    private void HideAlert() => pnlAlert.Visible = false;

    private void btnAlertClose_Click(object? sender, EventArgs e) => HideAlert();

    private void pnlTop_Paint(object? sender, PaintEventArgs e)
    {
        using var line = new Pen(Palette.Line);
        e.Graphics.DrawLine(line, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        camera.Dispose();
        lock (visionLock)
        {
            recognizer.Dispose();
            detector?.Dispose();
        }
        currentImage?.Dispose();
    }
}
