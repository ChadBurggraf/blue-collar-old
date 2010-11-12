//-----------------------------------------------------------------------
// <copyright file="PostgresJobStoreTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// PostgreSQL job store tests.
    /// </summary>
    [TestClass]
    public class PostgresJobStoreTests : JobStoreTestBase
    {
        private static readonly string connectionString = ConfigurationManager.AppSettings["PostgresConnectionString"];

        /// <summary>
        /// Initializes a new instance of the PostgresJobStoreTests class.
        /// </summary>
        public PostgresJobStoreTests()
            : base(CreateJobStore())
        {
        }

        /// <summary>
        /// Delete jobs tests.
        /// </summary>
        [TestMethod]
        public void PostgresJobStoreDeleteJobs()
        {
            ExecuteDeleteJobs();
        }

        /// <summary>
        /// Get jobs tests.
        /// </summary>
        [TestMethod]
        public void PostgresJobStoreGetJobs()
        {
            ExecuteGetJobs();
        }

        /// <summary>
        /// Get latest scheduled jobs tests.
        /// </summary>
        [TestMethod]
        public void PostgresJobStoreGetLatestScheduledJobs()
        {
            ExecuteGetLatestScheduledJobs();
        }

        /// <summary>
        /// Save jobs tests.
        /// </summary>
        [TestMethod]
        public void PostgresJobStoreSaveJobs()
        {
            ExecuteSaveJobs();
        }

        /// <summary>
        /// Creates a new <see cref="IJobStore"/> instance for use with this test class.
        /// </summary>
        /// <returns>A <see cref="IJobStore"/> instance.</returns>
        private static IJobStore CreateJobStore()
        {
            IJobStore store = null;

            if (!String.IsNullOrEmpty(connectionString))
            {
                store = new PostgresJobStore(connectionString);
                store.Initialize(null);
            }

            return store;
        }
    }
}
