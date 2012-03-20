//-----------------------------------------------------------------------
// <copyright file="MemoryJobStoreTests.cs" company="Tasty Codes">
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
    public class MemoryJobStoreTests : JobStoreTestBase
    {
        /// <summary>
        /// Initializes a new instance of the MemoryJobStoreTests class.
        /// </summary>
        public MemoryJobStoreTests()
            : base(CreateJobStore())
        {
        }

        /// <summary>
        /// Delete jobs tests.
        /// </summary>
        [TestMethod]
        public void MemoryJobStoreDeleteJobs()
        {
            ExecuteDeleteJobs();
        }

        /// <summary>
        /// Delete jobs older than tests.
        /// </summary>
        [TestMethod]
        public void MemoryJobStoreDeleteJobsOlderThan()
        {
            ExecuteDeleteJobsOlderThan();
        }

        /// <summary>
        /// Get jobs tests.
        /// </summary>
        [TestMethod]
        public void MemoryJobStoreGetJobs()
        {
            ExecuteGetJobs();
        }

        /// <summary>
        /// Save jobs tests.
        /// </summary>
        [TestMethod]
        public void MemoryStoreSaveJobs()
        {
            ExecuteSaveJobs();
        }

        /// <summary>
        /// Creates a new <see cref="IJobStore"/> instance for use with this test class.
        /// </summary>
        /// <returns>A <see cref="IJobStore"/> instance.</returns>
        private static IJobStore CreateJobStore()
        {
            IJobStore store = new MemoryJobStore();
            store.Initialize(null);
            return store;
        }
    }
}