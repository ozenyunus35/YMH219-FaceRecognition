using FaceRecognition.Data;
using FaceRecognition.Vision;
using OpenCvSharp;

namespace FaceRecognition.Tests;

/// <summary>Kod incelemesinde bulunan hataların tekrar etmemesi için eklenen testler.</summary>
public class RobustnessTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "saglam-" + Guid.NewGuid().ToString("N"));

    public RobustnessTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private static FaceRecognizerService TwoPeople()
    {
        var r = new FaceRecognizerService();
        using var a1 = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(20));
        using var a2 = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(40));
        using var b1 = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(200));
        using var b2 = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(220));
        r.Train(new[] { ("A", a1), ("A", a2), ("B", b1), ("B", b2) });
        return r;
    }

    [Fact]
    public void ZeroByteImage_ReturnsEmptyMat_InsteadOfThrowing()
    {
        string path = Path.Combine(root, "bos.png");
        File.WriteAllBytes(path, Array.Empty<byte>());
        using var img = ImageIO.Read(path);
        Assert.True(img.Empty());
    }

    [Fact]
    public void LoadAll_SkipsZeroByteFiles()
    {
        var lib = new SampleLibrary(root);
        using var face = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(50));
        lib.AddSample("a", face);
        File.WriteAllBytes(Path.Combine(root, "a", "bos.png"), Array.Empty<byte>());

        var (samples, skipped) = lib.LoadAll();

        Assert.Single(samples);
        Assert.Equal(1, skipped);
        foreach (var s in samples) s.Face.Dispose();
    }

    [Fact]
    public void Confidence_IsFinite_WhenMarginSettingIsZero()
    {
        using var r = new FaceRecognizerService { MinMargin = 0 };
        double c = r.ToConfidence(50, 0);
        Assert.False(double.IsNaN(c));
        Assert.InRange(c, 0, 100);
    }

    [Fact]
    public void Load_EmptyLabelFile_IsRejected_AndKeepsCurrentModel()
    {
        using var r = TwoPeople();
        File.WriteAllText(Path.Combine(root, "etiketler.txt"), "\n  \n");

        Assert.Throws<VisionException>(() => r.Load(root));
        Assert.True(r.IsTrained);
        Assert.Equal(2, r.Labels.Count);
    }

    [Fact]
    public void Load_DuplicateLabels_IsRejected()
    {
        using var r = TwoPeople();
        string dir = Path.Combine(root, "model");
        r.Save(dir);
        File.WriteAllLines(Path.Combine(dir, "etiketler.txt"), new[] { "A", "a" });

        Assert.Throws<VisionException>(() => r.Load(dir));
        Assert.Equal(new[] { "A", "B" }, r.Labels);
    }

    [Fact]
    public void SaveThenLoad_ResetsSampleCount()
    {
        using var r = TwoPeople();
        Assert.Equal(4, r.SampleCount);
        string dir = Path.Combine(root, "model");
        r.Save(dir);

        using var loaded = new FaceRecognizerService();
        loaded.Load(dir);
        Assert.Equal(0, loaded.SampleCount);
        Assert.Equal(new[] { "A", "B" }, loaded.Labels);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("Ali.")]
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("com1.txt")]
    public void DangerousLabels_AreRejected(string label)
    {
        Assert.NotNull(SampleLibrary.ValidateLabel(label));
    }

    [Theory]
    [InlineData("Gönüllü E")]
    [InlineData("Dr. Ayşe")]
    [InlineData("İpek")]
    public void NormalLabels_AreStillAccepted(string label)
    {
        Assert.Null(SampleLibrary.ValidateLabel(label));
    }

    [Fact]
    public void FindLabel_IgnoresCase()
    {
        var lib = new SampleLibrary(root);
        lib.CreateLabel("Ali");
        Assert.Equal("Ali", lib.FindLabel("ali"));
        Assert.Equal("Ali", lib.FindLabel("  ALI "));
        Assert.Null(lib.FindLabel("Veli"));
    }

    [Fact]
    public void Camera_StopWithoutStart_AndDoubleStop_AreSafe()
    {
        using var cam = new CameraService();
        cam.Stop();
        cam.Stop();
        Assert.False(cam.IsRunning);
    }

    [Fact]
    public void Camera_MissingDevice_GivesFriendlyError_AndCanRetry()
    {
        using var cam = new CameraService();
        for (int i = 0; i < 2; i++)
        {
            var ex = Assert.Throws<VisionException>(() => cam.Start(57));
            Assert.Contains("Kamera", ex.Message);
            Assert.False(cam.IsRunning);
        }
    }
}
