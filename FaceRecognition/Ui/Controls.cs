using System.ComponentModel;
using System.Drawing.Drawing2D;
using FaceRecognition.Vision;

namespace FaceRecognition.Ui;

/// <summary>Uygulamanın renkleri: koyu "tarayıcı" teması.</summary>
public static class Palette
{
    public static readonly Color Background = Color.FromArgb(12, 18, 24);
    public static readonly Color Panel = Color.FromArgb(19, 28, 36);
    public static readonly Color PanelLight = Color.FromArgb(28, 40, 50);
    public static readonly Color Line = Color.FromArgb(44, 60, 72);
    public static readonly Color Text = Color.FromArgb(226, 236, 240);
    public static readonly Color TextDim = Color.FromArgb(128, 150, 162);
    public static readonly Color Accent = Color.FromArgb(16, 199, 140);      // tanınan yüz
    public static readonly Color AccentDark = Color.FromArgb(10, 120, 86);
    public static readonly Color Unknown = Color.FromArgb(245, 166, 35);     // bilinmeyen yüz
    public static readonly Color Danger = Color.FromArgb(236, 72, 96);

    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        float d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

public enum ScanButtonKind
{
    Primary,
    Secondary,
    Danger
}

/// <summary>Koyu temaya uygun, köşeleri yuvarlak buton.</summary>
public class ScanButton : Button
{
    private ScanButtonKind kind = ScanButtonKind.Secondary;
    private bool hover, down;

    public ScanButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI Semibold", 9.5F);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    [Category("Appearance")]
    [DefaultValue(ScanButtonKind.Secondary)]
    public ScanButtonKind Kind { get => kind; set { kind = value; Invalidate(); } }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Palette.Panel);

        Color fill, text, border;
        switch (kind)
        {
            case ScanButtonKind.Primary: fill = Palette.Accent; text = Color.FromArgb(6, 30, 22); border = fill; break;
            case ScanButtonKind.Danger: fill = Palette.Danger; text = Color.White; border = fill; break;
            default: fill = Palette.PanelLight; text = Palette.Text; border = Palette.Line; break;
        }
        if (!Enabled) { fill = Color.FromArgb(24, 34, 42); text = Color.FromArgb(80, 100, 110); border = Palette.Line; }
        else if (down) fill = ControlPaint.Dark(fill, 0.1f);
        else if (hover) fill = ControlPaint.Light(fill, 0.15f);

        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var path = Palette.Rounded(r, 7))
        using (var b = new SolidBrush(fill))
        using (var p = new Pen(border))
        {
            g.FillPath(b, path);
            g.DrawPath(p, path);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>Başlıklı koyu kart (sağ panelde bölümler).</summary>
public class ScanCard : Panel
{
    private string title = "";
    private readonly Font titleFont = new("Segoe UI Semibold", 8.5F);

    public ScanCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Palette.Panel;
        Padding = new Padding(14, 34, 14, 12);
    }

    [Category("Appearance")]
    [DefaultValue("")]
    public string Title { get => title; set { title = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Palette.Background);
        using (var path = Palette.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10))
        using (var b = new SolidBrush(BackColor))
        using (var p = new Pen(Palette.Line))
        {
            g.FillPath(b, path);
            g.DrawPath(p, path);
        }
        if (title.Length > 0)
            TextRenderer.DrawText(g, title.ToUpper(new System.Globalization.CultureInfo("tr-TR")), titleFont,
                new Point(14, 12), Palette.TextDim);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) titleFont.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>Küçük durum rozeti (noktalı).</summary>
public class StatusChip : Control
{
    private Color dot = Palette.TextDim;
    private readonly Font font = new("Segoe UI Semibold", 9F);

    public StatusChip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Dot { get => dot; set { dot = value; Invalidate(); } }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Palette.Background);
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var path = Palette.Rounded(r, r.Height / 2))
        using (var b = new SolidBrush(Palette.PanelLight))
        using (var p = new Pen(Color.FromArgb(90, dot)))
        {
            g.FillPath(b, path);
            g.DrawPath(p, path);
        }
        using (var d = new SolidBrush(dot))
            g.FillEllipse(d, 12, Height / 2f - 4, 8, 8);
        TextRenderer.DrawText(g, Text, font, new Rectangle(26, 0, Width - 30, Height), Palette.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) font.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// Görüntüyü gösteren PictureBox. Algılanan yüzleri köşe çerçeveleri ve etiketlerle üzerine çiziyor.
