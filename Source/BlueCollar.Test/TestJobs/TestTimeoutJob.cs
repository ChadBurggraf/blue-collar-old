//-----------------------------------------------------------------------
// <copyright file="TestTimeoutJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test.TestJobs
{
    using System;
    using System.Runtime.Serialization;
    using System.Threading;
    using BlueCollar.Configuration;

    /// <summary>
    /// Test timeout job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestTimeoutJob : Job
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test Timeout Job"; }
        }

        /// <summary>
        /// Gets the timeout, in miliseconds, the job is allowed to run for.
        /// </summary>
        public override long Timeout
        {
            get
            {
                return BlueCollarSection.Current.Heartbeat;
            }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
            Thread.Sleep(BlueCollarSection.Current.Heartbeat * 10);
        }
    }
}
