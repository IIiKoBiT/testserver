using System.IO;

namespace System
{
    enum LoggerType
    {
        Info,
        PrimaryInfo,
        Warning,
        PrimaryWarning,
        Error,
        PrimaryError
    }

    static class Logger
    {
        // Logger threading block;
        private static object Locker = new object();

        // Logger file stream;
        private static StreamWriter LoggerFile = new StreamWriter(@"Logger.log", true);

        // Logger initializer;
        public static void Initialize()
        {
            // Register handler for unhandled exception;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Write separator in file;
            var separator = "";

            for (var i = 0; i < 256; i++)
            {
                separator += "-";
            }

            LoggerFile.WriteLine(separator);

            // Write logger start message;
            Log("Logger started!", LoggerType.Info);
        }

        // Method handler for unhandled exception;
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs arguments)
        {
            var exception = (Exception)arguments.ExceptionObject;
            Log(exception, LoggerType.PrimaryError);
        }

        // Method for log text in file;
        public static void Log(String text, LoggerType type = LoggerType.Error)
        {
            lock (Locker)
            {
                LoggerFile.WriteLine(DateTime.Now.ToString() + " | " + type.ToString() + " | " + text);
                LoggerFile.Flush();
            }
        }

        // Method for log exception in file;
        public static void Log(Exception exception, LoggerType type = LoggerType.Error)
        {
            Log(DateTime.Now.ToString() + " | " + type.ToString() + " | " + exception.Message + exception.StackTrace + exception.InnerException, type);
        }
    }
}
