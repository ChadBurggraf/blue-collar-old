//-----------------------------------------------------------------------
// <copyright file="SQLiteJobStoreTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// SQLite job store tests.
    /// </summary>
    [TestClass]
    public class SQLiteJobStoreTests : JobStoreTestBase
    {
        /// <summary>
        /// Initializes a new instance of the SQLiteJobStoreTests class.
        /// </summary>
        public SQLiteJobStoreTests()
            : base(CreateJobStore())
        {
        }

        /// <summary>
        /// Delete jobs tests.
        /// </summary>
        [TestMethod]
        public void SQLiteJobStoreDeleteJobs()
        {
            ExecuteDeleteJobs();
        }

        /// <summary>
        /// Get jobs tests.
        /// </summary>
        [TestMethod]
        public void SQLiteJobStoreGetJobs()
        {
            ExecuteGetJobs();
        }

        /// <summary>
        /// Get latest scheduled jobs tests.
        /// </summary>
        [TestMethod]
        public void SQLiteJobStoreGetLatestScheduledJobs()
        {
            ExecuteGetLatestScheduledJobs();
        }

        /// <summary>
        /// Save jobs tests.
        /// </summary>
        [TestMethod]
        public void SQLiteJobStoreSaveJobs()
        {
            ExecuteSaveJobs();
        }

        /// <summary>
        /// Creates a new <see cref="IJobStore"/> instance for use with this test class.
        /// </summary>
        /// <returns>A <see cref="IJobStore"/> instance.</returns>
        private static IJobStore CreateJobStore()
        {
            IJobStore store = new SQLiteJobStore();
            store.Initialize(null);
            return store;
        }
    }
}