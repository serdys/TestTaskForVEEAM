using NUnit.Framework;
using processMonitor;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void InitTypesTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();

            monitor.InitFromArgs(new[] { "notepad", "5", "1" });

            Assert.AreEqual(typeof(string), monitor.ProcessName.GetType());
            Assert.AreEqual(typeof(int), monitor.MaxLifetime.GetType());
            Assert.AreEqual(typeof(int), monitor.MonitorFrequency.GetType());
        }

        [Test]
        public void InitValuesTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();

            monitor.InitFromArgs(new[] { "notepad", "5", "1" });

            Assert.AreEqual("notepad", monitor.ProcessName);
            Assert.AreEqual(5, monitor.MaxLifetime);
            Assert.AreEqual(1, monitor.MonitorFrequency);
        }

        [Test]
        public void ValidationTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();
            string[] args = new[] { "notepad", "5", "1" };

            Assert.True(monitor.ValidateArgs(args));
        }

        [Test]
        public void ValidationNegativeTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();
            string[] args1 = null;
            string[] args2 = new[] { "notepad", "wrong", "1" };
            string[] args3 = new[] { "notepad", "5", "wrong" };
            string[] args4 = new[] { "notepad", "1" };
            string[] args5 = new[] { "notepad", "1", "1", "2" };

            Assert.False(monitor.ValidateArgs(args1));
            Assert.False(monitor.ValidateArgs(args2));
            Assert.False(monitor.ValidateArgs(args3));
            Assert.False(monitor.ValidateArgs(args4));
            Assert.False(monitor.ValidateArgs(args5));
        }

        [Test]
        public void NotepadKillingTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();

            monitor.InitFromArgs(new[] { "notepad", "1", "1" });
            Process notepad = Process.Start("notepad.exe");
            var cts = new CancellationTokenSource();
            var task = new Task(monitor.Run, cts.Token);
            DateTime killTime = notepad.StartTime.AddMinutes(monitor.MaxLifetime);
            task.Start();

            while (!notepad.HasExited)
            {
                // ideally we should get last monitoring time and compare it to kill time
                // but i don`t know yet how to get values back from object executed within a task..
                if (DateTime.Now.AddMinutes(-monitor.MonitorFrequency) > killTime)
                {
                    Assert.Fail();
                }
                Thread.Sleep(1000);
            }
            cts.Cancel();
        }

        [Test]
        public void NotepadMultipleKillingTest()
        {
            ProcessMonitor monitor = new ProcessMonitor();
            int current = 0;
            int maxTries = 3;
            monitor.InitFromArgs(new[] { "notepad", "1", "1" });
            
            var cts = new CancellationTokenSource();
            var task = new Task(monitor.Run, cts.Token);

            task.Start();
            do
            {
                Process notepad = Process.Start("notepad.exe");
                DateTime killTime = notepad.StartTime.AddMinutes(monitor.MaxLifetime);
                while (!notepad.HasExited)
                {
                    // ideally we should get last monitoring time and compare it to kill time
                    // but i don`t know yet how to get values back from object executed within a task..
                    if (DateTime.Now.AddMinutes(-monitor.MonitorFrequency) > killTime)
                    {
                        Assert.Fail();
                    }
                    Thread.Sleep(1000);
                }
                current++;                
            }
            while (maxTries > current);

            cts.Cancel();
        }
    }
}