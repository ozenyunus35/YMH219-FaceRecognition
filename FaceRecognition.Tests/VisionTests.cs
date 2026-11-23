using FaceRecognition.Data;
using FaceRecognition.Vision;
using OpenCvSharp;
using Xunit.Abstractions;

namespace FaceRecognition.Tests;

public class DetectorTests : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly FaceDetector detector = new(FaceDetector.DefaultCascadePath);
    private static string Deneme(string f) => Path.Combine(AppContext.BaseDirectory, "DenemeGorselleri", f);

    public DetectorTests(ITestOutputHelper output) => this.output = output;
    public void Dispose() => detector.Dispose();

    [Fact]
    public void MissingCascade_GivesFriendlyError()
    {
        var ex = Assert.Throws<VisionException>(() => new FaceDetector(Path.Combine(Path.GetTempPath(), "yok.xml")));
        Assert.Contains("bulunamadı", ex.Message);
    }

    [Fact]
    public void BrokenCascade_GivesFriendlyError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bozuk-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, "<xml>bozuk</xml>");
        try
        {
            Assert.ThrowsAny<Exception>(() => new FaceDetector(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("gonullu-a-1.png")]
    [InlineData("gonullu-a-2.png")]
    [InlineData("gonullu-b-1.png")]
    [InlineData("gonullu-b-2.png")]
    [InlineData("gonullu-c-1.png")]
    [InlineData("gonullu-c-2.png")]
    [InlineData("gonullu-d-1.png")]
    [InlineData("gonullu-d-2.png")]
    [InlineData("taninmayan-kisi.png")]
    public void SinglePortrait_HasExactlyOneFace(string file)
    {
        using var img = ImageIO.Read(Deneme(file));
        var faces = detector.Detect(img);
        output.WriteLine($"{file}: {string.Join(", ", faces)}");
        Assert.Single(faces);
    }

    [Fact]
    public void GroupPhoto_FindsAllFiveFaces()
    {
        using var img = ImageIO.Read(Deneme("grup-fotografi.png"));
        var faces = detector.Detect(img);
        output.WriteLine(string.Join(", ", faces));
        Assert.Equal(5, faces.Length);
    }

    [Fact]
    public void BlankImage_HasNoFaces()
    {
        using var img = new Mat(300, 300, MatType.CV_8UC3, Scalar.All(128));
        Assert.Empty(detector.Detect(img));
    }

    [Fact]
    public void EmptyMat_GivesFriendlyError()
    {
        using var img = new Mat();
        Assert.Throws<VisionException>(() => detector.Detect(img));
    }
}

public class RecognizerTests : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly FaceDetector detector = new(FaceDetector.DefaultCascadePath);
    private readonly FaceRecognizerService recognizer = new();
    private static string Deneme(string f) => Path.Combine(AppContext.BaseDirectory, "DenemeGorselleri", f);

    public RecognizerTests(ITestOutputHelper output)
    {
        this.output = output;
        var (samples, skipped) = new SampleLibrary(SampleLibrary.DefaultFolder).LoadAll();
        recognizer.Train(samples, detector);
        foreach (var s in samples) s.Face.Dispose();
    }

    public void Dispose()
    {
        detector.Dispose();
        recognizer.Dispose();
    }

    [Fact]
    public void Training_UsesAllLabelsAndSamples()
    {
        Assert.Equal(new[] { "Gönüllü A", "Gönüllü B", "Gönüllü C", "Gönüllü D" }, recognizer.Labels);
        Assert.Equal(32, recognizer.SampleCount);
    }

    [Theory]
    [InlineData("gonullu-a-1.png", "Gönüllü A")]
    [InlineData("gonullu-a-2.png", "Gönüllü A")]
    [InlineData("gonullu-b-1.png", "Gönüllü B")]
    [InlineData("gonullu-b-2.png", "Gönüllü B")]
    [InlineData("gonullu-c-1.png", "Gönüllü C")]
    [InlineData("gonullu-c-2.png", "Gönüllü C")]
    [InlineData("gonullu-d-1.png", "Gönüllü D")]
    [InlineData("gonullu-d-2.png", "Gönüllü D")]
    public void UnseenPhotos_AreRecognizedCorrectly(string file, string expected)
    {
        using var img = ImageIO.Read(Deneme(file));
        var analyzer = new FaceAnalyzer(detector, recognizer);

        var match = Assert.Single(analyzer.Analyze(img));
        output.WriteLine($"{file}: {match.Label} güven=%{match.Confidence:0} | " + string.Join(" ", match.Ranking!.Select(r => $"{r.Label[^1]}={r.Distance:0.0}")));

        Assert.Equal(expected, match.Label);
        Assert.True(match.IsKnown);
        Assert.InRange(match.Confidence, 1, 100);
    }

    [Fact]
    public void PersonNotInTraining_IsUnknown()
    {
        using var img = ImageIO.Read(Deneme("taninmayan-kisi.png"));
        var match = Assert.Single(new FaceAnalyzer(detector, recognizer).Analyze(img));
        output.WriteLine($"tanınmayan: {match.Label} | " + string.Join(" ", match.Ranking!.Select(r => $"{r.Label[^1]}={r.Distance:0.0}")));

        Assert.False(match.IsKnown);
        Assert.Equal("Bilinmeyen", match.Label);
    }

    [Fact]
    public void GroupPhoto_FourKnownOneUnknown()
    {
        using var img = ImageIO.Read(Deneme("grup-fotografi.png"));
        var matches = new FaceAnalyzer(detector, recognizer).Analyze(img);
        foreach (var m in matches)
            output.WriteLine($"x={m.Area.X} {m.Label} | " + string.Join(" ", m.Ranking!.Select(r => $"{r.Label[^1]}={r.Distance:0.0}")));

        Assert.Equal(5, matches.Count);
        Assert.Equal(new[] { "Gönüllü A", "Gönüllü B", "Bilinmeyen", "Gönüllü C", "Gönüllü D" }, matches.Select(m => m.Label));
    }

    [Fact]
    public void SaveAndLoad_GivesSamePredictions()
    {
        string path = Path.Combine(Path.GetTempPath(), $"model-{Guid.NewGuid():N}");
        try
        {
            recognizer.Save(path);
            using var loaded = new FaceRecognizerService();
            loaded.Load(path);

            using var img = ImageIO.Read(Deneme("gonullu-b-1.png"));
            var face = detector.Detect(img)[0];
            using var crop = new Mat(img, face);
            Assert.Equal(recognizer.Predict(crop).Label, loaded.Predict(crop).Label);
            Assert.Equal(recognizer.Labels, loaded.Labels);
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }

    [Fact]
    public void LoadingMissingModel_GivesFriendlyError()
    {
        using var r = new FaceRecognizerService();
        var ex = Assert.Throws<VisionException>(() => r.Load(Path.Combine(Path.GetTempPath(), "olmayan-model")));
        Assert.Contains("bulunamadı", ex.Message);
    }

    [Fact]
    public void PredictBeforeTraining_GivesFriendlyError()
    {
        using var r = new FaceRecognizerService();
        using var img = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(100));
        Assert.Throws<VisionException>(() => r.Predict(img));
    }

    [Fact]
    public void TrainingWithOneLabel_IsRejected()
    {
        using var r = new FaceRecognizerService();
        using var a = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(10));
        using var b = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(200));
        var ex = Assert.Throws<VisionException>(() => r.Train(new[] { ("tek", a), ("tek", b) }));
        Assert.Contains("en az 2", ex.Message);
    }

    [Fact]
    public void Confidence_DecreasesWithDistance()
    {
        Assert.Equal(100, recognizer.ToConfidence(0, 100));
        Assert.True(recognizer.ToConfidence(60, 20) > recognizer.ToConfidence(110, 20));
        Assert.True(recognizer.ToConfidence(100, 30) > recognizer.ToConfidence(100, 2));
        Assert.Equal(0, recognizer.ToConfidence(1000, 0));
    }
}

