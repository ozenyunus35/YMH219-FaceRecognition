using FaceRecognition.Data;
using FaceRecognition.Vision;
using OpenCvSharp;
using Xunit.Abstractions;

namespace FaceRecognition.Tests;

/// <summary>
/// Değerlendirme: modelin eğitimde HİÇ görmediği 36 kişinin 72 fotoğrafı (Olivetti veri seti) ile
/// "yabancı yüzü Bilinmeyen diyebilme" oranı ölçülüyor. Sonuç test çıktısına yazılıyor.
/// </summary>
public class EvaluationTests
{
    private readonly ITestOutputHelper output;
    public EvaluationTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void UnknownPeople_MostlyRejected()
    {
        using var detector = new FaceDetector(FaceDetector.DefaultCascadePath);
        using var recognizer = new FaceRecognizerService();
        var (samples, _) = new SampleLibrary(SampleLibrary.DefaultFolder).LoadAll();
        recognizer.Train(samples, detector);
        foreach (var s in samples) s.Face.Dispose();

        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "DegerlendirmeVerisi"), "*.png");
        int rejected = 0;
        foreach (string f in files)
        {
            using var img = ImageIO.Read(f, ImreadModes.Grayscale);
            using var face = ImagePreprocessor.CropFace(img, detector);
            if (!recognizer.Predict(face).IsKnown)
                rejected++;
        }

        double rate = 100.0 * rejected / files.Length;
        output.WriteLine($"Eğitimde olmayan {files.Length} fotoğrafın {rejected} tanesi 'Bilinmeyen' çıktı (%{rate:0}).");
        Assert.Equal(72, files.Length);
        Assert.True(rate >= 60, $"Yabancı yüz reddetme oranı düştü: %{rate:0}");
    }
}
