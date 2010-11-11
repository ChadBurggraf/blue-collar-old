﻿//-----------------------------------------------------------------------
// <copyright file="TestQuickJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test.TestJobs
{
    using System;
    using System.Runtime.Serialization;
    using System.Threading;

    /// <summary>
    /// Test quick job.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public class TestQuickJob : Job
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        public override string Name
        {
            get { return "Test Quick Job"; }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
            Thread.Sleep(100);
        }
    }
}
