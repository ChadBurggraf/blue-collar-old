//-----------------------------------------------------------------------
// <copyright file="FileSystemWatcherTests.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.IO;
    using System.Threading;
    using BlueCollar.Console;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// File system watcher tests.
    /// </summary>
    [TestClass]
    public sealed class FileSystemWatcherTests
    {
        /// <summary>
        /// Threshold tests.
        /// </summary>
        [TestMethod]
        public void FileSystemWatcherThreshold()
        {
            string dir = Path.GetFullPath(Guid.NewGuid().ToString());
            string file = Guid.NewGuid().ToString() + ".txt";
            string path = Path.Combine(dir, file);
            Directory.CreateDirectory(dir);
            File.AppendAllText(path, "Hello, world!");

            BlueCollar.Console.FileSystemWatcher watcher = new BlueCollar.Console.FileSystemWatcher(dir)
            {
                Threshold = 500
            };

            DateTime now = DateTime.Now;
            ManualResetEvent handle = new ManualResetEvent(false);

            watcher.Operation += new FileSystemEventHandler(
                delegate(object sender, FileSystemEventArgs e)
                {
                    Assert.IsTrue(DateTime.Now >= now.AddMilliseconds(500));
                    handle.Set();
                });

            watcher.EnableRaisingEvents = true;
            File.Delete(path);

            WaitHandle.WaitAll(new WaitHandle[] { handle });
        }
    }
}
