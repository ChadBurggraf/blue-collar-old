//-----------------------------------------------------------------------
// <copyright file="RunningJobsTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.IO;
    using BlueCollar.Test.TestJobs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Running jobs tests.
    /// </summary>
    [TestClass]
    public class RunningJobsTests
    {
        private static string persistencPath = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString().Hash() + ".xml");

        /// <summary>
        /// Flush tests.
        /// </summary>
        [TestMethod]
        public void RunningJobsFlush()
        {
            RunningJobs runs = new RunningJobs(persistencPath);

            if (File.Exists(runs.PersistencePath))
            {
                File.Delete(runs.PersistencePath);
            }

            runs.Add(new JobRun(1, new TestIdJob()));
            runs.Add(new JobRun(2, new TestIdJob()));
            runs.Flush();

            Assert.IsTrue(File.Exists(runs.PersistencePath));
        }

        /// <summary>
        /// Load tests.
        /// </summary>
        [TestMethod]
        public void RunningJobsLoad()
        {
            RunningJobs runs = new RunningJobs(persistencPath);

            if (File.Exists(runs.PersistencePath))
            {
                File.Delete(runs.PersistencePath);
            }

            runs.Add(new JobRun(1, new TestIdJob()));
            runs.Add(new JobRun(2, new TestIdJob()));
            runs.Flush();

            runs = new RunningJobs(persistencPath);
            Assert.AreEqual(2, runs.Count);
        }
    }
}
