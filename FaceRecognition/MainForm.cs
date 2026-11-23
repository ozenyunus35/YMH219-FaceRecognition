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
    // Kamera her başlatılıp durdurulduğunda artar. Kuyrukta bekleyen eski bir kare,
    // kamera durduktan sonra ekrandaki resmi (ve eğitim penceresine verilen Mat'i) ezmesin diye.
    private volatile int cameraSession;

    public MainForm()
    {
        InitializeComponent();
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch (Exception) { /* varsayılan ikon */ }

        camera.FrameReady += Camera_FrameReady;
        camera.Failed += (_, msg) =>
        {
            try
            {
                BeginInvoke(() =>
                {
                    StopCamera();
                    ShowAlert(msg, error: true);
                });
            }
            catch (InvalidOperationException) { /* form kapanıyor */ }
        };

        toolTip.SetToolTip(trkThreshold, "Yüz, en yakın kişiye bu mesafeden daha uzaksa \"Bilinmeyen\" sayılır. Küçük değer = daha seçici.\nÇift tıkla: varsayılan (125)");
        toolTip.SetToolTip(trkMargin, "En yakın iki kişi arasındaki fark bundan küçükse \"Bilinmeyen\" sayılır. Büyük değer = daha seçici.\nÇift tıkla: varsayılan (6)");
        toolTip.SetToolTip(btnCamera, "Kamerayı başlat / durdur");
        toolTip.SetToolTip(btnImage, "Bilgisayardan bir fotoğraf seç");
        toolTip.SetToolTip(btnSample, "Hazır grup fotoğrafıyla hemen dene");
        toolTip.SetToolTip(btnTraining, "Kişi ekle/sil, örnek fotoğraf ekle, modeli eğit");
        toolTip.SetToolTip(lstResults, "Bir satıra tıklayınca o yüz resimde vurgulanır");

        // Listeden seçilen yüz resimde, resimde tıklanan yüz listede vurgulanıyor
        lstResults.EmptyText = "Henüz yüz yok.\nBir görüntü seçin veya kamerayı başlatın.";
        lstResults.SelectedIndexChanged += (_, _) => picView.HighlightIndex = lstResults.SelectedIndex;
        picView.FaceClicked += (_, i) => { if (i < lstResults.Items.Count) lstResults.SelectedIndex = i; };

        // Kaydırıcı sürüklenirken her adımda analiz yapılıp arayüz takılmasın: kısa bir gecikmeyle bir kez
        sensitivityDelay.Tick += (_, _) => { sensitivityDelay.Stop(); ReanalyzeCurrent(); };
        trkThreshold.DoubleClick += (_, _) => trkThreshold.Value = 125;
        trkMargin.DoubleClick += (_, _) => trkMargin.Value = 6;

        alertTimer.Tick += (_, _) => { alertTimer.Stop(); HideAlert(); };
        lblAlert.AutoEllipsis = true;

        // Kartın genişliği ölçeklemeyle değişse de "Eğitim" butonu kartın dışına taşmasın
        pnlActions.Resize += (_, _) => YerlesimiAyarla();
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
        toolTip.SetToolTip(btnSaveFaces, "Ekrandaki yüzleri ayrı ayrı .png dosyası olarak bir klasöre kaydeder");
    }

    private readonly System.Windows.Forms.Timer sensitivityDelay = new() { Interval = 150 };
    private readonly System.Windows.Forms.Timer alertTimer = new() { Interval = 6000 };
    private readonly Stopwatch listUpdate = Stopwatch.StartNew();

    private void YerlesimiAyarla()
    {
        int gap = 8, w = (pnlActions.ClientSize.Width - gap) / 2;
        btnSaveFaces.SetBounds(0, btnSaveFaces.Top, w, btnSaveFaces.Height);
        btnTraining.SetBounds(w + gap, btnTraining.Top, pnlActions.ClientSize.Width - w - gap, btnTraining.Height);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && pnlAlert.Visible)
        {
            HideAlert();
            e.Handled = true;
        }
    }

    // Koyu temaya uygun koyu başlık çubuğu (Windows 10 20H1+ / 11)
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    internal static void DarkTitleBar(Form f)
    {
        try
        {
            int on = 1;
            DwmSetWindowAttribute(f.Handle, 20, ref on, sizeof(int));
        }
        catch (Exception) { /* eski Windows: varsayılan başlık */ }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkTitleBar(this);
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
        lstResults.ClearSelected();      // yeni görüntüde önceki seçim anlamsız
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
        // Kamera açılması birkaç saniye sürebiliyor: buton kapatılıp kullanıcıya söyleniyor (çift tıklama da engelleniyor)
        btnCamera.Enabled = false;
        lblSourceInfo.Text = "Kamera açılıyor…";
        UseWaitCursor = true;
        Refresh();
        try
        {
            camera.Start(0);
        }
        catch (VisionException ex)
        {
            ShowAlert(ex.Message, error: true);
            lblSourceInfo.Text = currentImage is null ? "" : lblSourceInfo.Text;
            UpdateSourceChip();
            return false;
        }
        finally
        {
            btnCamera.Enabled = true;
            UseWaitCursor = false;
        }

        cameraSession++;
        HideAlert();
        btnCamera.Text = "Kamerayı durdur";
        btnCamera.Kind = ScanButtonKind.Danger;
        lblSourceInfo.Text = "Canlı kamera görüntüsü";
        UpdateSourceChip();
        return true;
    }

    private void StopCamera()
    {
        // Kamera kendiliğinden kopmuş olsa bile (IsRunning false) cihaz bırakılıp arayüz sıfırlanıyor
        cameraSession++;
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
        int session = cameraSession;
        IReadOnlyList<FaceMatch> matches;
        TimeSpan elapsed;
        try
        {
            var sw = Stopwatch.StartNew();
            lock (visionLock)
                matches = analyzer.Analyze(frame);
            elapsed = sw.Elapsed;
        }
        catch
        {
            frame.Dispose();
            frameBusy = false;
            throw;               // CameraService bunu yakalayıp Failed olayıyla bildiriyor
        }

        try
        {
            BeginInvoke(() =>
            {
                try
                {
                    // Kamera bu arada durdurulduysa (görüntü seçildi, eğitim açıldı, form kapanıyor) kare atılıyor
                    if (session != cameraSession || IsDisposed || !camera.IsRunning)
                        frame.Dispose();
                    else
                        ShowFrame(frame, matches, elapsed);
                }
                finally
                {
                    frameBusy = false;
                }
            });
        }
        catch (InvalidOperationException)
        {
            frame.Dispose();     // form kapanıyor
            frameBusy = false;
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

        // Kamera açıkken liste saniyede 4 kezden fazla yenilenmesin (titreme ve kaydırma sıçraması olmasın)
        bool updateList = !camera.IsRunning || listUpdate.ElapsedMilliseconds > 250 || lstResults.Items.Count != matches.Count;
        if (updateList)
        {
            listUpdate.Restart();
            UpdateResultList(image, matches);
        }

        int known = matches.Count(m => m.IsKnown);
        lblStatus.Text = matches.Count == 0
            ? $"Yüz bulunamadı  ·  {image.Width}x{image.Height}  ·  {elapsed.TotalMilliseconds:0} ms"
            : $"{matches.Count} yüz bulundu  ·  {known} tanındı  ·  {matches.Count - known} bilinmeyen  ·  {elapsed.TotalMilliseconds:0} ms";
        btnSaveFaces.Enabled = matches.Count > 0;
    }

    private void UpdateResultList(Mat image, IReadOnlyList<FaceMatch> matches)
    {
        lstResults.SetItems(matches.Select(m =>
        {
            using var crop = new Mat(image, m.Area);
            using var small = crop.Resize(new OpenCvSharp.Size(88, 88));
            return new FaceResultList.Item(small.ToBitmap(), m);
        }).ToList());
        picView.HighlightIndex = lstResults.SelectedIndex;
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
        sensitivityDelay.Stop();
        sensitivityDelay.Start();
    }

    private void UpdateSensitivityLabels()
    {
        lblThreshold.Text = $"En fazla mesafe: {trkThreshold.Value}" + (trkThreshold.Value == 125 ? "  (varsayılan)" : "");
        lblMargin.Text = $"Ayırt etme farkı: {trkMargin.Value}" + (trkMargin.Value == 6 ? "  (varsayılan)" : "");
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

        bool needsRetrain;
        using (var form = new TrainingForm(library, recognizer, detector, visionLock, currentImage, currentMatches))
        {
            form.ShowDialog(this);
            needsRetrain = form.NeedsRetrain;
        }

        UpdateModelChip();
        if (needsRetrain)
        {
            // Örnekler değişti ama model yeniden eğitilmedi: ekrandaki sonuçlar eski modele göre
            chipModel.Text = "Model: yeniden eğitim gerekli";
            chipModel.Dot = Palette.Unknown;
        }
        if (wasRunning)
            StartCamera();
        else
            ReanalyzeCurrent();
    }

    // ===================== Ekran yardımcıları =====================

    private void UpdateModelChip()
    {
        chipModel.Text = !recognizer.IsTrained ? "Model eğitilmedi"
            : recognizer.SampleCount > 0 ? $"Model: {recognizer.Labels.Count} kişi · {recognizer.SampleCount} örnek"
            : $"Model: {recognizer.Labels.Count} kişi · dosyadan";
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
        toolTip.SetToolTip(lblAlert, message);
        pnlAlert.BackColor = error ? Color.FromArgb(78, 28, 40) : Color.FromArgb(16, 70, 52);
        pnlAlert.Visible = true;
        // Başarı mesajları kendiliğinden kapanıyor, hatalar kullanıcı kapatana (✕ veya Esc) kadar kalıyor
        alertTimer.Stop();
        if (!error)
            alertTimer.Start();
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
        sensitivityDelay.Dispose();
        alertTimer.Dispose();
        cameraSession++;
        camera.Dispose();
        lock (visionLock)
        {
            recognizer.Dispose();
            detector?.Dispose();
        }
        currentImage?.Dispose();
    }
}