public class SampleLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ornek-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [Fact]
    public void DefaultLibrary_HasFourVolunteersWithEightSamples()
    {
        var lib = new SampleLibrary(SampleLibrary.DefaultFolder);
        Assert.Equal(4, lib.Labels.Count);
        Assert.All(lib.Labels, l => Assert.Equal(8, lib.CountOf(l)));
    }

    [Fact]
    public void AddSample_SavesNormalized100x100Png()
    {
        var lib = new SampleLibrary(root);
        using var face = new Mat(240, 200, MatType.CV_8UC3, Scalar.All(90));

        string path = lib.AddSample("Deneme Kişi", face);

        Assert.True(File.Exists(path));
        using var saved = ImageIO.Read(path, ImreadModes.Unchanged);
        Assert.Equal(100, saved.Width);
        Assert.Equal(100, saved.Height);
        Assert.Equal(1, saved.Channels());
        Assert.Equal(1, lib.CountOf("Deneme Kişi"));
    }

    [Fact]
    public void AddSample_DoesNotOverwriteExisting()
    {
        var lib = new SampleLibrary(root);
        using var face = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(50));

        string a = lib.AddSample("x", face);
        string b = lib.AddSample("x", face);

        Assert.NotEqual(a, b);
        Assert.Equal(2, lib.CountOf("x"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a/b")]
    [InlineData("soru?")]
    public void InvalidLabels_AreRejected(string label)
    {
        Assert.NotNull(SampleLibrary.ValidateLabel(label));
        Assert.Throws<ArgumentException>(() => new SampleLibrary(root).CreateLabel(label));
    }

    [Fact]
    public void LoadAll_SkipsUnreadableFiles()
    {
        var lib = new SampleLibrary(root);
        using var face = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(50));
        lib.AddSample("a", face);
        File.WriteAllText(Path.Combine(root, "a", "bozuk.png"), "resim değil");

        var (samples, skipped) = lib.LoadAll();

        Assert.Single(samples);
        Assert.Equal(1, skipped);
        foreach (var s in samples) s.Face.Dispose();
    }

    [Fact]
    public void SaveCrops_WritesOneFilePerFace()
    {
        using var img = new Mat(200, 300, MatType.CV_8UC3, Scalar.All(120));
        var saved = SampleLibrary.SaveCrops(img, new[] { new Rect(10, 10, 50, 50), new Rect(100, 20, 60, 60) }, root);

        Assert.Equal(2, saved.Count);
        Assert.All(saved, p => Assert.True(File.Exists(p)));
    }

    [Fact]
    public void Normalize_ProducesGray100x100()
    {
        using var color = new Mat(64, 64, MatType.CV_8UC3, Scalar.All(30));
        using var n = ImagePreprocessor.NormalizeFace(color);
        Assert.Equal(new Size(100, 100), n.Size());
        Assert.Equal(1, n.Channels());
    }
}

public class CameraTests
{
    [Fact]
    public void Camera_EitherOpensOrGivesFriendlyError()
    {
        using var cam = new CameraService();
        try
        {
            cam.Start(0);
            Assert.True(cam.IsRunning);
            cam.Stop();
        }
        catch (VisionException ex)
        {
            Assert.Contains("Kamera", ex.Message);
            Assert.False(cam.IsRunning);
        }
    }
}
