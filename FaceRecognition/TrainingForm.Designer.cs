namespace FaceRecognition;

partial class TrainingForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
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
        pnlLeft = new Panel();
        lstPeople = new ListBox();
        pnlAddPerson = new Panel();
        btnDeletePerson = new FaceRecognition.Ui.ScanButton();
        btnAddPerson = new FaceRecognition.Ui.ScanButton();
        txtNewPerson = new TextBox();
        lblPeople = new Label();
        pnlRight = new Panel();
        lvSamples = new ListView();
        imgSamples = new ImageList(components);
        pnlBottom = new Panel();
        lblInfo = new Label();
        btnLoadModel = new FaceRecognition.Ui.ScanButton();
        btnSaveModel = new FaceRecognition.Ui.ScanButton();
        btnRetrain = new FaceRecognition.Ui.ScanButton();
        btnAddFromCurrent = new FaceRecognition.Ui.ScanButton();
        btnAddFromFile = new FaceRecognition.Ui.ScanButton();
        lblSelected = new Label();
        pnlLeft.SuspendLayout();
        pnlAddPerson.SuspendLayout();
        pnlRight.SuspendLayout();
        pnlBottom.SuspendLayout();
        SuspendLayout();
        //
        // pnlLeft
        //
        pnlLeft.BackColor = Color.FromArgb(19, 28, 36);
        pnlLeft.Controls.Add(lstPeople);
        pnlLeft.Controls.Add(pnlAddPerson);
        pnlLeft.Controls.Add(lblPeople);
        pnlLeft.Dock = DockStyle.Left;
        pnlLeft.Name = "pnlLeft";
        pnlLeft.Padding = new Padding(14, 12, 14, 12);
        pnlLeft.Size = new Size(250, 560);
        pnlLeft.TabIndex = 0;
        //
        // lstPeople
        //
        lstPeople.BackColor = Color.FromArgb(19, 28, 36);
        lstPeople.BorderStyle = BorderStyle.None;
        lstPeople.Dock = DockStyle.Fill;
        lstPeople.DrawMode = DrawMode.OwnerDrawFixed;
        lstPeople.ForeColor = Color.FromArgb(226, 236, 240);
        lstPeople.IntegralHeight = false;
        lstPeople.ItemHeight = 44;
        lstPeople.Name = "lstPeople";
        lstPeople.TabIndex = 1;
        lstPeople.DrawItem += lstPeople_DrawItem;
        lstPeople.SelectedIndexChanged += lstPeople_SelectedIndexChanged;
        //
        // pnlAddPerson
        //
        pnlAddPerson.Controls.Add(btnDeletePerson);
        pnlAddPerson.Controls.Add(btnAddPerson);
        pnlAddPerson.Controls.Add(txtNewPerson);
        pnlAddPerson.Dock = DockStyle.Bottom;
        pnlAddPerson.Name = "pnlAddPerson";
        pnlAddPerson.Size = new Size(222, 112);
        pnlAddPerson.TabIndex = 2;
        //
        // btnDeletePerson
        //
        btnDeletePerson.Enabled = false;
        btnDeletePerson.Kind = FaceRecognition.Ui.ScanButtonKind.Danger;
        btnDeletePerson.Location = new Point(0, 76);
        btnDeletePerson.Name = "btnDeletePerson";
        btnDeletePerson.Size = new Size(222, 32);
        btnDeletePerson.TabIndex = 2;
        btnDeletePerson.Text = "Seçili kişiyi sil";
        btnDeletePerson.Click += btnDeletePerson_Click;
        //
        // btnAddPerson
        //
        btnAddPerson.Location = new Point(0, 38);
        btnAddPerson.Name = "btnAddPerson";
        btnAddPerson.Size = new Size(222, 32);
        btnAddPerson.TabIndex = 1;
        btnAddPerson.Text = "+ Yeni kişi ekle";
        btnAddPerson.Click += btnAddPerson_Click;
        //
        // txtNewPerson
        //
        txtNewPerson.BackColor = Color.FromArgb(28, 40, 50);
        txtNewPerson.BorderStyle = BorderStyle.FixedSingle;
        txtNewPerson.Font = new Font("Segoe UI", 10F);
        txtNewPerson.ForeColor = Color.FromArgb(226, 236, 240);
        txtNewPerson.Location = new Point(0, 6);
        txtNewPerson.MaxLength = 30;
        txtNewPerson.Name = "txtNewPerson";
        txtNewPerson.PlaceholderText = "Yeni kişi etiketi (ör. Gönüllü E)";
        txtNewPerson.Size = new Size(222, 25);
        txtNewPerson.TabIndex = 0;
        //
        // lblPeople
        //
        lblPeople.Dock = DockStyle.Top;
        lblPeople.Font = new Font("Segoe UI Semibold", 8.5F);
        lblPeople.ForeColor = Color.FromArgb(128, 150, 162);
        lblPeople.Name = "lblPeople";
        lblPeople.Size = new Size(222, 28);
        lblPeople.TabIndex = 0;
        lblPeople.Text = "KİŞİLER (ETİKETLER)";
        //
        // pnlRight
        //
        pnlRight.BackColor = Color.FromArgb(12, 18, 24);
        pnlRight.Controls.Add(lvSamples);
        pnlRight.Controls.Add(pnlBottom);
        pnlRight.Controls.Add(lblSelected);
        pnlRight.Dock = DockStyle.Fill;
        pnlRight.Name = "pnlRight";
        pnlRight.Padding = new Padding(16, 12, 16, 12);
        pnlRight.TabIndex = 1;
        //
        // lvSamples
        //
        lvSamples.BackColor = Color.FromArgb(12, 18, 24);
        lvSamples.BorderStyle = BorderStyle.None;
        lvSamples.Dock = DockStyle.Fill;
        lvSamples.ForeColor = Color.FromArgb(128, 150, 162);
        lvSamples.LargeImageList = imgSamples;
        lvSamples.Name = "lvSamples";
        lvSamples.TabIndex = 1;
        lvSamples.UseCompatibleStateImageBehavior = false;
        //
        // imgSamples
        //
        imgSamples.ColorDepth = ColorDepth.Depth24Bit;
        imgSamples.ImageSize = new Size(80, 80);
        imgSamples.TransparentColor = Color.Transparent;
        //
        // pnlBottom
        //
        pnlBottom.Controls.Add(lblInfo);
        pnlBottom.Controls.Add(btnLoadModel);
        pnlBottom.Controls.Add(btnSaveModel);
        pnlBottom.Controls.Add(btnRetrain);
        pnlBottom.Controls.Add(btnAddFromCurrent);
        pnlBottom.Controls.Add(btnAddFromFile);
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.Name = "pnlBottom";
        pnlBottom.Size = new Size(588, 96);
        pnlBottom.TabIndex = 2;
        //
        // lblInfo
        //
        lblInfo.ForeColor = Color.FromArgb(128, 150, 162);
        lblInfo.Location = new Point(0, 70);
        lblInfo.Name = "lblInfo";
        lblInfo.Size = new Size(588, 22);
        lblInfo.TabIndex = 5;
        lblInfo.Text = "Eğitim için en az 2 kişi gerekli. Yalnızca izin veren gönüllülerin fotoğraflarını kullanın.";
        //
        // btnLoadModel
        //
        btnLoadModel.Location = new Point(448, 34);
        btnLoadModel.Name = "btnLoadModel";
        btnLoadModel.Size = new Size(140, 30);
        btnLoadModel.TabIndex = 4;
        btnLoadModel.Text = "Modeli yükle...";
        btnLoadModel.Click += btnLoadModel_Click;
        //
        // btnSaveModel
        //
        btnSaveModel.Location = new Point(302, 34);
        btnSaveModel.Name = "btnSaveModel";
        btnSaveModel.Size = new Size(140, 30);
        btnSaveModel.TabIndex = 3;
        btnSaveModel.Text = "Modeli kaydet...";
        btnSaveModel.Click += btnSaveModel_Click;
        //
        // btnRetrain
        //
        btnRetrain.Kind = FaceRecognition.Ui.ScanButtonKind.Primary;
        btnRetrain.Location = new Point(302, 0);
        btnRetrain.Name = "btnRetrain";
        btnRetrain.Size = new Size(286, 30);
        btnRetrain.TabIndex = 2;
        btnRetrain.Text = "Modeli yeniden eğit";
        btnRetrain.Click += btnRetrain_Click;
        //
        // btnAddFromCurrent
        //
        btnAddFromCurrent.Enabled = false;
        btnAddFromCurrent.Location = new Point(0, 34);
        btnAddFromCurrent.Name = "btnAddFromCurrent";
        btnAddFromCurrent.Size = new Size(290, 30);
        btnAddFromCurrent.TabIndex = 1;
        btnAddFromCurrent.Text = "Ekrandaki yüzü bu kişiye ekle";
        btnAddFromCurrent.Click += btnAddFromCurrent_Click;
        //
        // btnAddFromFile
        //
        btnAddFromFile.Enabled = false;
        btnAddFromFile.Location = new Point(0, 0);
        btnAddFromFile.Name = "btnAddFromFile";
        btnAddFromFile.Size = new Size(290, 30);
        btnAddFromFile.TabIndex = 0;
        btnAddFromFile.Text = "Dosyadan örnek ekle...";
        btnAddFromFile.Click += btnAddFromFile_Click;
        //
        // lblSelected
        //
        lblSelected.Dock = DockStyle.Top;
        lblSelected.Font = new Font("Segoe UI Semibold", 12F);
        lblSelected.ForeColor = Color.FromArgb(226, 236, 240);
        lblSelected.Name = "lblSelected";
        lblSelected.Size = new Size(588, 34);
        lblSelected.TabIndex = 0;
        lblSelected.Text = "Bir kişi seçin";
        //
        // TrainingForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(12, 18, 24);
        ClientSize = new Size(870, 560);
        Controls.Add(pnlRight);
        Controls.Add(pnlLeft);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(226, 236, 240);
        MinimizeBox = false;
        MinimumSize = new Size(800, 480);
        Name = "TrainingForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Eğitim ve örnekler";
        Load += TrainingForm_Load;
        pnlLeft.ResumeLayout(false);
        pnlAddPerson.ResumeLayout(false);
        pnlAddPerson.PerformLayout();
        pnlRight.ResumeLayout(false);
        pnlBottom.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlLeft;
    private Label lblPeople;
    private ListBox lstPeople;
    private Panel pnlAddPerson;
    private TextBox txtNewPerson;
    private FaceRecognition.Ui.ScanButton btnAddPerson;
    private FaceRecognition.Ui.ScanButton btnDeletePerson;
    private Panel pnlRight;
    private Label lblSelected;
    private ListView lvSamples;
    private ImageList imgSamples;
    private Panel pnlBottom;
    private FaceRecognition.Ui.ScanButton btnAddFromFile;
    private FaceRecognition.Ui.ScanButton btnAddFromCurrent;
    private FaceRecognition.Ui.ScanButton btnRetrain;
    private FaceRecognition.Ui.ScanButton btnSaveModel;
    private FaceRecognition.Ui.ScanButton btnLoadModel;
    private Label lblInfo;
}
