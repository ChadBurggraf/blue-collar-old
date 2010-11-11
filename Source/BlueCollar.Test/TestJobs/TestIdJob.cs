//-----------------------------------------------------------------------
// <copyright file="TestIdJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test.TestJobs
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Test ID serialization job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestIdJob : Job
    {
        /// <summary>
        /// Initializes a new instance of the TestIdJob class.
        /// </summary>
        public TestIdJob()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets the test ID.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test ID Job"; }
        }

        /// <summary>
        /// Gets the timeout, in miliseconds, the job is allowed to run for.
        /// </summary>
        public override long Timeout
        {
            get
            {
                return 10; // 10 ms.
            }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
        }
    }
}
