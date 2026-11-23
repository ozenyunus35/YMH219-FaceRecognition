using OpenCvSharp;

namespace FaceRecognition.Data;

/// <summary>
/// Yerel örnek kümesi. Her kişi için bir klasör: Ornekler/&lt;etiket&gt;/*.png
/// Klasör adı kişinin etiketi oluyor.
/// </summary>
public sealed class SampleLibrary
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public SampleLibrary(string rootFolder)
    {
        RootFolder = rootFolder;
    }

    public static string DefaultFolder => Path.Combine(AppContext.BaseDirectory, "Ornekler");

    public string RootFolder { get; }

    public IReadOnlyList<string> Labels =>
        Directory.Exists(RootFolder)
            ? Directory.GetDirectories(RootFolder).Select(Path.GetFileName).OfType<string>().OrderBy(n => n).ToList()
            : Array.Empty<string>();

    public IReadOnlyList<string> FilesOf(string label)
    {
        string dir = Path.Combine(RootFolder, label);
        return Directory.Exists(dir)
            ? Directory.GetFiles(dir).Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f).ToList()
            : Array.Empty<string>();
    }

    public int CountOf(string label) => FilesOf(label).Count;

    /// <summary>Tüm örnekleri okur. Okunamayan dosyalar atlanıyor ve sayısı dönülüyor.</summary>
    public (List<(string Label, Mat Face)> Samples, int Skipped) LoadAll()
    {
        var samples = new List<(string, Mat)>();
        int skipped = 0;
        try
        {
            ReadAllInto(samples, ref skipped);
        }
        catch
        {
            foreach (var (_, m) in samples) m.Dispose();
            throw;
        }
        return (samples, skipped);
    }

    private void ReadAllInto(List<(string, Mat)> samples, ref int skipped)
    {
        foreach (string label in Labels)
        {
            foreach (string file in FilesOf(label))
            {
                Mat img;
                try
                {
                    img = Vision.ImageIO.Read(file, ImreadModes.Grayscale);
                }
                catch (Vision.VisionException)
                {
                    skipped++;
                    continue;
                }
                if (img.Empty())
                {
                    img.Dispose();
                    skipped++;
                    continue;
                }
                samples.Add((label, img));
            }
        }
    }

    /// <summary>Büyük/küçük harf farkı gözetmeden var olan etiketi bulur (NTFS "Ali" ile "ali"yi aynı klasör sayar).</summary>
    public string? FindLabel(string label) =>
        Labels.FirstOrDefault(l => string.Equals(l, label.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? ValidateLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "Etiket (kişi adı) boş olamaz.";
        if (label.Trim().Length > 30)
            return "Etiket en fazla 30 karakter olabilir.";
        if (label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Etikette \\ / : * ? \" < > | karakterleri kullanılamaz.";
        string t = label.Trim();
        // "." / ".." üst klasörü gösterir; Windows sondaki noktayı siler ("Ali." -> "Ali")
        if (t.Trim('.').Length == 0 || t.EndsWith('.'))
            return "Etiket nokta ile bitemez ve sadece noktadan oluşamaz.";
        if (ReservedNames.Contains(t.Split('.')[0].Trim()))
            return $"\"{t}\" Windows'ta ayrılmış bir ad, başka bir etiket seçin.";
        return null;
    }

    public void CreateLabel(string label)
    {
        if (ValidateLabel(label) is string error)
            throw new ArgumentException(error);
        Directory.CreateDirectory(Path.Combine(RootFolder, label.Trim()));
    }

    /// <summary>Yüz kesitini 100x100 gri olarak etiketin klasörüne kaydeder, dosya yolunu döner.</summary>
    public string AddSample(string label, Mat face)
    {
        CreateLabel(label);
        string dir = Path.Combine(RootFolder, label.Trim());
        int next = FilesOf(label.Trim()).Count + 1;
        string path;
        do
        {
            path = Path.Combine(dir, $"{next:00}.png");
            next++;
        } while (File.Exists(path));

        using var normalized = Vision.ImagePreprocessor.NormalizeFace(face);
        Vision.ImageIO.Write(path, normalized);
        return path;
    }

    public void DeleteLabel(string label)
    {
        string dir = Path.Combine(RootFolder, label);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    /// <summary>Algılanan yüzleri ayrı bir klasöre kaydeder (isteğe bağlı özellik).</summary>
    public static IReadOnlyList<string> SaveCrops(Mat image, IEnumerable<Rect> faces, string folder)
    {
        Directory.CreateDirectory(folder);
        var saved = new List<string>();
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        int i = 1;
        foreach (var r in faces)
        {
            using var crop = new Mat(image, r);
            string path = Path.Combine(folder, $"yuz-{stamp}-{i++}.png");
            Vision.ImageIO.Write(path, crop);
            saved.Add(path);
        }
        return saved;
    }
}
