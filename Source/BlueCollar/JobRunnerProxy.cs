//-----------------------------------------------------------------------
// <copyright file="JobRunnerProxy.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides proxy access to the singleton <see cref="JobRunner"/> instance
    /// across application domains.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "False positive.")]
    public sealed class JobRunnerProxy : MarshalByRefObject
    {
        #region Private Fields

        private bool isGreen = true;

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the sink to use for raising events in a parent <see cref="AppDomain"/>.
        /// </summary>
        public JobRunnerEventSink EventSink { get; set; }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Pauses the job runner.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Not applicable in our (cross app-domain) scenario.")]
        public void PauseRunner()
        {
            JobRunner.DefaultRunner.Pause();
        }

        /// <summary>
        /// Starts the job runner.
        /// </summary>
        public void StartRunner()
        {
            JobRunner runner = JobRunner.DefaultRunner;

            if (this.isGreen)
            {
                runner.AllFinished += new EventHandler(this.JobRunnerAllFinished);
                runner.CancelJob += new EventHandler<JobRecordEventArgs>(this.JobRunnerCancelJob);
                runner.DequeueJob += new EventHandler<JobRecordEventArgs>(this.JobRunnerDequeueJob);
                runner.Error += new EventHandler<JobErrorEventArgs>(this.JobRunnerError);
                runner.ExecuteScheduledJob += new EventHandler<JobRecordEventArgs>(this.JobRunnerExecuteScheduledJob);
                runner.FinishJob += new EventHandler<JobRecordEventArgs>(this.JobRunnerFinishJob);
                runner.RetryEnqueued += new EventHandler<JobRecordEventArgs>(this.JobRunnerRetryEnqueued);
                runner.TimeoutJob += new EventHandler<JobRecordEventArgs>(this.JobRunnerTimeoutJob);

                this.isGreen = false;
            }

            runner.Start();
        }

        /// <summary>
        /// Stops the job runner by issuing a stop command and firing
        /// an <see cref="JobRunnerEventSink.AllFinished"/> event once all running jobs have finished executing.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Not applicable in our (cross app-domain) scenario.")]
        public void StopRunner()
        {
            JobRunner.DefaultRunner.Stop(true);
        }

        #endregion

        #region Private Instance Methods

        /// <summary>
        /// Raises the JobRunner's AllFinished event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerAllFinished(object sender, EventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireAllFinished();
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's CancelJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerCancelJob(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireCancelJob(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's DequeueJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerDequeueJob(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireDequeueJob(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's Error event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerError(object sender, JobErrorEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireError(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's ExecuteScheduledJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerExecuteScheduledJob(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireExecuteScheduledJob(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's FinishJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerFinishJob(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireFinishJob(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's RetryEnqueued event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerRetryEnqueued(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireRetryEnqueued(e);
                }
            }
        }

        /// <summary>
        /// Raises the JobRunner's TimeoutJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerTimeoutJob(object sender, JobRecordEventArgs e)
        {
            lock (this)
            {
                if (this.EventSink != null)
                {
                    this.EventSink.FireTimeoutJob(e);
                }
            }
        }

        #endregion
    }
}
