//-----------------------------------------------------------------------
// <copyright file="JobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using BlueCollar.Configuration;

    /// <summary>
    /// Provides a base <see cref="IJobStore"/> implementation.
    /// </summary>
    public abstract class JobStore : IJobStore
    {
        private static readonly object locker = new object();
        private static IJobStore current;
        private string typeName;

        /// <summary>
        /// Gets the current <see cref="IJobStore"/> implementation in use.
        /// </summary>
        public static IJobStore Current
        {
            get
            {
                lock (locker)
                {
                    return current ?? (current = Create());
                }
            }
        }

        /// <summary>
        /// Gets this instance's full type name, including the assembly name but not including
        /// version or public key information.
        /// </summary>
        protected string TypeName
        {
            get
            {
                lock (this)
                {
                    if (this.typeName == null)
                    {
                        Type type = GetType();
                        this.typeName = String.Concat(type.FullName, ", ", type.Assembly.GetName().Name);
                    }

                    return this.typeName;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="IJobStore"/> instance.
        /// </summary>
        /// <returns>The created <see cref="IJobStore"/>.</returns>
        public static IJobStore Create()
        {
            return Create(BlueCollarSection.Current.Store);
        }

        /// <summary>
        /// Creates a new <see cref="IJobStore"/> instance.
        /// </summary>
        /// <param name="element">The configuration element to create the instance from.</param>
        /// <returns>The created <see cref="IJobStore"/>.</returns>
        public static IJobStore Create(JobStoreElement element)
        {
            IJobStore store = null;

            if (element != null && !String.IsNullOrEmpty(element.JobStoreType))
            {
                store = (IJobStore)Activator.CreateInstance(Type.GetType(element.JobStoreType));
            }
            else
            {
                // TODO: SQLite job store.
            }

            store.Initialize(element);
            return store;
        }

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A new <see cref="IJobStoreTransaction"/>.</returns>
        public abstract IJobStoreTransaction BeginTransaction();

        /// <summary>
        /// Delets all jobs in the job store.
        /// </summary>
        public virtual void DeleteAllJobs()
        {
            this.DeleteAllJobs(null);
        }

        /// <summary>
        /// Deletes all jobs in the job store.
        /// </summary>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public abstract void DeleteAllJobs(IJobStoreTransaction transaction);

        /// <summary>
        /// Deletes a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        public virtual void DeleteJob(int id)
        {
            this.DeleteJob(id, null);
        }

        /// <summary>
        /// Deletes a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public abstract void DeleteJob(int id, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to get.</param>
        /// <returns>The job with the given ID.</returns>
        public virtual JobRecord GetJob(int id)
        {
            return this.GetJob(id, null);
        }

        /// <summary>
        /// Gets a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The job with the given ID.</returns>
        public abstract JobRecord GetJob(int id, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets the number of jobs in the store that match the given filter.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <returns>The number of jobs that match the given filter.</returns>
        public virtual int GetJobCount(string likeName, JobStatus? withStatus, string inSchedule)
        {
            return this.GetJobCount(likeName, withStatus, inSchedule, null);
        }

        /// <summary>
        /// Gets the number of jobs in the store that match the given filter.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The number of jobs that match the given filter.</returns>
        public abstract int GetJobCount(string likeName, JobStatus? withStatus, string inSchedule, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of jobs that match the given collection of IDs.
        /// </summary>
        /// <param name="ids">The IDs of the jobs to get.</param>
        /// <returns>A collection of jobs.</returns>
        public virtual IEnumerable<JobRecord> GetJobs(IEnumerable<int> ids)
        {
            return this.GetJobs(ids, null);
        }

        /// <summary>
        /// Gets a collection of jobs that match the given collection of IDs.
        /// </summary>
        /// <param name="ids">The IDs of the jobs to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public abstract IEnumerable<JobRecord> GetJobs(IEnumerable<int> ids, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of jobs with the given status, returning
        /// at most the number of jobs identified by <paramref name="count"/>.
        /// </summary>
        /// <param name="status">The status of the jobs to get.</param>
        /// <param name="count">The maximum number of jobs to get.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <returns>A collection of jobs.</returns>
        public virtual IEnumerable<JobRecord> GetJobs(JobStatus status, int count, DateTime before)
        {
            return this.GetJobs(status, count, before, null);
        }

        /// <summary>
        /// Gets a collection of jobs with the given status, returning
        /// at most the number of jobs identified by <paramref name="count"/>.
        /// </summary>
        /// <param name="status">The status of the jobs to get.</param>
        /// <param name="count">The maximum number of jobs to get.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public abstract IEnumerable<JobRecord> GetJobs(JobStatus status, int count, DateTime before, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of jobs that match the given filter parameters, ordered by the given sort parameters.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="orderBy">A field to order the resultset by.</param>
        /// <param name="sortDescending">A value indicating whether to order the resultset in descending order.</param>
        /// <param name="pageNumber">The page number to get.</param>
        /// <param name="pageSize">The size of the pages to get.</param>
        /// <returns>A collection of jobs.</returns>
        public virtual IEnumerable<JobRecord> GetJobs(string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize)
        {
            return this.GetJobs(likeName, withStatus, inSchedule, orderBy, sortDescending, pageNumber, pageSize, null);
        }

        /// <summary>
        /// Gets a collection of jobs that match the given filter parameters, ordered by the given sort parameters.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="orderBy">A field to order the resultset by.</param>
        /// <param name="sortDescending">A value indicating whether to order the resultset in descending order.</param>
        /// <param name="pageNumber">The page number to get.</param>
        /// <param name="pageSize">The size of the pages to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public abstract IEnumerable<JobRecord> GetJobs(string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of the most recently scheduled persisted job for each
        /// scheduled job in the given collection.
        /// </summary>
        /// <param name="scheduleNames">A collection of schedule names to get the latest persisted jobs for.</param>
        /// <returns>A collection of recently scheduled jobs.</returns>
        public virtual IEnumerable<JobRecord> GetLatestScheduledJobs(IEnumerable<string> scheduleNames)
        {
            return this.GetLatestScheduledJobs(scheduleNames, null);
        }

        /// <summary>
        /// Gets a collection of the most recently scheduled persisted job for each
        /// scheduled job in the given collection.
        /// </summary>
        /// <param name="scheduleNames">A collection of schedule names to get the latest persisted jobs for.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of recently scheduled jobs.</returns>
        public abstract IEnumerable<JobRecord> GetLatestScheduledJobs(IEnumerable<string> scheduleNames, IJobStoreTransaction transaction);

        /// <summary>
        /// Initializes the job store from the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to initialize the job store from.</param>
        public virtual void Initialize(JobStoreElement element)
        {
        }

        /// <summary>
        /// Saves the given job record, either creating it or updating it.
        /// </summary>
        /// <param name="record">The job to save.</param>
        public virtual void SaveJob(JobRecord record)
        {
            this.SaveJob(record, null);
        }

        /// <summary>
        /// Saves the given job record, either creating it or updating it.
        /// </summary>
        /// <param name="record">The job to save.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public abstract void SaveJob(JobRecord record, IJobStoreTransaction transaction);
    }
}
