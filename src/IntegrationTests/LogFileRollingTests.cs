using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    public class LogFileRollingTests
    {
        [Test]
        public void ShippingShouldNotBlockLogFileRolling()
        {
            Process shipper = null;
            Process processWithLogFileRolling = null;

            DeleteOldLogFiles();

            File.Delete("LogsearchShipper.Service.exe.config.bak");
            File.Move("LogsearchShipper.Service.exe.config", "LogsearchShipper.Service.exe.config.bak");
            File.Move("LogsearchShipper.Service.exe.config.ShipDummyService", "LogsearchShipper.Service.exe.config");
            try
            {
                shipper = StartProcess(Environment.CurrentDirectory + @"\LogsearchShipper.Service.exe", "-instance:integrationtest001");
                processWithLogFileRolling = StartProcess(Environment.CurrentDirectory + @"\DummyServiceWithLogRolling.exe", "");

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

                //There should be 6 DummyServiceWithLogRolling.log.* files, unless the shipper has blocked file rolling
                var logFiles =
                    new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*");
                Assert.AreEqual(6, logFiles.Count());

            }
            finally
            {
                //TODO:  Fix shutdown process - seems to leave rogue processes:
                // * DummyServiceWithLogRolling.exe
                // * LogsearchShipper.Service.exe
                // * tmp????.tmp-go-logstash-forwarder.exe
                ShutdownProcess(shipper);
                ShutdownProcess(processWithLogFileRolling);
                
                File.Delete("LogsearchShipper.Service.exe.config");
                File.Move("LogsearchShipper.Service.exe.config.bak", "LogsearchShipper.Service.exe.config");
            }
           
        }

        private Process StartProcess(string processPath, string processArgs)
        {
            var process = new Process { StartInfo = { FileName = processPath, Arguments = processArgs, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardInput = true} };
            process.OutputDataReceived += (sender, args) => Console.WriteLine("{0}: {1}", processPath, args.Data);
            process.Start();
            process.BeginOutputReadLine();

            return process;
        }

        private void ShutdownProcess(Process _process)
        {
            if (_process == null) return;

            _process.StandardInput.WriteLine(char.ConvertFromUtf32(3)); // send Ctrl-C to logstash-forwarder so it can clean up
            _process.CancelOutputRead();
            _process.WaitForExit(5 * 1000);
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
        private static void DeleteOldLogFiles()
        {
            foreach (FileInfo f in new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*"))
            {
                f.Delete();
            }
        }
    }
}
