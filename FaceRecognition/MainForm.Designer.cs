namespace FaceRecognition;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pnlTop = new Panel();
        chipModel = new FaceRecognition.Ui.StatusChip();
        chipSource = new FaceRecognition.Ui.StatusChip();
        lblSubtitle = new Label();
        lblTitle = new Label();
        pnlAlert = new Panel();
        btnAlertClose = new Button();
        lblAlert = new Label();
        pnlSide = new Panel();
        cardResults = new FaceRecognition.Ui.ScanCard();
        lstResults = new FaceRecognition.Ui.FaceResultList();
        spacer3 = new Panel();
        cardSettings = new FaceRecognition.Ui.ScanCard();
        trkMargin = new TrackBar();
        lblMargin = new Label();
        trkThreshold = new TrackBar();
        lblThreshold = new Label();
        spacer2 = new Panel();
        pnlActions = new Panel();
        btnTraining = new FaceRecognition.Ui.ScanButton();
        btnSaveFaces = new FaceRecognition.Ui.ScanButton();
        spacer1 = new Panel();
        cardSource = new FaceRecognition.Ui.ScanCard();
        lblSourceInfo = new Label();
        btnSample = new FaceRecognition.Ui.ScanButton();
        btnImage = new FaceRecognition.Ui.ScanButton();
        btnCamera = new FaceRecognition.Ui.ScanButton();
        pnlView = new Panel();
        picView = new FaceRecognition.Ui.AnalysisPictureBox();
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        toolTip = new ToolTip(components);
        pnlTop.SuspendLayout();
        pnlAlert.SuspendLayout();
        pnlSide.SuspendLayout();
        cardResults.SuspendLayout();
        cardSettings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)trkMargin).BeginInit();
        ((System.ComponentModel.ISupportInitialize)trkThreshold).BeginInit();
        pnlActions.SuspendLayout();
        cardSource.SuspendLayout();
        pnlView.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picView).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // pnlTop
        //
        pnlTop.BackColor = Color.FromArgb(19, 28, 36);
        pnlTop.Controls.Add(chipModel);
        pnlTop.Controls.Add(chipSource);
        pnlTop.Controls.Add(lblSubtitle);
        pnlTop.Controls.Add(lblTitle);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Location = new Point(0, 0);
        pnlTop.Name = "pnlTop";
        pnlTop.Size = new Size(1060, 64);
        pnlTop.TabIndex = 0;
        pnlTop.Paint += pnlTop_Paint;
        //
        // chipModel
        //
        chipModel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        chipModel.Location = new Point(840, 16);
        chipModel.Name = "chipModel";
        chipModel.Size = new Size(204, 32);
        chipModel.TabIndex = 3;
        chipModel.Text = "Model yükleniyor...";
        //
        // chipSource
        //
        chipSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        chipSource.Location = new Point(666, 16);
        chipSource.Name = "chipSource";
        chipSource.Size = new Size(166, 32);
        chipSource.TabIndex = 2;
        chipSource.Text = "Kaynak yok";
        //
        // lblSubtitle
        //
        lblSubtitle.AutoSize = true;
        lblSubtitle.ForeColor = Color.FromArgb(128, 150, 162);
        lblSubtitle.Location = new Point(22, 39);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(230, 15);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "OpenCV · Haar cascade · LBPH  ·  YMH219";
        //
        // lblTitle
        //
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI Semibold", 15F);
        lblTitle.ForeColor = Color.FromArgb(226, 236, 240);
        lblTitle.Location = new Point(20, 8);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(110, 28);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Yüz Tanıma";
        //
        // pnlAlert
        //
        pnlAlert.BackColor = Color.FromArgb(70, 26, 36);
        pnlAlert.Controls.Add(btnAlertClose);
        pnlAlert.Controls.Add(lblAlert);
        pnlAlert.Dock = DockStyle.Top;
        pnlAlert.Location = new Point(0, 64);
        pnlAlert.Name = "pnlAlert";
        pnlAlert.Size = new Size(1060, 40);
        pnlAlert.TabIndex = 1;
        pnlAlert.Visible = false;
        //
        // btnAlertClose
        //
        btnAlertClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAlertClose.FlatAppearance.BorderSize = 0;
        btnAlertClose.FlatStyle = FlatStyle.Flat;
        btnAlertClose.ForeColor = Color.White;
        btnAlertClose.Location = new Point(1020, 6);
        btnAlertClose.Name = "btnAlertClose";
        btnAlertClose.Size = new Size(30, 28);
        btnAlertClose.TabIndex = 1;
        btnAlertClose.Text = "✕";
        btnAlertClose.Click += btnAlertClose_Click;
        //
        // lblAlert
        //
        lblAlert.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblAlert.Font = new Font("Segoe UI Semibold", 9F);
        lblAlert.ForeColor = Color.White;
        lblAlert.Location = new Point(20, 0);
        lblAlert.Name = "lblAlert";
        lblAlert.Size = new Size(990, 40);
        lblAlert.TabIndex = 0;
        lblAlert.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pnlSide
        //
        pnlSide.BackColor = Color.FromArgb(12, 18, 24);
        pnlSide.Controls.Add(cardResults);
        pnlSide.Controls.Add(spacer3);
        pnlSide.Controls.Add(cardSettings);
        pnlSide.Controls.Add(spacer2);
        pnlSide.Controls.Add(pnlActions);
        pnlSide.Controls.Add(spacer1);
        pnlSide.Controls.Add(cardSource);
        pnlSide.Dock = DockStyle.Right;
        pnlSide.Location = new Point(730, 104);
        pnlSide.Name = "pnlSide";
        pnlSide.Padding = new Padding(0, 14, 16, 12);
        pnlSide.Size = new Size(330, 534);
        pnlSide.TabIndex = 3;
        //
        // cardResults
        //
        cardResults.Controls.Add(lstResults);
        cardResults.Dock = DockStyle.Fill;
        cardResults.Name = "cardResults";
        cardResults.TabIndex = 4;
        cardResults.Title = "Bulunan yüzler";
        //
        // lstResults
        //
        lstResults.Dock = DockStyle.Fill;
        lstResults.Name = "lstResults";
        lstResults.TabIndex = 0;
        //
        // spacer3
        //
        spacer3.Dock = DockStyle.Bottom;
        spacer3.Name = "spacer3";
        spacer3.Size = new Size(314, 10);
        spacer3.TabIndex = 5;
        //
        // cardSettings
        //
        cardSettings.Controls.Add(trkMargin);
        cardSettings.Controls.Add(lblMargin);
        cardSettings.Controls.Add(trkThreshold);
        cardSettings.Controls.Add(lblThreshold);
        cardSettings.Dock = DockStyle.Bottom;
        cardSettings.Name = "cardSettings";
        cardSettings.Size = new Size(314, 132);
        cardSettings.TabIndex = 3;
        cardSettings.Title = "Hassasiyet";
        //
        // trkMargin
        //
        trkMargin.AutoSize = false;
        trkMargin.BackColor = Color.FromArgb(19, 28, 36);
        trkMargin.Location = new Point(10, 98);
        trkMargin.Maximum = 20;
        trkMargin.Name = "trkMargin";
        trkMargin.Size = new Size(294, 26);
        trkMargin.TabIndex = 3;
        trkMargin.TickStyle = TickStyle.None;
        trkMargin.Value = 6;
        trkMargin.ValueChanged += sensitivity_ValueChanged;
        //
        // lblMargin
        //
        lblMargin.AutoSize = true;
        lblMargin.ForeColor = Color.FromArgb(226, 236, 240);
        lblMargin.Location = new Point(14, 80);
        lblMargin.Name = "lblMargin";
        lblMargin.Size = new Size(140, 15);
        lblMargin.TabIndex = 2;
        lblMargin.Text = "Ayırt etme farkı: 6";
        //
        // trkThreshold
        //
        trkThreshold.AutoSize = false;
        trkThreshold.BackColor = Color.FromArgb(19, 28, 36);
        trkThreshold.Location = new Point(10, 50);
        trkThreshold.Maximum = 150;
        trkThreshold.Minimum = 80;
        trkThreshold.Name = "trkThreshold";
        trkThreshold.Size = new Size(294, 26);
        trkThreshold.TabIndex = 1;
        trkThreshold.TickStyle = TickStyle.None;
        trkThreshold.Value = 125;
        trkThreshold.ValueChanged += sensitivity_ValueChanged;
        //
        // lblThreshold
        //
        lblThreshold.AutoSize = true;
        lblThreshold.ForeColor = Color.FromArgb(226, 236, 240);
        lblThreshold.Location = new Point(14, 32);
        lblThreshold.Name = "lblThreshold";
        lblThreshold.Size = new Size(140, 15);
        lblThreshold.TabIndex = 0;
        lblThreshold.Text = "En fazla mesafe: 125";
        //
        // spacer2
        //
        spacer2.Dock = DockStyle.Bottom;
        spacer2.Name = "spacer2";
        spacer2.Size = new Size(314, 10);
        spacer2.TabIndex = 6;
        //
        // pnlActions
        //
        pnlActions.Controls.Add(btnTraining);
        pnlActions.Controls.Add(btnSaveFaces);
        pnlActions.Dock = DockStyle.Bottom;
        pnlActions.Name = "pnlActions";
        pnlActions.Size = new Size(314, 40);
        pnlActions.TabIndex = 2;
        //
        // btnTraining
        //
        btnTraining.Kind = FaceRecognition.Ui.ScanButtonKind.Primary;
        btnTraining.Location = new Point(158, 2);
        btnTraining.Name = "btnTraining";
        btnTraining.Size = new Size(156, 36);
        btnTraining.TabIndex = 1;
        btnTraining.Text = "Eğitim / örnekler";
        btnTraining.Click += btnTraining_Click;
        //
        // btnSaveFaces
        //
        btnSaveFaces.Enabled = false;
        btnSaveFaces.Location = new Point(0, 2);
        btnSaveFaces.Name = "btnSaveFaces";
        btnSaveFaces.Size = new Size(150, 36);
        btnSaveFaces.TabIndex = 0;
        btnSaveFaces.Text = "Yüzleri kaydet";
        btnSaveFaces.Click += btnSaveFaces_Click;
        //
        // spacer1
        //
        spacer1.Dock = DockStyle.Top;
        spacer1.Name = "spacer1";
        spacer1.Size = new Size(314, 10);
        spacer1.TabIndex = 7;
        //
        // cardSource
        //
        cardSource.Controls.Add(lblSourceInfo);
        cardSource.Controls.Add(btnSample);
        cardSource.Controls.Add(btnImage);
        cardSource.Controls.Add(btnCamera);
        cardSource.Dock = DockStyle.Top;
        cardSource.Name = "cardSource";
        cardSource.Size = new Size(314, 150);
        cardSource.TabIndex = 0;
        cardSource.Title = "Kaynak";
        //
        // lblSourceInfo
        //
        lblSourceInfo.ForeColor = Color.FromArgb(128, 150, 162);
        lblSourceInfo.Location = new Point(14, 118);
        lblSourceInfo.Name = "lblSourceInfo";
        lblSourceInfo.Size = new Size(288, 22);
        lblSourceInfo.TabIndex = 3;
        lblSourceInfo.Text = "Henüz görüntü yok";
        //
        // btnSample
        //
        btnSample.Location = new Point(158, 76);
        btnSample.Name = "btnSample";
        btnSample.Size = new Size(142, 34);
        btnSample.TabIndex = 2;
        btnSample.Text = "Örnek grup fotoğrafı";
        btnSample.Click += btnSample_Click;
        //
        // btnImage
        //
        btnImage.Location = new Point(14, 76);
        btnImage.Name = "btnImage";
        btnImage.Size = new Size(138, 34);
        btnImage.TabIndex = 1;
        btnImage.Text = "Görüntü seç...";
        btnImage.Click += btnImage_Click;
        //
        // btnCamera
        //
        btnCamera.Kind = FaceRecognition.Ui.ScanButtonKind.Primary;
        btnCamera.Location = new Point(14, 34);
        btnCamera.Name = "btnCamera";
        btnCamera.Size = new Size(286, 36);
        btnCamera.TabIndex = 0;
        btnCamera.Text = "Kamerayı başlat";
        btnCamera.Click += btnCamera_Click;
        //
        // pnlView
        //
        pnlView.BackColor = Color.FromArgb(12, 18, 24);
        pnlView.Controls.Add(picView);
        pnlView.Dock = DockStyle.Fill;
        pnlView.Location = new Point(0, 104);
        pnlView.Name = "pnlView";
        pnlView.Padding = new Padding(16, 14, 14, 12);
        pnlView.Size = new Size(730, 534);
        pnlView.TabIndex = 2;
        //
        // picView
        //
        picView.Dock = DockStyle.Fill;
        picView.Name = "picView";
        picView.TabIndex = 0;
        picView.TabStop = false;
        //
        // statusStrip
        //
        statusStrip.BackColor = Color.FromArgb(19, 28, 36);
        statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
        statusStrip.Name = "statusStrip";
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 4;
        //
        // lblStatus
        //
        lblStatus.ForeColor = Color.FromArgb(128, 150, 162);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(200, 17);
        lblStatus.Text = "Hazır";
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(12, 18, 24);
        ClientSize = new Size(1060, 660);
        Controls.Add(pnlView);
        Controls.Add(pnlSide);
        Controls.Add(pnlAlert);
        Controls.Add(pnlTop);
        Controls.Add(statusStrip);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(226, 236, 240);
        MinimumSize = new Size(940, 600);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Yüz Tanıma - YMH219";
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        pnlTop.ResumeLayout(false);
        pnlTop.PerformLayout();
        pnlAlert.ResumeLayout(false);
        pnlSide.ResumeLayout(false);
        cardResults.ResumeLayout(false);
        cardSettings.ResumeLayout(false);
        cardSettings.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)trkMargin).EndInit();
        ((System.ComponentModel.ISupportInitialize)trkThreshold).EndInit();
        pnlActions.ResumeLayout(false);
        cardSource.ResumeLayout(false);
        pnlView.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picView).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel pnlTop;
    private Label lblTitle;
    private Label lblSubtitle;
    private FaceRecognition.Ui.StatusChip chipSource;
    private FaceRecognition.Ui.StatusChip chipModel;
    private Panel pnlAlert;
    private Label lblAlert;
    private Button btnAlertClose;
    private Panel pnlSide;
    private FaceRecognition.Ui.ScanCard cardSource;
    private FaceRecognition.Ui.ScanButton btnCamera;
    private FaceRecognition.Ui.ScanButton btnImage;
    private FaceRecognition.Ui.ScanButton btnSample;
    private Label lblSourceInfo;
    private Panel spacer1;
    private FaceRecognition.Ui.ScanCard cardResults;
    private FaceRecognition.Ui.FaceResultList lstResults;
    private Panel spacer3;
    private FaceRecognition.Ui.ScanCard cardSettings;
    private Label lblThreshold;
    private TrackBar trkThreshold;
    private Label lblMargin;
    private TrackBar trkMargin;
    private Panel spacer2;
    private Panel pnlActions;
    private FaceRecognition.Ui.ScanButton btnSaveFaces;
    private FaceRecognition.Ui.ScanButton btnTraining;
    private Panel pnlView;
    private FaceRecognition.Ui.AnalysisPictureBox picView;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private ToolTip toolTip;
}
