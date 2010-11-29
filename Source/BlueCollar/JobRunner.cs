//-----------------------------------------------------------------------
// <copyright file="JobRunner.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Threading;
    using BlueCollar.Configuration;

    /// <summary>
    /// Runs jobs.
    /// </summary>
    public sealed class JobRunner : IDisposable
    {
        #region Private Fields

        private readonly object stateLocker = new object();
        private readonly object runLocker = new object();
        private ReadOnlyCollection<JobScheduleElement> schedules;
        private IEnumerable<ScheduledJobTuple> scheduledJobs;
        private RunningJobs runs;
        private IJobStore store;
        private Thread god;
        private bool disposed;
        private bool deleteRecordsOnSuccess;
        private int heartbeat, maximumConcurrency, retryTimeout;
        private DateTime? lastScheduleCheck;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the JobRunner class.
        /// </summary>
        public JobRunner()
            : this(JobStore.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JobRunner class.
        /// </summary>
        /// <param name="store">The <see cref="IJobStore"/> to use when accessing job data.</param>
        public JobRunner(IJobStore store)
            : this(store, BlueCollarSection.Current.PersistencePath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JobRunner class.
        /// </summary>
        /// <param name="store">The <see cref="IJobStore"/> to use when accessing job data.</param>
        /// <param name="runningJobsPersistencePath">The path to use when saving running jobs state to disk.</param>
        public JobRunner(IJobStore store, string runningJobsPersistencePath)
            : this(store, runningJobsPersistencePath, BlueCollarSection.Current.Store.DeleteRecordsOnSuccess, BlueCollarSection.Current.Heartbeat, BlueCollarSection.Current.MaximumConcurrency, BlueCollarSection.Current.RetryTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JobRunner class.
        /// </summary>
        /// <param name="store">The <see cref="IJobStore"/> to use when accessing job data.</param>
        /// <param name="runningJobsPersistencePath">The path to use when saving running jobs state to disk.</param>
        /// <param name="deleteRecordsOnSuccess">A value indicating whether to delete records after successful job runs.</param>
        /// <param name="heartbeat">The heartbeat, in milliseconds, to poll for jobs with.</param>
        /// <param name="maximumConcurrency">The maximum number of simultaneous jobs to run.</param>
        /// <param name="retryTimeout">The timeout, in milliseconds, to use for failed job retries.</param>
        public JobRunner(IJobStore store, string runningJobsPersistencePath, bool deleteRecordsOnSuccess, int heartbeat, int maximumConcurrency, int retryTimeout)
            : this(store, runningJobsPersistencePath, deleteRecordsOnSuccess, heartbeat, maximumConcurrency, retryTimeout, BlueCollarSection.Current.Schedules)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JobRunner class.
        /// </summary>
        /// <param name="store">The <see cref="IJobStore"/> to use when accessing job data.</param>
        /// <param name="runningJobsPersistencePath">The path to use when saving running jobs state to disk.</param>
        /// <param name="deleteRecordsOnSuccess">A value indicating whether to delete records after successful job runs.</param>
        /// <param name="heartbeat">The heartbeat, in milliseconds, to poll for jobs with.</param>
        /// <param name="maximumConcurrency">The maximum number of simultaneous jobs to run.</param>
        /// <param name="retryTimeout">The timeout, in milliseconds, to use for failed job retries.</param>
        /// <param name="schedules">The collection of scheduled jobs to run.</param>
        public JobRunner(IJobStore store, string runningJobsPersistencePath, bool deleteRecordsOnSuccess, int heartbeat, int maximumConcurrency, int retryTimeout, IEnumerable<JobScheduleElement> schedules)
        {
            this.store = store ?? JobStore.Current;
            this.runs = new RunningJobs(runningJobsPersistencePath);
            this.deleteRecordsOnSuccess = deleteRecordsOnSuccess;
            this.heartbeat = heartbeat < 1 ? 10000 : heartbeat;
            this.maximumConcurrency = maximumConcurrency < 1 ? 25 : maximumConcurrency;
            this.retryTimeout = retryTimeout < 1 ? 60000 : retryTimeout;
            this.SetSchedules(schedules);
        }

        #endregion

        #region Destruction

        /// <summary>
        /// Finalizes an instance of the JobRunner class.
        /// </summary>
        ~JobRunner()
        {
            this.Dispose(false);
        }

        #endregion

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

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets a value indicating whether to delete records after successful job runs.
        /// </summary>
        public bool DeleteRecordsOnSuccess
        {
            get
            {
                lock (this.stateLocker)
                {
                    return this.deleteRecordsOnSuccess;
                }
            }

            set
            {
                lock (this.stateLocker)
                {
                    this.deleteRecordsOnSuccess = value;
                }
            }
        }

        /// <summary>
        /// Gets the number of jobs currently being executed by the runner.
        /// This number may reflect jobs that have finished but have yet to
        /// be flushed.
        /// </summary>
        public int ExecutingJobCount
        {
            get 
            {
                lock (this.runLocker)
                {
                    return this.runs.Count;
                }
            }
        }

        /// <summary>
        /// Gets or sets the run loop heartbeat, in miliseconds.
        /// </summary>
        public int Heartbeat
        {
            get
            {
                lock (this.stateLocker)
                {
                    return this.heartbeat;
                }
            }

            set
            {
                lock (this.stateLocker)
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("value must be greater than 0.", "value");
                    }

                    this.heartbeat = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the runner is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the running is in the process of shutting down.
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        /// <summary>
        /// Gets or sets the maximum number of simultaneous jobs to run.
        /// </summary>
        public int MaximumConcurrency
        {
            get
            {
                lock (this.stateLocker)
                {
                    return this.maximumConcurrency;
                }
            }

            set
            {
                lock (this.stateLocker)
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("value must be greater than or equal to 0.", "value");
                    }

                    this.maximumConcurrency = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the timeout, in milliseconds, to wait before retrying a failed job.
        /// </summary>
        public int RetryTimeout
        {
            get
            {
                lock (this.stateLocker)
                {
                    return this.retryTimeout;
                }
            }

            set
            {
                lock (this.stateLocker)
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("value must be greater than or equal to 0.", "value");
                    }

                    this.retryTimeout = value;
                }
            }
        }

        /// <summary>
        /// Gets a collection of tuples representing scheduled jobs.
        /// </summary>
        public IEnumerable<ScheduledJobTuple> ScheduledJobs
        {
            get
            {
                lock (this.stateLocker)
                {
                    if (this.scheduledJobs == null)
                    {
                        this.scheduledJobs = (from s in this.Schedules
                                              from j in s.ScheduledJobs
                                              select new ScheduledJobTuple()
                                              {
                                                  Schedule = s,
                                                  ScheduledJob = j
                                              }).ToArray();
                    }

                    return this.scheduledJobs;
                }
            }
        }

        /// <summary>
        /// Gets the schedule collection to use when processing scheduled jobs.
        /// </summary>
        public ReadOnlyCollection<JobScheduleElement> Schedules
        {
            get
            {
                lock (this.stateLocker)
                {
                    return this.schedules ?? (this.schedules = new ReadOnlyCollection<JobScheduleElement>(new List<JobScheduleElement>()));
                }
            }
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Disposes of resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Pauses the job runner by preventing any new jobs
        /// from being dequeued. Does not abort any currently running jobs.
        /// </summary>
        public void Pause()
        {
            lock (this.stateLocker)
            {
                this.IsRunning = false;
            }
        }

        /// <summary>
        /// Sets the schedule collection to use when processing scheduled jobs.
        /// </summary>
        /// <param name="schedules">A collection of schedules.</param>
        public void SetSchedules(IEnumerable<JobScheduleElement> schedules)
        {
            lock (this.stateLocker)
            {
                this.schedules = new ReadOnlyCollection<JobScheduleElement>(schedules != null ? schedules.ToList() : new List<JobScheduleElement>());
                this.scheduledJobs = null;
            }
        }

        /// <summary>
        /// Starts the runner if it is not already running.
        /// </summary>
        public void Start()
        {
            lock (this.stateLocker)
            {
                if (!this.IsShuttingDown)
                {
                    this.IsRunning = true;

                    if (this.god == null || !this.god.IsAlive)
                    {
                        this.god = new Thread(this.SmiteThee);
                        this.god.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Stops the runner if it is running.
        /// </summary>
        /// <param name="safely">A value indicating whether to safely shut down, allowing all currently
        /// running jobs to complete, or immediately terminate all running jobs.</param>
        public void Stop(bool safely)
        {
            bool abort = false;

            lock (this.stateLocker)
            {
                if (!safely || !this.IsShuttingDown)
                {
                    this.IsShuttingDown = true;
                    this.IsRunning = false;
                    abort = !safely;
                }
            }

            if (abort)
            {
                lock (this.runLocker)
                {
                    this.runs.AbortAll();
                    this.runs.Clear();
                    this.runs.Flush();
                }

                if (this.god != null && this.god.IsAlive)
                {
                    try
                    {
                        this.god.Abort();
                        this.god = null;
                    }
                    catch (SecurityException)
                    {
                    }
                    catch (ThreadStateException)
                    {
                    }
                }

                lock (this.stateLocker)
                {
                    this.IsShuttingDown = false;
                }
            }
        }

        #endregion

        #region Private Instance Methods

        /// <summary>
        /// Cancels any jobs marked as <see cref="JobStatus.Canceling"/>.
        /// </summary>
        private void CancelJobs()
        {
            using (IJobStoreTransaction trans = this.store.BeginTransaction())
            {
                try
                {
                    var runs = this.runs.GetRunning();
                    var records = this.store.GetJobs(runs.Select(r => r.JobId), trans);

                    var canceling = from run in runs
                                    join record in records on run.JobId equals record.Id.Value
                                    where record.Status == JobStatus.Canceling
                                    select new
                                    {
                                        Run = run,
                                        Record = record
                                    };

                    foreach (var job in canceling)
                    {
                        job.Run.Abort();
                        job.Record.Status = JobStatus.Canceled;
                        job.Record.FinishDate = job.Run.FinishDate;

                        this.store.SaveJob(job.Record, trans);
                        this.runs.Remove(job.Record.Id.Value);

                        this.RaiseEvent(this.CancelJob, new JobRecordEventArgs(job.Record));
                    }

                    this.runs.Flush();
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Cleans up any recovered jobs in this instance's <see cref="RunningJobs"/>.
        /// </summary>
        private void CleanupRecoveredJobs()
        {
            var runs = this.runs.GetAll().Where(r => r.WasRecovered);
            var records = this.store.GetJobs(runs.Select(r => r.JobId));

            var recovered = from run in runs
                            join record in records on run.JobId equals record.Id.Value
                            select new
                            {
                                Run = run,
                                Record = record
                            };

            foreach (var job in recovered)
            {
                job.Record.Status = JobStatus.Interrupted;

                if (job.Run.ExecutionException != null)
                {
                    job.Record.Exception = new ExceptionXElement(job.Run.ExecutionException).ToString();
                }

                this.store.SaveJob(job.Record);
            }

            this.runs.Clear();
            this.runs.Flush();
        }

        /// <summary>
        /// Dequeues pending jobs in the job store.
        /// </summary>
        private void DequeueJobs()
        {
            int count = this.MaximumConcurrency - this.ExecutingJobCount;

            if (count > 0)
            {
                DateTime now = DateTime.UtcNow;

                using (IJobStoreTransaction trans = this.store.BeginTransaction())
                {
                    try
                    {
                        foreach (var record in this.store.GetJobs(JobStatus.Queued, count, now, trans))
                        {
                            record.Status = JobStatus.Started;
                            record.StartDate = now;

                            IJob job = null;
                            Exception toJobEx = null;

                            try
                            {
                                job = record.ToJob();
                            }
                            catch (InvalidOperationException ex)
                            {
                                toJobEx = ex.InnerException ?? ex;
                                this.RaiseEvent(this.Error, new JobErrorEventArgs(record, toJobEx));
                            }

                            if (job != null)
                            {
                                JobRun run = new JobRun(record.Id.Value, job);
                                run.Finished += new EventHandler<JobRunEventArgs>(this.JobRunFinished);
                                this.runs.Add(run);

                                run.Start();
                            }
                            else
                            {
                                record.Status = JobStatus.FailedToLoadType;
                                record.FinishDate = now;
                                record.Exception = new ExceptionXElement(toJobEx).ToString();
                            }

                            this.store.SaveJob(record, trans);
                            this.RaiseEvent(this.DequeueJob, new JobRecordEventArgs(record));
                        }

                        this.runs.Flush();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether to dispose of managed resources.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.runs != null)
                    {
                        this.runs.AbortAll();
                        this.runs.Flush();
                        this.runs = null;
                    }

                    if (this.god != null && this.god.IsAlive)
                    {
                        this.god.Abort();
                        this.god = null;
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Re-enqueues a job for a retry if more retries are available.
        /// </summary>
        /// <param name="job">The job to re-enqueue.</param>
        /// <param name="trans">The transaction to use when saving the new job record, if applicable.</param>
        private void EnqueueJobForRetry(IJob job, IJobStoreTransaction trans)
        {
            if (job != null && job.TryNumber <= job.Retries)
            {
                JobRecord record = job.CreateRecord();
                record.TryNumber = job.TryNumber + 1;
                record.QueueDate = DateTime.UtcNow.AddMilliseconds(this.RetryTimeout);
                this.store.SaveJob(record, trans);

                this.RaiseEvent(this.RetryEnqueued, new JobRecordEventArgs(record));
            }
        }

        /// <summary>
        /// Executes any scheduled jobs that are due.
        /// </summary>
        private void ExecuteScheduledJobs()
        {
            int count = this.MaximumConcurrency - this.ExecutingJobCount;

            if (count > 0)
            {
                DateTime now = DateTime.UtcNow;
                long heartbeat = this.lastScheduleCheck == null ? this.Heartbeat : (long)Math.Ceiling(now.Subtract(this.lastScheduleCheck.Value).TotalMilliseconds);
                this.lastScheduleCheck = now;

                var scheduleNames = this.Schedules.Select(s => s.Name);
                var records = this.store.GetLatestScheduledJobs(scheduleNames);
                var tuples = ScheduledJobTuple.GetExecutableTuples(this.ScheduledJobs, records, now, heartbeat, count);

                using (IJobStoreTransaction trans = this.store.BeginTransaction())
                {
                    try
                    {
                        foreach (ScheduledJobTuple tuple in tuples)
                        {
                            JobRecord record = ScheduledJob.CreateRecord(tuple.Schedule, tuple.ScheduledJob, now);
                            bool running = this.runs.GetAll().Any(
                                r => tuple.Schedule.Name.Equals(r.ScheduleName, StringComparison.OrdinalIgnoreCase) &&
                                     tuple.ScheduledJob.JobType.StartsWith(record.JobType, StringComparison.OrdinalIgnoreCase));

                            if (!running)
                            {
                                IJob job = null;
                                Exception toJobEx = null;

                                try
                                {
                                    job = ScheduledJob.CreateFromConfiguration(tuple.ScheduledJob);
                                }
                                catch (ConfigurationErrorsException ex)
                                {
                                    toJobEx = ex;
                                    this.RaiseEvent(this.Error, new JobErrorEventArgs(record, toJobEx));
                                }

                                if (job != null)
                                {
                                    record.Name = job.Name;
                                    record.JobType = JobRecord.JobTypeString(job);
                                    record.Data = job.Serialize();
                                    this.store.SaveJob(record, trans);

                                    JobRun run = new JobRun(record.Id.Value, job);
                                    run.Finished += new EventHandler<JobRunEventArgs>(this.JobRunFinished);
                                    this.runs.Add(run);

                                    run.Start();
                                }
                                else
                                {
                                    record.Status = JobStatus.FailedToLoadType;
                                    record.FinishDate = now;
                                    record.Exception = new ExceptionXElement(toJobEx).ToString();
                                }

                                this.store.SaveJob(record, trans);
                                this.RaiseEvent(this.ExecuteScheduledJob, new JobRecordEventArgs(record));
                            }
                        }

                        this.runs.Flush();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Performs the concrete finishing of the given job run.
        /// </summary>
        /// <param name="run">A job run to finish.</param>
        /// <param name="record">The run's related record.</param>
        /// <param name="trans">The transaction to access the job store in.</param>
        private void FinishJobRun(JobRun run, JobRecord record, IJobStoreTransaction trans)
        {
            record.FinishDate = run.FinishDate;

            if (run.ExecutionException != null)
            {
                record.Exception = new ExceptionXElement(run.ExecutionException).ToString();
                record.Status = JobStatus.Failed;

                this.RaiseEvent(this.Error, new JobErrorEventArgs(record, run.ExecutionException));
                this.EnqueueJobForRetry(run.Job, trans);
            }
            else if (run.WasRecovered)
            {
                record.Status = JobStatus.Interrupted;
            }
            else
            {
                record.Status = JobStatus.Succeeded;
            }

            if (this.DeleteRecordsOnSuccess)
            {
                this.store.DeleteJob(record.Id.Value, trans);
            }
            else
            {
                this.store.SaveJob(record, trans);
            }

            this.runs.Remove(record.Id.Value);
            this.RaiseEvent(this.FinishJob, new JobRecordEventArgs(record));
        }

        /// <summary>
        /// Finishes any jobs that have completed by updating their records in the job store.
        /// </summary>
        private void FinishJobs()
        {
            using (IJobStoreTransaction trans = this.store.BeginTransaction())
            {
                try
                {
                    var runs = this.runs.GetNotRunning();
                    var records = this.store.GetJobs(runs.Select(r => r.JobId), trans);

                    var finishing = from run in runs
                                    join record in records on run.JobId equals record.Id.Value
                                    where record.Status == JobStatus.Started
                                    select new
                                    {
                                        Run = run,
                                        Record = record
                                    };

                    foreach (var job in finishing)
                    {
                        this.FinishJobRun(job.Run, job.Record, trans);
                    }

                    this.runs.Flush();
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Raises a JobRun's Finished event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunFinished(object sender, JobRunEventArgs e)
        {
            lock (this.runLocker)
            {
                using (IJobStoreTransaction trans = this.store.BeginTransaction())
                {
                    try
                    {
                        var run = this.runs.GetAll().Where(r => r.JobId == e.JobId).FirstOrDefault();

                        if (run != null)
                        {
                            var record = this.store.GetJob(e.JobId, trans);

                            if (record != null)
                            {
                                this.FinishJobRun(run, record, trans);
                            }
                        }

                        trans.Commit();
                        this.runs.Flush();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Main God thread run loop.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want the run loop to continue at all costs.")]
        private void SmiteThee()
        {
            lock (this.runLocker)
            {
                try
                {
                    this.CleanupRecoveredJobs();
                }
                catch (Exception ex)
                {
                    this.RaiseEvent(this.Error, new JobErrorEventArgs(null, ex));
                }
            }

            while (true)
            {
                try
                {
                    lock (this.runLocker)
                    {
                        this.CancelJobs();
                        this.TimeoutJobs();
                        this.FinishJobs();

                        lock (this.stateLocker)
                        {
                            if (this.IsRunning)
                            {
                                this.ExecuteScheduledJobs();
                                this.DequeueJobs();
                            }
                            else if (this.IsShuttingDown && this.ExecutingJobCount == 0)
                            {
                                this.IsShuttingDown = false;
                                this.RaiseEvent(this.AllFinished, EventArgs.Empty);
                                break;
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.RaiseEvent(this.Error, new JobErrorEventArgs(null, ex));
                }

                Thread.Sleep(this.Heartbeat);
            }
        }

        /// <summary>
        /// Times out any currently running jobs that have been running for too long.
        /// </summary>
        private void TimeoutJobs()
        {
            using (IJobStoreTransaction trans = this.store.BeginTransaction())
            {
                try
                {
                    var runs = this.runs.GetRunning();
                    var records = this.store.GetJobs(runs.Select(r => r.JobId), trans);

                    var timingOut = from run in runs
                                    join record in records on run.JobId equals record.Id.Value
                                    where run.Job != null
                                        && run.StartDate != null
                                        && DateTime.UtcNow.Subtract(run.StartDate.Value).TotalMilliseconds >= run.Job.Timeout
                                        && record.Status == JobStatus.Started
                                    select new
                                    {
                                        Run = run,
                                        Record = record
                                    };

                    foreach (var job in timingOut)
                    {
                        if (job.Run.Abort())
                        {
                            job.Record.Status = JobStatus.TimedOut;
                            job.Record.FinishDate = job.Run.FinishDate;

                            this.store.SaveJob(job.Record, trans);
                            this.runs.Remove(job.Record.Id.Value);

                            this.EnqueueJobForRetry(job.Run.Job, trans);
                            this.RaiseEvent(this.TimeoutJob, new JobRecordEventArgs(job.Record));
                        }
                    }

                    this.runs.Flush();
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        #endregion
    }
}
