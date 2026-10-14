namespace game_câu_cá
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Form1.LogError("ThreadException", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Form1.LogError("UnhandledException", e.ExceptionObject as Exception);
            Application.Run(new Form1());
        }
    }
}