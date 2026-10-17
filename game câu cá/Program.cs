namespace game_câu_cá
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Form1.LogError("ThreadException", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Form1.LogError("UnhandledException", e.ExceptionObject as Exception);
            Application.Run(new Form1());
        }
    }
}