//-----------------------------------------------------------------------
// <copyright file="JobStatus.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System.ComponentModel;

    /// <summary>
    /// Defies the possible job status types.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Identifies an explicitly canceled job.
        /// </summary>
        Canceled,

        /// <summary>
        /// Identifies a job that is in the process of being canceled.
        /// </summary>
        Canceling,

        /// <summary>
        /// Identifies a failed job.
        /// </summary>
        Failed,

        /// <summary>
        /// Identifies a job that was interrupted during execution.
        /// </summary>
        Interrupted,

        /// <summary>
        /// Identifies a queued job.
        /// </summary>
        Queued,

        /// <summary>
        /// Identifies a job that has started.
        /// </summary>
        Started,

        /// <summary>
        /// Identifies a job that succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Identifies a job that has timed out.
        /// </summary>
        [Description("Timed Out")]
        TimedOut,

        /// <summary>
        /// Identifies that a job type instance failed to load when deserialized from the job store.
        /// </summary>
        [Description("Failed to Load Type")]
        FailedToLoadType
    }
}