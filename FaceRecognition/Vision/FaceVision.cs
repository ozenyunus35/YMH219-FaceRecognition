using OpenCvSharp;
using OpenCvSharp.Face;
// WinForms System.Drawing'i de otomatik ekliyor; Size iki yerde de var, OpenCV'ninkini kullanıyoruz
using Size = OpenCvSharp.Size;

namespace FaceRecognition.Vision;

/// <summary>Kullanıcıya gösterilecek, anlaşılır mesajlı hata (model yok, kamera yok...).</summary>
public sealed class VisionException : Exception
{
    public VisionException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Bir yüz için sonuç: yeri, tahmin edilen etiket ve güven.</summary>
public sealed record FaceMatch(Rect Area, string Label, double Distance, double Confidence, bool IsKnown,
                               string? ClosestLabel = null, IReadOnlyList<(string Label, double Distance)>? Ranking = null);

/// <summary>
/// Görüntü dosyası okuma/yazma. Cv2.ImRead/ImWrite Türkçe karakterli yolları (ör. "Kişi")
/// Türkçe olmayan Windows'ta açamıyor; dosyayı .NET ile okuyup OpenCV'ye bellekten veriyoruz.
/// </summary>
public static class ImageIO
{
    public static Mat Read(string path, ImreadModes mode = ImreadModes.Color)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new VisionException("Görüntü dosyası okunamadı: " + Path.GetFileName(path), ex);
        }
        // Boş (0 bayt) veya bozuk dosyada OpenCV hata fırlatabiliyor; ikisinde de boş Mat dönüyoruz
        if (bytes.Length == 0)
            return new Mat();
        try
        {
            return Cv2.ImDecode(bytes, mode);
        }
        catch (Exception ex) when (ex is OpenCVException or OpenCvSharpException)
        {
            return new Mat();
        }
    }

    public static void Write(string path, Mat image)
    {
        string ext = Path.GetExtension(path);
        Cv2.ImEncode(string.IsNullOrEmpty(ext) ? ".png" : ext, image, out byte[] bytes);
        File.WriteAllBytes(path, bytes);
    }
}

/// <summary>Algılama ve tanıma öncesi görüntüyü aynı şekle getiren yardımcı.</summary>
public static class ImagePreprocessor
{
    public const int FaceSize = 100;

    public static Mat ToGray(Mat image)
    {
        if (image.Empty())
            throw new VisionException("Görüntü boş.");

        var gray = new Mat();
        if (image.Channels() == 1)
            image.CopyTo(gray);
        else if (image.Channels() == 4)
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);
        else
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    /// <summary>Yüzü gri, 100x100 ve histogramı eşitlenmiş hale getirir (ışık farkını azaltmak için).</summary>
    public static Mat NormalizeFace(Mat face)
    {
        using var gray = ToGray(face);
        var resized = new Mat();
        Cv2.Resize(gray, resized, new Size(FaceSize, FaceSize), 0, 0, InterpolationFlags.Area);
        Cv2.EqualizeHist(resized, resized);
        return resized;
    }

    /// <summary>
    /// Örnek fotoğraftaki yüzü bulup sadece o bölgeyi döner; bulunamazsa tüm görüntüyü kullanır.
    /// Böylece eğitim ve tanıma aynı şekilde kırpılmış yüzlerle yapılıyor (aksi halde mesafeler çok büyüyordu).
    /// Yüzü sıkıca kırpılmış fotoğraflarda da algılama çalışsın diye kenarlar genişletiliyor.
    /// </summary>
    public static Mat CropFace(Mat image, FaceDetector detector)
    {
        int padX = image.Width * 2 / 5, padY = image.Height * 2 / 5;
        using var padded = new Mat();
        Cv2.CopyMakeBorder(image, padded, padY, padY, padX, padX, BorderTypes.Replicate);

        var faces = detector.Detect(padded, minFaceSize: Math.Max(24, Math.Min(image.Width, image.Height) / 3));
        if (faces.Length == 0)
            return image.Clone();

        Rect best = faces.OrderByDescending(f => f.Width * f.Height).First();
        Rect inPadded = best.Intersect(new Rect(0, 0, padded.Width, padded.Height));
        return new Mat(padded, inPadded).Clone();
    }
}

/// <summary>Haar cascade ile görüntüdeki yüzleri bulur.</summary>
public sealed class FaceDetector : IDisposable
{
    private readonly CascadeClassifier cascade;

    public FaceDetector(string cascadePath)
    {
        if (!File.Exists(cascadePath))
            throw new VisionException($"Yüz algılama modeli bulunamadı: {Path.GetFileName(cascadePath)}. " +
                                      "Assets klasöründeki haarcascade_frontalface_default.xml dosyası silinmiş olabilir.");

        cascade = new CascadeClassifier(cascadePath);
        if (cascade.Empty())
            throw new VisionException("Yüz algılama modeli okunamadı (dosya bozuk olabilir).");
    }

