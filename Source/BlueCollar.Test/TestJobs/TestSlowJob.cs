//-----------------------------------------------------------------------
// <copyright file="TestSlowJob.cs" company="Tasty Codes">
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
    /// Test slow job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestSlowJob : Job
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test Slow Job"; }
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
