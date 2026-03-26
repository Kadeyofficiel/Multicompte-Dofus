using System;
using System.Windows.Forms;

namespace MultiCompte
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                NativeMethods.SetProcessDpiAwarenessContext(
                    NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch { }
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
