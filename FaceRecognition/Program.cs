namespace FaceRecognition;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Beklenmedik bir hata olursa uygulama kapanmasın; kullanıcıya ne olduğu söylensin
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show("Beklenmeyen bir hata oluştu, işlem iptal edildi:\n\n" + e.Exception.Message,
                "Yüz Tanıma", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        Application.Run(new MainForm());
    }
}
