namespace BotNetVersionControl.Core
{
    public enum ETypeLog
    {
        Log,
        Succes,
        Warning,
        Error,
    }

    public static class Debuger
    {
        private const string LOG_PREFIX = "BOT VERSION[LOG] ~ ";

        public static void PrintLog( string logText, ETypeLog eTypeLog = ETypeLog.Log )
        {
            SetColorConsole( eTypeLog );
            string text = string.Format( "{0}[{1}] {2}", LOG_PREFIX, DateTime.Now, logText );
            Console.WriteLine( text );
            Console.ResetColor();
        }

        private static void SetColorConsole( ETypeLog eTypeLog )
        {
            switch( eTypeLog )
            {
                case ETypeLog.Log:
                    Console.ResetColor();
                    break;
                case ETypeLog.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case ETypeLog.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case ETypeLog.Succes:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
        }
    }
}