    public static string DefaultCascadePath => Path.Combine(AppContext.BaseDirectory, "Assets", "haarcascade_frontalface_default.xml");

    public Rect[] Detect(Mat image, int minFaceSize = 40)
    {
        using var gray = ImagePreprocessor.ToGray(image);
        Cv2.EqualizeHist(gray, gray);
        return cascade.DetectMultiScale(gray, scaleFactor: 1.05, minNeighbors: 4,
                                        flags: HaarDetectionTypes.ScaleImage,
                                        minSize: new Size(minFaceSize, minFaceSize))
                      .OrderBy(r => r.X)
                      .ToArray();
    }

    public void Dispose() => cascade.Dispose();
}

/// <summary>Bir yüz için tahmin sonucu.</summary>
public sealed record Prediction(string Label, double Distance, double Confidence, bool IsKnown,
                                string Closest, IReadOnlyList<(string Label, double Distance)> Ranking);

/// <summary>
/// LBPH (Local Binary Patterns Histograms) ile yüz tanıma.
/// Her kişi için ayrı bir LBPH modeli tutuluyor; böylece yüzün HER kişiye olan mesafesi biliniyor.
/// Karar iki koşula bakıyor:
///   1) en yakın kişiye mesafe eşikten küçük olmalı,
///   2) en yakın ile ikinci en yakın arasında yeterli fark olmalı (yabancı yüzler herkese benzer uzaklıkta kalıyor).
/// </summary>
public sealed class FaceRecognizerService : IDisposable
{
    private readonly Dictionary<string, LBPHFaceRecognizer> models = new();
    private readonly List<string> labels = new();

    /// <summary>En yakın kişiye mesafe bundan büyükse "Bilinmeyen".</summary>
    public double UnknownThreshold { get; set; } = 125;

    /// <summary>En yakın ile ikinci en yakın arasındaki fark bundan küçükse "Bilinmeyen".</summary>
    public double MinMargin { get; set; } = 6;

    public bool IsTrained => models.Count > 0;
    public IReadOnlyList<string> Labels => labels;
    public int SampleCount { get; private set; }
    public TimeSpan LastTrainingTime { get; private set; }

    /// <summary>Örnek fotoğraflardaki yüzleri bulup kırparak eğitir.</summary>
    public void Train(IEnumerable<(string Label, Mat Image)> samples, FaceDetector detector)
    {
        var crops = new List<(string Label, Mat Face)>();
        try
        {
            // Döngü yarıda hata verirse o ana kadar kırpılanlar da finally'de serbest bırakılıyor
            foreach (var s in samples)
                crops.Add((s.Label, ImagePreprocessor.CropFace(s.Image, detector)));
            Train(crops);
        }
        finally
        {
            foreach (var c in crops) c.Item2.Dispose();
        }
    }

    public void Train(IEnumerable<(string Label, Mat Face)> samples)
    {
        var groups = samples.GroupBy(s => s.Label).OrderBy(g => g.Key).ToList();
        if (groups.Count < 2)
            throw new VisionException("Tanıma için en az 2 farklı kişiye (etikete) ait örnek gerekli.");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var yeni = new Dictionary<string, LBPHFaceRecognizer>();
        int count = 0;
        try
        {
            foreach (var g in groups)
            {
                var faces = g.Select(s => ImagePreprocessor.NormalizeFace(s.Face)).ToList();
                try
                {
                    var model = LBPHFaceRecognizer.Create(radius: 1, neighbors: 8, gridX: 8, gridY: 8);
                    model.Train(faces, Enumerable.Repeat(0, faces.Count));
                    yeni[g.Key] = model;
                    count += faces.Count;
                }
                finally
                {
                    foreach (var f in faces) f.Dispose();
                }
            }
        }
        catch
        {
            foreach (var m in yeni.Values) m.Dispose();
            throw;
        }

        DisposeModels();
        foreach (var (k, v) in yeni) models[k] = v;
        labels.AddRange(yeni.Keys);
        SampleCount = count;
        LastTrainingTime = sw.Elapsed;
    }

    public Prediction Predict(Mat face)
    {
        if (!IsTrained)
            throw new VisionException("Tanıma modeli henüz eğitilmedi.");

        using var normalized = ImagePreprocessor.NormalizeFace(face);
        var ranking = new List<(string Label, double Distance)>();
        foreach (string label in labels)
        {
            models[label].Predict(normalized, out _, out double d);
            ranking.Add((label, d));
        }
        ranking.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        var (closest, best) = ranking[0];
        double second = ranking.Count > 1 ? ranking[1].Distance : double.MaxValue;
        double margin = second - best;
        bool known = best <= UnknownThreshold && margin >= MinMargin;

        return new Prediction(known ? closest : "Bilinmeyen", best, ToConfidence(best, margin), known, closest, ranking);
    }

