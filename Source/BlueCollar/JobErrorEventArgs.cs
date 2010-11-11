//-----------------------------------------------------------------------
// <copyright file="JobErrorEventArgs.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;

    /// <summary>
    /// Event arguments passed to <see cref="JobRunner"/> error events.
    /// </summary>
    [Serializable]
    public class JobErrorEventArgs : JobRecordEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the JobErrorEventArgs class.
        /// </summary>
        /// <param name="record">The <see cref="JobRecord"/> the event is being raised for.</param>
        /// <param name="ex">The <see cref="Exception"/> that caused the error.</param>
        public JobErrorEventArgs(JobRecord record, Exception ex)
            : base(record)
        {
            this.Exception = ex;
        }

        /// <summary>
        /// Gets the <see cref="Exception"/> that caused the error.
        /// </summary>
        public Exception Exception { get; private set; }
    }
}
