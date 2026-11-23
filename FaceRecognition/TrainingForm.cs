using FaceRecognition.Data;
using FaceRecognition.Ui;
using FaceRecognition.Vision;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FaceRecognition;

/// <summary>
/// Kişi (etiket) ekleme/silme, örnek fotoğraf ekleme, modeli yeniden eğitme ve kaydetme/yükleme penceresi.
/// </summary>
public partial class TrainingForm : Form
{
    private readonly SampleLibrary library;
    private readonly FaceRecognizerService recognizer;
    private readonly FaceDetector detector;
    private readonly object visionLock;
    private readonly Mat? currentImage;
    private readonly IReadOnlyList<FaceMatch> currentMatches;

    public TrainingForm(SampleLibrary library, FaceRecognizerService recognizer, FaceDetector detector,
                        object visionLock, Mat? currentImage, IReadOnlyList<FaceMatch> currentMatches)
    {
        InitializeComponent();
        this.library = library;
        this.recognizer = recognizer;
        this.detector = detector;
        this.visionLock = visionLock;
        this.currentImage = currentImage;
        this.currentMatches = currentMatches;
    }

    private void TrainingForm_Load(object? sender, EventArgs e)
    {
        RefreshPeople();
        ShowModelInfo();
    }

    private string? SelectedPerson => lstPeople.SelectedItem as string;

    internal void RefreshPeople(string? select = null)
    {
        string? keep = select ?? SelectedPerson;
        lstPeople.BeginUpdate();
        lstPeople.Items.Clear();
        foreach (string label in library.Labels)
            lstPeople.Items.Add(label);
        lstPeople.EndUpdate();

        // NTFS "Ali" ile "ali"yi aynı klasör sayıyor; arama da büyük/küçük harf duyarsız
        int index = keep is null ? -1 : lstPeople.FindStringExact(keep);
        if (index >= 0)
            lstPeople.SelectedIndex = index;
        else if (lstPeople.Items.Count > 0)
            lstPeople.SelectedIndex = 0;
        else
            ShowSamples(null);
    }

    private void lstPeople_SelectedIndexChanged(object? sender, EventArgs e) => ShowSamples(SelectedPerson);

    private void ShowSamples(string? person)
    {
        lvSamples.BeginUpdate();
        lvSamples.Items.Clear();
        foreach (Image img in imgSamples.Images) img.Dispose();
        imgSamples.Images.Clear();

        if (person != null)
        {
            foreach (string file in library.FilesOf(person))
            {
                using var mat = ImageIO.Read(file, ImreadModes.Grayscale);
                if (mat.Empty()) continue;
                using var small = mat.Resize(new OpenCvSharp.Size(80, 80));
                imgSamples.Images.Add(small.ToBitmap());
                lvSamples.Items.Add(new ListViewItem(Path.GetFileName(file), imgSamples.Images.Count - 1));
            }
        }
        lvSamples.EndUpdate();

        lblSelected.Text = person is null ? "Bir kişi seçin" : $"{person}  ·  {lvSamples.Items.Count} örnek";
        btnAddFromFile.Enabled = person != null;
        btnAddFromCurrent.Enabled = person != null && currentImage != null && currentMatches.Count > 0;
        btnDeletePerson.Enabled = person != null;
        lstPeople.Invalidate();
    }

