//-----------------------------------------------------------------------
// <copyright file="JobRecordEventArgs.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;

    /// <summary>
    /// Event arguments passed to <see cref="JobRunner"/> events.
    /// </summary>
    [Serializable]
    public class JobRecordEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the JobRecordEventArgs class.
        /// </summary>
        /// <param name="record">The <see cref="JobRecord"/> the event is being raised for.</param>
        public JobRecordEventArgs(JobRecord record)
        {
            this.Record = new JobRecord(record);
        }

        /// <summary>
        /// Gets or sets the <see cref="JobRecord"/> the event is being raised for.
        /// </summary>
        public JobRecord Record { get; protected set; }
    }
}