/// SizeMode = Zoom; çizim sırasında yüz koordinatları ekrandaki ölçeğe çevriliyor.
/// </summary>
public class AnalysisPictureBox : PictureBox
{
    private IReadOnlyList<FaceMatch> matches = Array.Empty<FaceMatch>();
    private readonly Font labelFont = new("Segoe UI Semibold", 9.5F);
    private readonly Font emptyFont = new("Segoe UI", 11F);
    private readonly Font emptySmall = new("Segoe UI", 9F);

    public AnalysisPictureBox()
    {
        SizeMode = PictureBoxSizeMode.Zoom;
        BackColor = Palette.Background;
        DoubleBuffered = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<FaceMatch> Matches
    {
        get => matches;
        set { matches = value ?? Array.Empty<FaceMatch>(); Invalidate(); }
    }

    /// <summary>Görüntünün kontrol içinde çizildiği alan (Zoom modunda kenarlarda boşluk kalıyor).</summary>
    private RectangleF ImageRect()
    {
        if (Image is null) return RectangleF.Empty;
        float scale = Math.Min(Width / (float)Image.Width, Height / (float)Image.Height);
        float w = Image.Width * scale, h = Image.Height * scale;
        return new RectangleF((Width - w) / 2, (Height - h) / 2, w, h);
    }

    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        Graphics g = pe.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (Image is null)
        {
            DrawEmptyState(g);
            return;
        }

        RectangleF ir = ImageRect();
        float scale = ir.Width / Image.Width;

        foreach (var m in matches)
        {
            var r = new RectangleF(ir.X + m.Area.X * scale, ir.Y + m.Area.Y * scale, m.Area.Width * scale, m.Area.Height * scale);
            Color c = m.IsKnown ? Palette.Accent : Palette.Unknown;
            DrawBrackets(g, r, c);

            string text = m.IsKnown ? $"{m.Label}  ·  %{m.Confidence:0}" : m.Label;
            Size ts = TextRenderer.MeasureText(text, labelFont);
            var tag = new RectangleF(r.X, Math.Max(ir.Y, r.Y - ts.Height - 10), ts.Width + 14, ts.Height + 6);
            using (var path = Palette.Rounded(tag, 5))
            using (var b = new SolidBrush(Color.FromArgb(225, c)))
                g.FillPath(b, path);
            TextRenderer.DrawText(g, text, labelFont, Rectangle.Round(tag), Color.FromArgb(10, 20, 16),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (m.IsKnown)
            {
                // güven çubuğu (çerçevenin altında)
                var bar = new RectangleF(r.X, r.Bottom + 6, r.Width, 4);
                using var bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                using var fg = new SolidBrush(c);
                g.FillRectangle(bg, bar);
                g.FillRectangle(fg, bar.X, bar.Y, bar.Width * (float)(m.Confidence / 100.0), bar.Height);
            }
        }
    }

    private static void DrawBrackets(Graphics g, RectangleF r, Color c)
    {
        float len = Math.Max(10, Math.Min(r.Width, r.Height) * 0.22f);
        using var pen = new Pen(c, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var thin = new Pen(Color.FromArgb(70, c), 1);
        g.DrawRectangle(thin, r.X, r.Y, r.Width, r.Height);
        // dört köşe
        g.DrawLines(pen, new[] { new PointF(r.X, r.Y + len), new PointF(r.X, r.Y), new PointF(r.X + len, r.Y) });
        g.DrawLines(pen, new[] { new PointF(r.Right - len, r.Y), new PointF(r.Right, r.Y), new PointF(r.Right, r.Y + len) });
        g.DrawLines(pen, new[] { new PointF(r.X, r.Bottom - len), new PointF(r.X, r.Bottom), new PointF(r.X + len, r.Bottom) });
        g.DrawLines(pen, new[] { new PointF(r.Right - len, r.Bottom), new PointF(r.Right, r.Bottom), new PointF(r.Right, r.Bottom - len) });
    }

    private void DrawEmptyState(Graphics g)
    {
        float cx = Width / 2f, cy = Height / 2f - 30;
        using (var pen = new Pen(Palette.Line, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            var r = new RectangleF(cx - 50, cy - 50, 100, 100);
            float len = 26;
            g.DrawLines(pen, new[] { new PointF(r.X, r.Y + len), new PointF(r.X, r.Y), new PointF(r.X + len, r.Y) });
            g.DrawLines(pen, new[] { new PointF(r.Right - len, r.Y), new PointF(r.Right, r.Y), new PointF(r.Right, r.Y + len) });
            g.DrawLines(pen, new[] { new PointF(r.X, r.Bottom - len), new PointF(r.X, r.Bottom), new PointF(r.X + len, r.Bottom) });
            g.DrawLines(pen, new[] { new PointF(r.Right - len, r.Bottom), new PointF(r.Right, r.Bottom), new PointF(r.Right, r.Bottom - len) });
            g.DrawEllipse(pen, cx - 22, cy - 30, 44, 50);
        }
        var rect = new Rectangle(0, (int)cy + 70, Width, 26);
        TextRenderer.DrawText(g, "Kamerayı başlatın veya bir görüntü seçin", emptyFont, rect, Palette.Text, TextFormatFlags.HorizontalCenter);
        rect.Offset(0, 26);
        TextRenderer.DrawText(g, "Sağdaki \"Örnek grup fotoğrafı\" ile hemen deneyebilirsiniz", emptySmall, rect, Palette.TextDim, TextFormatFlags.HorizontalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            labelFont.Dispose();
            emptyFont.Dispose();
            emptySmall.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Bulunan yüzlerin küçük resimli listesi.</summary>
public class FaceResultList : ListBox
{
    private readonly Font nameFont = new("Segoe UI Semibold", 9.5F);
    private readonly Font smallFont = new("Segoe UI", 8F);

    public sealed record Item(Bitmap Thumbnail, FaceMatch Match);

    public FaceResultList()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 54;
        BorderStyle = BorderStyle.None;
        BackColor = Palette.Panel;
        IntegralHeight = false;
        SelectionMode = SelectionMode.None;
    }

    public void SetItems(IEnumerable<Item> items)
    {
        foreach (var old in Items.OfType<Item>())
            old.Thumbnail.Dispose();
        BeginUpdate();
        Items.Clear();
        foreach (var i in items)
            Items.Add(i);
        EndUpdate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not Item item)
            return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var bg = new SolidBrush(BackColor))
            g.FillRectangle(bg, e.Bounds);

        var m = item.Match;
        Color c = m.IsKnown ? Palette.Accent : Palette.Unknown;
        var thumb = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 5, 44, 44);
        g.DrawImage(item.Thumbnail, thumb);
        using (var p = new Pen(c, 2))
            g.DrawRectangle(p, thumb);

        int x = thumb.Right + 10;
        TextRenderer.DrawText(g, m.Label, nameFont, new Point(x, e.Bounds.Y + 6), m.IsKnown ? Palette.Text : Palette.Unknown);

        string detail = double.IsNaN(m.Distance) ? "model eğitilmedi"
            : m.IsKnown ? $"güven %{m.Confidence:0}  ·  mesafe {m.Distance:0}"
            : $"en yakın: {m.ClosestLabel}  ·  mesafe {m.Distance:0}";
        TextRenderer.DrawText(g, detail, smallFont, new Point(x, e.Bounds.Y + 25), Palette.TextDim);

        var bar = new Rectangle(x, e.Bounds.Y + 42, e.Bounds.Width - x - 8, 4);
        using (var b1 = new SolidBrush(Palette.PanelLight)) g.FillRectangle(b1, bar);
        using (var b2 = new SolidBrush(c)) g.FillRectangle(b2, bar.X, bar.Y, (int)(bar.Width * m.Confidence / 100.0), bar.Height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var old in Items.OfType<Item>()) old.Thumbnail.Dispose();
            nameFont.Dispose();
            smallFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
