//-----------------------------------------------------------------------
// <copyright file="JobRunnerEventSink.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides access to <see cref="JobRunner"/> and <see cref="JobRunnerProxy"/>
    /// events across <see cref="AppDomain"/> boundaries.
    /// </summary>
    [Serializable]
    public sealed class JobRunnerEventSink : MarshalByRefObject
    {
        #region Events

        /// <summary>
        /// Event raised when the runner has finished safely shutting down
        /// there are no jobs currently running.
        /// </summary>
        public event EventHandler AllFinished;

        /// <summary>
        /// Event raised when a job has been canceled and and its run terminated.
        /// </summary>
        public event EventHandler<JobRecordEventArgs> CancelJob;

        /// <summary>
        /// Event raised when a job has been dequeued from the persistent store
        /// and its run started.
        /// </summary>
        public event EventHandler<JobRecordEventArgs> DequeueJob;

        /// <summary>
        /// Event raised when an error occurs.
        /// WARNING: The <see cref="JobRecord"/> passed with this event may be empty,
        /// or the <see cref="Exception"/> may be null, or both.
        /// </summary>
        public event EventHandler<JobErrorEventArgs> Error;

        /// <summary>
        /// Event raised when a sceduled job loaded for execution.
        /// </summary>
        public event EventHandler<JobRecordEventArgs> ExecuteScheduledJob;

        /// <summary>
        /// Event raised when a job has finished execution.
        /// This event is raised when the job finished naturally (i.e., not by canceling or timing out),
        /// but regardless of whether it succeeded or not.
        /// </summary>
        public event EventHandler<JobRecordEventArgs> FinishJob;

        /// <summary>
        /// Event raised when a failed or timed out job is enqueued for a retry.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
        public event EventHandler<JobRecordEventArgs> RetryEnqueued;

        /// <summary>
        /// Event raised when a job has been timed out.
        /// </summary>
        public event EventHandler<JobRecordEventArgs> TimeoutJob;

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Fires <see cref="AllFinished"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireAllFinished()
        {
            this.RaiseEvent(this.AllFinished, EventArgs.Empty);
        }

        /// <summary>
        /// Fires <see cref="CancelJob"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireCancelJob(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.CancelJob, e);
        }

        /// <summary>
        /// Fires <see cref="DequeueJob"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireDequeueJob(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.DequeueJob, e);
        }

        /// <summary>
        /// Fires <see cref="Error"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireError(JobErrorEventArgs e)
        {
            this.RaiseEvent(this.Error, e);
        }

        /// <summary>
        /// Fires <see cref="ExecuteScheduledJob"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireExecuteScheduledJob(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.ExecuteScheduledJob, e);
        }

        /// <summary>
        /// Fires <see cref="FinishJob"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireFinishJob(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.FinishJob, e);
        }

        /// <summary>
        /// Fires <see cref="RetryEnqueued"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
        public void FireRetryEnqueued(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.RetryEnqueued, e);
        }

        /// <summary>
        /// Fires <see cref="TimeoutJob"/> in this instance's original <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate.")]
        public void FireTimeoutJob(JobRecordEventArgs e)
        {
            this.RaiseEvent(this.TimeoutJob, e);
        }

        #endregion
    }
}
