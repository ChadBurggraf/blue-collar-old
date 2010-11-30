//-----------------------------------------------------------------------
// <copyright file="JobsInProcessModule.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Web;
    using System.Web.Caching;

    /// <summary>
    /// Implements <see cref="IHttpModule"/> for maintaining a <see cref="JobRunner"/> in-process
    /// in ASP.NET web applications.
    /// </summary>
    public sealed class JobsInProcessModule : IHttpModule
    {
        private const string CacheKey = "BlueCollar.JobsInProcessModule.KeepAlive";
        private const int KeepAliveTimeoutSeconds = 30;
        private static readonly object cacheLocker = new object();
        private static readonly object runnerLocker = new object();
        private static JobRunner runner;

        /// <summary>
        /// Event raised when the runner has finished safely shutting down
        /// there are no jobs currently running.
        /// </summary>
        public static event EventHandler AllFinished;

        /// <summary>
        /// Event raised when a job has been canceled and and its run terminated.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> CancelJob;

        /// <summary>
        /// Event raised when a job has been dequeued from the persistent store
        /// and its run started.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> DequeueJob;

        /// <summary>
        /// Event raised when an error occurs.
        /// WARNING: The <see cref="JobRecord"/> passed with this event may be empty,
        /// or the <see cref="Exception"/> may be null, or both.
        /// </summary>
        public static event EventHandler<JobErrorEventArgs> Error;

        /// <summary>
        /// Event raised when a sceduled job loaded for execution.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> ExecuteScheduledJob;

        /// <summary>
        /// Event raised when a job has finished execution.
        /// This event is raised when the job finished naturally (i.e., not by canceling or timing out),
        /// but regardless of whether it succeeded or not.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> FinishJob;

        /// <summary>
        /// Event raised when a failed or timed out job is enqueued for a retry.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> RetryEnqueued;

        /// <summary>
        /// Event raised when a job has been timed out.
        /// </summary>
        public static event EventHandler<JobRecordEventArgs> TimeoutJob;

        /// <summary>
        /// Gets the application's <see cref="JobRunner"/> instance used by the module.
        /// </summary>
        public static JobRunner Runner
        {
            get
            {
                lock (runnerLocker)
                {
                    if (runner == null)
                    {
                        runner = new JobRunner();
                        runner.AllFinished += new EventHandler(RunnerAllFinished);
                        runner.CancelJob += new EventHandler<JobRecordEventArgs>(RunnerCancelJob);
                        runner.DequeueJob += new EventHandler<JobRecordEventArgs>(RunnerDequeueJob);
                        runner.Error += new EventHandler<JobErrorEventArgs>(RunnerError);
                        runner.ExecuteScheduledJob += new EventHandler<JobRecordEventArgs>(RunnerExecuteScheduledJob);
                        runner.FinishJob += new EventHandler<JobRecordEventArgs>(RunnerFinishJob);
                        runner.RetryEnqueued += new EventHandler<JobRecordEventArgs>(RunnerRetryEnqueued);
                        runner.TimeoutJob += new EventHandler<JobRecordEventArgs>(RunnerTimeoutJob);
                    }

                    return runner;
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by this instance.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">An <see cref="HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application.</param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(ContextBeginRequest);
        }

        /// <summary>
        /// Callback for the keep-alive's cache item removed event.
        /// </summary>
        /// <param name="key">The key whose cache item was removed.</param>
        /// <param name="value">The cache item's value that was removed.</param>
        /// <param name="reason">The reason for removal.</param>
        private static void CacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            EnsureKeepAlive();
            Runner.Start();
        }

        /// <summary>
        /// Raises the context's PostAcquireRequestState event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void ContextBeginRequest(object sender, EventArgs e)
        {
            EnsureKeepAlive();
            Runner.Start();
        }

        /// <summary>
        /// Ensures that a keep alive is currently in cache.
        /// </summary>
        private static void EnsureKeepAlive()
        {
            lock (cacheLocker)
            {
                if (HttpRuntime.Cache[CacheKey] == null)
                {
                    HttpRuntime.Cache.Add(
                        CacheKey,
                        new object(),
                        null,
                        DateTime.Now.AddSeconds(KeepAliveTimeoutSeconds),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.NotRemovable,
                        new CacheItemRemovedCallback(CacheItemRemoved));
                }
            }
        }

        /// <summary>
        /// Raises the runner's AllFinished event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerAllFinished(object sender, EventArgs e)
        {
            if (AllFinished != null)
            {
                AllFinished(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's CancelJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerCancelJob(object sender, JobRecordEventArgs e)
        {
            if (CancelJob != null)
            {
                CancelJob(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's DequeueJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerDequeueJob(object sender, JobRecordEventArgs e)
        {
            if (DequeueJob != null)
            {
                DequeueJob(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's Error event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerError(object sender, JobErrorEventArgs e)
        {
            if (Error != null)
            {
                Error(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's ExecuteScheduledJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerExecuteScheduledJob(object sender, JobRecordEventArgs e)
        {
            if (ExecuteScheduledJob != null)
            {
                ExecuteScheduledJob(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's FinishJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerFinishJob(object sender, JobRecordEventArgs e)
        {
            if (FinishJob != null)
            {
                FinishJob(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's RetryEnqueued event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerRetryEnqueued(object sender, JobRecordEventArgs e)
        {
            if (RetryEnqueued != null)
            {
                RetryEnqueued(sender, e);
            }
        }

        /// <summary>
        /// Raises the runner's TimeoutJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void RunnerTimeoutJob(object sender, JobRecordEventArgs e)
        {
            if (TimeoutJob != null)
            {
                TimeoutJob(sender, e);
            }
        }
    }
}