//-----------------------------------------------------------------------
// <copyright file="JobRunEventArgs.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;

    /// <summary>
    /// Represents the event arguments for events raised by a <see cref="JobRun"/>.
    /// </summary>
    [Serializable]
    public class JobRunEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the JobRunEventArgs class.
        /// </summary>
        /// <param name="jobId">The ID of the job the event is being raised for.</param>
        public JobRunEventArgs(int jobId)
        {
            this.JobId = jobId;
        }

        /// <summary>
        /// Gets the ID of the job the job run event is being raised for.
        /// </summary>
        public int JobId { get; private set; }
    }
}