    private void lstPeople_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        string name = lstPeople.Items[e.Index]?.ToString() ?? "";
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (var bg = new SolidBrush(selected ? Palette.PanelLight : Palette.Panel))
            e.Graphics.FillRectangle(bg, e.Bounds);
        if (selected)
            using (var bar = new SolidBrush(Palette.Accent))
                e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Y + 8, 3, e.Bounds.Height - 16);

        using var nameFont = new Font("Segoe UI Semibold", 10F);
        TextRenderer.DrawText(e.Graphics, name, nameFont, new System.Drawing.Point(e.Bounds.X + 12, e.Bounds.Y + 4), Palette.Text);
        TextRenderer.DrawText(e.Graphics, $"{library.CountOf(name)} örnek", Font, new System.Drawing.Point(e.Bounds.X + 12, e.Bounds.Y + 23), Palette.TextDim);
    }

    // ---------- Kişi ekle / sil ----------

    private void btnAddPerson_Click(object? sender, EventArgs e) => AddPerson(txtNewPerson.Text);

    internal bool AddPerson(string label)
    {
        if (SampleLibrary.ValidateLabel(label) is string error)
        {
            Info(error, error: true);
            return false;
        }
        if (library.FindLabel(label) is string existing)
        {
            txtNewPerson.Clear();
            RefreshPeople(existing);
            Info($"\"{existing}\" zaten var, listede seçildi.", error: false);
            return false;
        }
        try
        {
            library.CreateLabel(label);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Info("Kişi klasörü oluşturulamadı: " + ex.Message, error: true);
            return false;
        }
        txtNewPerson.Clear();
        RefreshPeople(label.Trim());
        Info($"\"{label.Trim()}\" eklendi. Şimdi bu kişiye örnek fotoğraf ekleyin.", error: false);
        return true;
    }

    private void btnDeletePerson_Click(object? sender, EventArgs e)
    {
        if (SelectedPerson is not string person) return;
        var answer = MessageBox.Show(this, $"\"{person}\" ve tüm örnekleri silinsin mi?\n(Sadece program klasöründeki kopya silinir.)",
            "Kişiyi sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        try
        {
            library.DeleteLabel(person);
            RefreshPeople();
            Info($"\"{person}\" silindi. Değişikliğin tanımaya yansıması için modeli yeniden eğitin.", error: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Info("Silinemedi: " + ex.Message, error: true);
        }
    }

    // ---------- Örnek ekle ----------

    private void btnAddFromFile_Click(object? sender, EventArgs e)
    {
        if (SelectedPerson is not string person) return;
        using var dialog = new OpenFileDialog
        {
            Title = $"{person} için örnek fotoğraflar",
            Filter = "Resim dosyaları (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddSamplesFromFiles(person, dialog.FileNames);
    }

    /// <summary>Her dosyada yüzü bulup sadece yüz bölgesini örnek olarak kaydeder.</summary>
    internal int AddSamplesFromFiles(string person, IEnumerable<string> files)
    {
        int added = 0, failed = 0;
        foreach (string file in files)
        {
            try
            {
                using var img = ImageIO.Read(file);
                if (img.Empty()) { failed++; continue; }
                Mat face;
                lock (visionLock)
                    face = ImagePreprocessor.CropFace(img, detector);
                using (face)
                    library.AddSample(person, face);
                added++;
            }
            catch (Exception ex) when (ex is VisionException or IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        ShowSamples(person);
        Info(failed == 0
                ? $"{added} örnek eklendi. Modeli yeniden eğitmeyi unutmayın."
                : $"{added} örnek eklendi, {failed} dosya okunamadı.", error: failed > 0 && added == 0);
        return added;
    }

    private void btnAddFromCurrent_Click(object? sender, EventArgs e)
    {
        if (SelectedPerson is not string person || currentImage is null || currentImage.IsDisposed || currentMatches.Count == 0) return;

        // Ekranda birden fazla yüz varsa en büyüğü (kameraya en yakın olan) ekleniyor
        var area = currentMatches.OrderByDescending(m => m.Area.Width * m.Area.Height).First().Area;
        area = area.Intersect(new Rect(0, 0, currentImage.Width, currentImage.Height));
        if (area.Width <= 0 || area.Height <= 0) return;
        try
        {
            using var crop = new Mat(currentImage, area);
            library.AddSample(person, crop);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Info("Örnek kaydedilemedi: " + ex.Message, error: true);
            return;
        }
        ShowSamples(person);
        Info("Ekrandaki yüz örnek olarak eklendi.", error: false);
    }

    // ---------- Eğitim / model ----------

    private void btnRetrain_Click(object? sender, EventArgs e) => Retrain();

    internal bool Retrain()
    {
        Cursor = Cursors.WaitCursor;
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
            ShowModelInfo(skipped);
            return true;
        }
        catch (VisionException ex)
        {
            Info(ex.Message, error: true);
            return false;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void ShowModelInfo(int skipped = 0)
    {
        if (!recognizer.IsTrained)
        {
            Info("Model henüz eğitilmedi.", error: true);
            return;
        }
        Info($"Model: {recognizer.Labels.Count} kişi, {recognizer.SampleCount} örnek  ·  eğitim {recognizer.LastTrainingTime.TotalMilliseconds:0} ms" +
             (skipped > 0 ? $"  ·  {skipped} dosya okunamadı" : ""), error: false);
    }

    private void btnSaveModel_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Modelin kaydedileceği klasör", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SaveModelTo(dialog.SelectedPath);
    }

    internal bool SaveModelTo(string folder)
    {
        try
        {
            lock (visionLock)
                recognizer.Save(folder);
            Info($"Model kaydedildi: {folder}", error: false);
            return true;
        }
        catch (Exception ex) when (ex is VisionException or IOException or UnauthorizedAccessException)
        {
            Info("Model kaydedilemedi: " + ex.Message, error: true);
            return false;
        }
    }

    private void btnLoadModel_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Kaydedilmiş model klasörünü seçin", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadModelFrom(dialog.SelectedPath);
    }

    internal bool LoadModelFrom(string folder)
    {
        try
        {
            lock (visionLock)
                recognizer.Load(folder);
            Info($"Model yüklendi: {recognizer.Labels.Count} kişi ({string.Join(", ", recognizer.Labels)})", error: false);
            return true;
        }
        catch (VisionException ex)
        {
            // Model dosyası yoksa / bozuksa anlaşılır mesaj
            Info(ex.Message, error: true);
            return false;
        }
    }

    private void Info(string text, bool error)
    {
        lblInfo.Text = (error ? "⚠  " : "✓  ") + text;
        lblInfo.ForeColor = error ? Palette.Unknown : Palette.Accent;
    }
}