    /// <summary>
    /// 0-100 arası güven: mesafe küçüldükçe ve ikinci adaya fark arttıkça yükselir.
    /// </summary>
    public double ToConfidence(double distance, double margin = 20)
    {
        double distanceScore = Math.Clamp(1.0 - distance / (UnknownThreshold * 1.25), 0, 1);
        // Fark ayarı 0'a çekilince 0/0 = NaN çıkmasın diye ölçek en az 1 alınıyor
        double marginScore = Math.Clamp(margin / (Math.Max(MinMargin, 1) * 4), 0, 1);
        return Math.Round(100 * (0.5 * distanceScore + 0.5 * marginScore), 1);
    }

    /// <summary>Modeli bir klasöre kaydeder (her kişi için bir .yml + etiket listesi).</summary>
    public void Save(string folder)
    {
        if (!IsTrained)
            throw new VisionException("Kaydedilecek eğitilmiş model yok.");
        Directory.CreateDirectory(folder);
        for (int i = 0; i < labels.Count; i++)
            models[labels[i]].Write(Path.Combine(folder, $"kisi{i}.yml"));
        File.WriteAllLines(Path.Combine(folder, "etiketler.txt"), labels);
    }

    public void Load(string folder)
    {
        string labelFile = Path.Combine(folder, "etiketler.txt");
        if (!File.Exists(labelFile))
            throw new VisionException($"Model dosyası bulunamadı: {Path.GetFileName(folder)}");

        List<string> names;
        try
        {
            names = File.ReadAllLines(labelFile).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new VisionException("Etiket dosyası okunamadı: " + ex.Message, ex);
        }
        // Boş ya da tekrar eden etiketler mevcut modeli bozmasın diye yükleme başlamadan reddediliyor
        if (names.Count < 2)
            throw new VisionException("Model dosyasında en az 2 kişi olmalı (etiketler.txt boş veya eksik).");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
            throw new VisionException("etiketler.txt içinde aynı kişi birden fazla kez geçiyor.");

        var yeni = new Dictionary<string, LBPHFaceRecognizer>();
        try
        {
            for (int i = 0; i < names.Count; i++)
            {
                string yml = Path.Combine(folder, $"kisi{i}.yml");
                if (!File.Exists(yml))
                    throw new VisionException($"Model dosyası eksik: kisi{i}.yml");
                var m = LBPHFaceRecognizer.Create();
                m.Read(yml);
                yeni[names[i]] = m;
            }
        }
        catch (Exception ex) when (ex is OpenCVException or OpenCvSharpException or IOException or UnauthorizedAccessException)
        {
            foreach (var m in yeni.Values) m.Dispose();
            throw new VisionException("Model dosyası okunamadı (bozuk olabilir).", ex);
        }
        catch
        {
            foreach (var m in yeni.Values) m.Dispose();
            throw;
        }

        DisposeModels();
        foreach (var (k, v) in yeni) models[k] = v;
        labels.AddRange(names);
        // Dosyadan yüklenen modelde örnek sayısı bilinmiyor; eski eğitimin sayıları kalmasın
        SampleCount = 0;
        LastTrainingTime = TimeSpan.Zero;
    }

    private void DisposeModels()
    {
        foreach (var m in models.Values) m.Dispose();
        models.Clear();
        labels.Clear();
    }

    public void Dispose() => DisposeModels();
}

/// <summary>Algılama + tanımayı birleştiren sınıf: bir görüntü verilir, her yüz için sonuç döner.</summary>
public sealed class FaceAnalyzer
{
    private readonly FaceDetector detector;
    private readonly FaceRecognizerService recognizer;

    public FaceAnalyzer(FaceDetector detector, FaceRecognizerService recognizer)
    {
        this.detector = detector;
        this.recognizer = recognizer;
    }

    public IReadOnlyList<FaceMatch> Analyze(Mat image)
    {
        var results = new List<FaceMatch>();
        foreach (Rect area in detector.Detect(image))
        {
            if (!recognizer.IsTrained)
            {
                results.Add(new FaceMatch(area, "Model yok", double.NaN, 0, false));
                continue;
            }

            using var face = new Mat(image, area);
            var p = recognizer.Predict(face);
            results.Add(new FaceMatch(area, p.Label, p.Distance, p.Confidence, p.IsKnown, p.Closest, p.Ranking));
        }
        return results;
    }
}
