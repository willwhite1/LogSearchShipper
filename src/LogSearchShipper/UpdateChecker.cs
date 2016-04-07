using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LogSearchShipper
{
    public class UpdateChecker
    {
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL
        }
        
        private Process _updateProcess;

        public void Start()
        {
            var updaterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Updater.exe");
            if (File.Exists(updaterPath))
            {
                var processInfo = new ProcessStartInfo(updaterPath) { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true };
                _updateProcess = Process.Start(processInfo);
            }
        }

        public void Stop()
        {
            if (_updateProcess != null)
            {
                if (!_updateProcess.WaitForExit(1))
                {
                    GenerateConsoleCtrlEvent(UpdateChecker.ConsoleCtrlEvent.CTRL_C, 0);
                    if (!_updateProcess.WaitForExit(5000))
                    {
                        _updateProcess.Kill();
                    }
                }
                _updateProcess = null;
            }
        }
    }
}