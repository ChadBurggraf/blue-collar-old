//-----------------------------------------------------------------------
// <copyright file="TestScheduledJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test.TestJobs
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Test scheduled job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestScheduledJob : ScheduledJob
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test Scheduled Job"; }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
        }
    }
}
