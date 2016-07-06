using System;
using System.Runtime.InteropServices;

namespace Mirai
{
    class ConsoleEvents
    {
        static ConsoleEventDelegate ConsoleEventHandler;
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        internal static void SetHandler(Action Action)
        {
            ConsoleEventHandler = new ConsoleEventDelegate(delegate (int EventType)
            {
                if (EventType == 2)
                {
                    Action();
                    return false;
                }

                return true;
            });

            SetConsoleCtrlHandler(ConsoleEventHandler, true);
        }
    }
}
