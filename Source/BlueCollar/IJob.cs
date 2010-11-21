//-----------------------------------------------------------------------
// <copyright file="IJob.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines the interface for background jobs.
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the number of times the job can be retried if it fails.
        /// </summary>
        int Retries { get; }

        /// <summary>
        /// Gets the timeout, in miliseconds, the job is allowed to run for.
        /// </summary>
        long Timeout { get; }

        /// <summary>
        /// Gets or sets the try number the job is executing for.
        /// </summary>
        int TryNumber { get; set; }

        /// <summary>
        /// Creates a new job record representing an enqueue-able state for this instance.
        /// </summary>
        /// <returns>The created job record.</returns>
        JobRecord CreateRecord();

        /// <summary>
        /// Enqueues the job for execution.
        /// </summary>
        /// <returns>The job record that was persisted.</returns>
        JobRecord Enqueue();

        /// <summary>
        /// Enqueues the job for execution.
        /// </summary>
        /// <param name="store">The job store to use when queueing the job.</param>
        /// <returns>The job record that was persisted.</returns>
        JobRecord Enqueue(IJobStore store);

        /// <summary>
        /// Executes the job.
        /// </summary>
        void Execute();

        /// <summary>
        /// Serializes the job state for enqueueing.
        /// </summary>
        /// <returns>The serialized job data.</returns>
        string Serialize();
    }
}