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
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

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
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        // Tab ile gelindiğinde odak çerçevesi
        if (Focused && ShowFocusCues)
        {
            using var focus = new Pen(Palette.Text, 1.5f) { DashStyle = DashStyle.Dot };
            using var inner = Palette.Rounded(RectangleF.Inflate(r, -3, -3), 5);
            g.DrawPath(focus, inner);
        }
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
        set
        {
            matches = value ?? Array.Empty<FaceMatch>();
            if (highlightIndex >= matches.Count) highlightIndex = -1;
            Invalidate();
        }
    }

    private int highlightIndex = -1;

    /// <summary>Listede seçilen yüz; resimde kalın çerçeveyle gösteriliyor.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HighlightIndex
    {
        get => highlightIndex;
        set { highlightIndex = value; Invalidate(); }
    }

    /// <summary>Resimdeki bir yüze tıklandı (indeks).</summary>
    public event EventHandler<int>? FaceClicked;

    private RectangleF FaceRect(FaceMatch m, RectangleF ir, float scale) =>
        new(ir.X + m.Area.X * scale, ir.Y + m.Area.Y * scale, m.Area.Width * scale, m.Area.Height * scale);

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (Image is null) return;
        RectangleF ir = ImageRect();
        float scale = ir.Width / Image.Width;
        for (int i = 0; i < matches.Count; i++)
            if (FaceRect(matches[i], ir, scale).Contains(e.Location))
            {
                FaceClicked?.Invoke(this, i);
                return;
            }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (Image is null) { Cursor = Cursors.Default; return; }
        RectangleF ir = ImageRect();
        float scale = ir.Width / Image.Width;
        Cursor = matches.Any(m => FaceRect(m, ir, scale).Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
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
        var usedTags = new List<RectangleF>();

        if (matches.Count == 0 && ShowNoFaceHint)
            DrawNoFaceHint(g, ir);

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var r = FaceRect(m, ir, scale);
            Color c = m.IsKnown ? Palette.Accent : Palette.Unknown;
            DrawBrackets(g, r, c, i == highlightIndex);

            string text = m.IsKnown ? $"{m.Label}  ·  %{m.Confidence:0}" : m.Label;
            Size ts = TextRenderer.MeasureText(text, labelFont, Size.Empty, TextFormatFlags.NoPrefix);
            float tw = ts.Width + 14, th = ts.Height + 6;
            // Etiket yüzün üstünde; yer yoksa çerçevenin içine, sağ/alt kenardan da taşmasın
            float ty = r.Y - th - 4;
            if (ty < ir.Y) ty = Math.Min(r.Y + 4, ir.Bottom - th);
            float tx = Math.Clamp(r.X, ir.X, Math.Max(ir.X, ir.Right - tw));
            var tag = new RectangleF(tx, ty, tw, th);
            // Yakın yüzlerin etiketleri üst üste binmesin
            for (int guard = 0; guard < 8 && usedTags.Any(u => u.IntersectsWith(tag)); guard++)
                tag.Y = Math.Min(tag.Y + th + 2, ir.Bottom - th);
            usedTags.Add(tag);

            using (var path = Palette.Rounded(tag, 5))
            using (var b = new SolidBrush(Color.FromArgb(225, c)))
                g.FillPath(b, path);
            TextRenderer.DrawText(g, text, labelFont, Rectangle.Round(tag), Color.FromArgb(10, 20, 16),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            if (m.IsKnown)
            {
                // güven çubuğu (çerçevenin altında, resmin dışına taşmadan)
                var bar = new RectangleF(r.X, Math.Min(r.Bottom + 6, ir.Bottom - 5), r.Width, 4);
                using var bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                using var fg = new SolidBrush(c);
                g.FillRectangle(bg, bar);
                g.FillRectangle(fg, bar.X, bar.Y, bar.Width * (float)(m.Confidence / 100.0), bar.Height);
            }
        }
    }

    /// <summary>Görüntü var ama yüz yoksa resmin altında kısa bir açıklama.</summary>
    [DefaultValue(true)]
    public bool ShowNoFaceHint { get; set; } = true;

    private void DrawNoFaceHint(Graphics g, RectangleF ir)
    {
        const string text = "Yüz bulunamadı · daha aydınlık ve kameraya dönük bir görüntü deneyin";
        Size ts = TextRenderer.MeasureText(text, emptySmall, Size.Empty, TextFormatFlags.NoPrefix);
        var pill = new RectangleF(ir.X + (ir.Width - ts.Width - 24) / 2, ir.Bottom - ts.Height - 22, ts.Width + 24, ts.Height + 10);
        using (var path = Palette.Rounded(pill, pill.Height / 2))
        using (var b = new SolidBrush(Color.FromArgb(210, 12, 18, 24)))
            g.FillPath(b, path);
        TextRenderer.DrawText(g, text, emptySmall, Rectangle.Round(pill), Palette.Unknown,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static void DrawBrackets(Graphics g, RectangleF r, Color c, bool highlighted = false)
    {
        float len = Math.Max(10, Math.Min(r.Width, r.Height) * 0.22f);
        if (highlighted)
            using (var glow = new Pen(Color.FromArgb(110, Color.White), 7))
                g.DrawRectangle(glow, r.X - 4, r.Y - 4, r.Width + 8, r.Height + 8);
        using var pen = new Pen(c, highlighted ? 5 : 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
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
        SelectionMode = SelectionMode.One;
    }

    /// <summary>Liste boşken ortada gösterilen yazı.</summary>
    [DefaultValue("")]
    public string EmptyText { get; set; } = "";

    private int S(int v) => (int)Math.Round(v * DeviceDpi / 96.0);

    // ItemHeight form ölçeklemesiyle büyümüyor; %125-%150'de satırlar üst üste binmesin
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ItemHeight = S(54);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        const int WM_PAINT = 0x000F;
        if (m.Msg == WM_PAINT && Items.Count == 0 && EmptyText.Length > 0)
        {
            using var g = CreateGraphics();
            TextRenderer.DrawText(g, EmptyText, Font, ClientRectangle, Palette.TextDim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    public void SetItems(IEnumerable<Item> items)
    {
        foreach (var old in Items.OfType<Item>())
            old.Thumbnail.Dispose();
        int keep = SelectedIndex;
        BeginUpdate();
        Items.Clear();
        foreach (var i in items)
            Items.Add(i);
        if (keep >= 0 && keep < Items.Count)
            SelectedIndex = keep;
        EndUpdate();
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not Item item)
            return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (var bg = new SolidBrush(selected ? Palette.PanelLight : BackColor))
            g.FillRectangle(bg, e.Bounds);

        var m = item.Match;
        Color c = m.IsKnown ? Palette.Accent : Palette.Unknown;
        if (selected)
            using (var mark = new SolidBrush(c))
                g.FillRectangle(mark, e.Bounds.X, e.Bounds.Y + S(6), S(3), e.Bounds.Height - S(12));
        var thumb = new Rectangle(e.Bounds.X + S(6), e.Bounds.Y + S(5), S(44), S(44));
        g.DrawImage(item.Thumbnail, thumb);
        using (var p = new Pen(c, 2))
            g.DrawRectangle(p, thumb);

        int x = thumb.Right + S(10);
        int w = Math.Max(10, e.Bounds.Right - x - S(8));
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(g, m.Label, nameFont, new Rectangle(x, e.Bounds.Y + S(6), w, S(20)), m.IsKnown ? Palette.Text : Palette.Unknown, flags);

        string detail = double.IsNaN(m.Distance) ? "model eğitilmedi"
            : m.IsKnown ? $"güven %{m.Confidence:0}  ·  mesafe {m.Distance:0}"
            : $"en yakın: {m.ClosestLabel}  ·  mesafe {m.Distance:0}";
        TextRenderer.DrawText(g, detail, smallFont, new Rectangle(x, e.Bounds.Y + S(26), w, S(16)), Palette.TextDim, flags);

        var bar = new Rectangle(x, e.Bounds.Y + S(44), w, Math.Max(3, S(4)));
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
