//-----------------------------------------------------------------------
// <copyright file="JobRun.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using System.Threading;

    /// <summary>
    /// Represents a single, individually-threaded job run.
    /// </summary>
    public sealed class JobRun
    {
        private Thread executionThread;

        /// <summary>
        /// Initializes a new instance of the JobRun class.
        /// </summary>
        /// <param name="jobId">The ID of the job to run.</param>
        /// <param name="job">The job to run.</param>
        public JobRun(int jobId, IJob job)
        {
            if (jobId < 1)
            {
                throw new ArgumentOutOfRangeException("jobId", "jobId must be greater than 0.");
            }

            if (job == null)
            {
                throw new ArgumentNullException("job", "job must have a value.");
            }

            this.JobId = jobId;
            this.Job = job;
        }

        /// <summary>
        /// Initializes a new instance of the JobRun class.
        /// </summary>
        /// <param name="jobId">The ID of the job to run.</param>
        /// <param name="job">The job to run.</param>
        /// <param name="scheduleName">The name of the schedule to run the job for.</param>
        public JobRun(int jobId, IJob job, string scheduleName)
            : this(jobId, job)
        {
            this.ScheduleName = scheduleName;
        }

        /// <summary>
        /// Initializes a new instance of the JobRun class.
        /// </summary>
        /// <param name="persisted">The persisted job run to initialize this instance from.</param>
        public JobRun(PersistedJobRun persisted)
        {
            if (persisted == null)
            {
                throw new ArgumentNullException("persisted", "persisted cannot be null.");
            }

            Exception jobEx = null;

            if (!String.IsNullOrEmpty(persisted.JobType) && !String.IsNullOrEmpty(persisted.JobXml))
            {
                try
                {
                    this.Job = BlueCollar.Job.Deserialize(persisted.JobType, persisted.JobXml);
                }
                catch (Exception ex)
                {
                    jobEx = ex;
                }
            }

            this.ExecutionException = persisted.ExecutionException;
            this.FinishDate = persisted.FinishDate;
            this.JobId = persisted.JobId;
            this.ScheduleName = persisted.ScheduleName;
            this.StartDate = persisted.StartDate;
            this.WasRecovered = true;

            if (this.FinishDate == null)
            {
                this.FinishDate = DateTime.UtcNow;
            }

            if (this.ExecutionException == null)
            {
                this.ExecutionException = jobEx;
            }
        }

        /// <summary>
        /// Event fired when the job run has finished.
        /// </summary>
        public event EventHandler<JobRunEventArgs> Finished;

        /// <summary>
        /// Gets an exception that occurred during execution, if applicable.
        /// </summary>
        public Exception ExecutionException { get; private set; }

        /// <summary>
        /// Gets the date the run was finished, if applicable.
        /// </summary>
        public DateTime? FinishDate { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the run is currently in progress.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the job being run.
        /// </summary>
        public IJob Job { get; private set; }

        /// <summary>
        /// Gets the ID of the job being run.
        /// </summary>
        public int JobId { get; private set; }

        /// <summary>
        /// Gets the name of the schedule the job run is executing for.
        /// </summary>
        public string ScheduleName { get; private set; }

        /// <summary>
        /// Gets the date the job was started, if applicable.
        /// </summary>
        public DateTime? StartDate { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance was created via
        /// recovery from the running jobs persistenc file.
        /// </summary>
        public bool WasRecovered { get; private set; }

        /// <summary>
        /// Aborts the job run if it is currently in progress.
        /// </summary>
        /// <returns>True if the job was running and was aborted, false if 
        /// the job was not running and no abort was necessary.</returns>
        public bool Abort()
        {
            lock (this)
            {
                bool aborted = false;

                if (this.IsRunning)
                {
                    try
                    {
                        if (this.executionThread != null && this.executionThread.IsAlive)
                        {
                            this.executionThread.Abort();
                            this.executionThread = null;
                        }
                    }
                    catch (SecurityException)
                    {
                    }
                    catch (ThreadStateException)
                    {
                    }

                    this.IsRunning = false;
                    this.FinishDate = DateTime.UtcNow;
                    aborted = true;
                }

                return aborted;
            }
        }

        /// <summary>
        /// Starts the job if it has not already been run and it is not currently running.
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                if (!this.IsRunning && this.Job != null && this.FinishDate == null)
                {
                    this.IsRunning = true;
                    this.StartDate = DateTime.UtcNow;

                    this.executionThread = new Thread(this.StartInternal);
                    this.executionThread.Start();
                }
            }
        }

        /// <summary>
        /// Concrete job execution method.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch and act on all exceptions later.")]
        private void StartInternal()
        {
            try
            {
                this.Job.Execute();

                lock (this)
                {
                    this.IsRunning = false;
                    this.FinishDate = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    this.ExecutionException = ex;
                    this.IsRunning = false;
                    this.FinishDate = DateTime.UtcNow;
                }
            }
            finally
            {
                this.RaiseEvent(this.Finished, new JobRunEventArgs(this.JobId));
            }
        }
    }
}
