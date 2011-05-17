//-----------------------------------------------------------------------
// <copyright file="SqlServerJobStoreTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// SQL Server job store tests.
    /// </summary>
    [TestClass]
    public class SqlServerJobStoreTests : JobStoreTestBase
    {
        private static readonly string connectionString = ConfigurationManager.AppSettings["SqlServerConnectionString"];

        /// <summary>
        /// Initializes a new instance of the SqlServerJobStoreTests class.
        /// </summary>
        public SqlServerJobStoreTests()
            : base(CreateJobStore())
        {
        }

        /// <summary>
        /// Delete jobs tests.
        /// </summary>
        [TestMethod]
        public void SqlServerJobStoreDeleteJobs()
        {
            ExecuteDeleteJobs();
        }

        /// <summary>
        /// Get jobs tests.
        /// </summary>
        [TestMethod]
        public void SqlServerJobStoreGetJobs()
        {
            ExecuteGetJobs();
        }

        /// <summary>
        /// Save jobs tests.
        /// </summary>
        [TestMethod]
        public void SqlServerJobStoreSaveJobs()
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
                store = new SqlServerJobStore(connectionString);
                store.Initialize(null);
            }

            return store;
        }
    }
}
