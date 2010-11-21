//-----------------------------------------------------------------------
// <copyright file="TestFailRetryJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test.TestJobs
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Test fail + retry job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestFailRetryJob : Job
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test Retry Fail Job"; }
        }

        /// <summary>
        /// Gets the number of times the job can be retried if it fails.
        /// When not overridden, defaults to 0 (no retries).
        /// </summary>
        public override int Retries
        {
            get { return 1; }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
            if (this.TryNumber == 1)
            {
                throw new InvalidOperationException("This job only succeeds on its second try.");
            }
        }
    }
}
