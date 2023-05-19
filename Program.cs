using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace processMonitor
{
    public class ProcessMonitor
    {
        private string processName;
        private int maxLifetime;
        private int monitorFrequency;
        private DateTime processKillTime;
        private DateTime lastMonotoringTime;
        private Process process;
        private CancellationTokenSource cts;

        public void InitFromArgs(string[] args)
        {
            processName = args[0];
            maxLifetime = int.Parse(args[1]);
            monitorFrequency = int.Parse(args[2]);
        }

        public int MaxLifetime
        {
            get { return maxLifetime; }
        }

        public int MonitorFrequency
        {
            get { return monitorFrequency; }
        }

        public string ProcessName
        {
            get { return processName; }
        }

        public DateTime ProcessKillTime
        {
            get { return processKillTime; }
        }

        public DateTime LastMonotoringTime
        {
            get { return lastMonotoringTime; }
        }

        static void Main(string[] args)
        {
            ProcessMonitor monitor = new ProcessMonitor();

            if (monitor.ValidateArgs(args))
            {
                monitor.InitFromArgs(args);
                monitor.Run();
            }
            else
            {
                Console.WriteLine("Args are invalid");
            }
        }

        private bool GetProcessByName(string processName)
        {
            bool ret;
            Process[] processByNameCollection = Process.GetProcessesByName(processName);

            ret = processByNameCollection.Length > 0;

            if (ret)
            {
                process = processByNameCollection[0];
            }

            return ret;
        }

        public void Run()
        {
            Console.WriteLine("monitoring started");

            cts = new CancellationTokenSource();
            var task = new Task(MonitorProcess, cts.Token);

            task.Start();

            while (!task.IsCompleted)
            {
                var keyInput = Console.ReadKey(true);

                if (keyInput.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Escape was pressed, cancelling...");
                    cts.Cancel();
                    Thread.Sleep(1050);
                }
            }

            Console.WriteLine("Done.");
        }

        private void KillProcess()
        {
            process.Kill();
            Console.WriteLine("Process killed");
            processKillTime = DateTime.MinValue;
        }

        //monitoring process:
        //a) if process NOT found - sleep for X minutes
        //b) else - save current timestamp
        //c) validate maxLifetime(current time - saved time) - if not yet reached - sleep for X minutes
        //d) else - kill process
        private void MonitorProcess()
        {
            double second = 0;
            
            while (!cts.IsCancellationRequested)
            {
                if (second / 30 == monitorFrequency)
                {
                    Console.WriteLine($"monitoring time - {DateTime.Now}");
                    lastMonotoringTime = DateTime.Now;
                    if (GetProcessByName(processName))
                    {
                        SaveProcessKillTime();

                        if (!ValidateMaxLifetime())
                        {
                            KillProcess();
                        }
                    }
                    second = 0;
                }

                Thread.Sleep(1000);
                second++;
            }
        }

        private void SaveProcessKillTime()
        { 
            if (processKillTime == DateTime.MinValue)
            {
                processKillTime = process.StartTime.AddMinutes(maxLifetime);
                Console.WriteLine($"process kill time - {processKillTime}");
            }
        }

        public bool ValidateArgs(string[] args)
        {
            bool ret = true;

            if (args == null)
            {
                Console.WriteLine("args are empty");
                return false;
            }

            if (args.Length != 3)
            {
                Console.WriteLine("Wrong number of arguments specified");
                return false;
            }

            if (args[0].GetType() != typeof(String))
            {
                Console.WriteLine("Process name is invalid");
                ret = false;
            }

            if (!int.TryParse(args[1], out _))
            {
                Console.WriteLine("Max lifetime is invalid");
                ret = false;
            }

            if (!int.TryParse(args[2], out _))
            {
                Console.WriteLine("Monitor frequency is invalid");
                ret = false;
            }

            return ret;
        }

        private bool ValidateMaxLifetime()
        {
            return processKillTime > DateTime.Now;
        }
    }
}
