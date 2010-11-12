//-----------------------------------------------------------------------
// <copyright file="IJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using BlueCollar.Configuration;

    /// <summary>
    /// Defines the interface for persistent job stores.
    /// </summary>
    public interface IJobStore
    {
        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A new <see cref="IJobStoreTransaction"/>.</returns>
        IJobStoreTransaction BeginTransaction();

        /// <summary>
        /// Delets all jobs in the job store.
        /// </summary>
        void DeleteAllJobs();

        /// <summary>
        /// Deletes all jobs in the job store.
        /// </summary>
        /// <param name="transaction">The transaction to execute the command in.</param>
        void DeleteAllJobs(IJobStoreTransaction transaction);

        /// <summary>
        /// Deletes a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        void DeleteJob(int id);

        /// <summary>
        /// Deletes a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        void DeleteJob(int id, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to get.</param>
        /// <returns>The job with the given ID.</returns>
        JobRecord GetJob(int id);

        /// <summary>
        /// Gets a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The job with the given ID.</returns>
        JobRecord GetJob(int id, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets the number of jobs in the store that match the given filter.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <returns>The number of jobs that match the given filter.</returns>
        int GetJobCount(string likeName, JobStatus? withStatus, string inSchedule);

        /// <summary>
        /// Gets the number of jobs in the store that match the given filter.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The number of jobs that match the given filter.</returns>
        int GetJobCount(string likeName, JobStatus? withStatus, string inSchedule, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of jobs that match the given collection of IDs.
        /// </summary>
        /// <param name="ids">The IDs of the jobs to get.</param>
        /// <returns>A collection of jobs.</returns>
        IEnumerable<JobRecord> GetJobs(IEnumerable<int> ids);

        /// <summary>
        /// Gets a collection of jobs that match the given collection of IDs.
        /// </summary>
        /// <param name="ids">The IDs of the jobs to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        IEnumerable<JobRecord> GetJobs(IEnumerable<int> ids, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of jobs with the given status, returning
        /// at most the number of jobs identified by <paramref name="count"/>.
        /// </summary>
        /// <param name="status">The status of the jobs to get.</param>
        /// <param name="count">The maximum number of jobs to get.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <returns>A collection of jobs.</returns>
        IEnumerable<JobRecord> GetJobs(JobStatus status, int count, DateTime before);

        /// <summary>
        /// Gets a collection of jobs with the given status, returning
        /// at most the number of jobs identified by <paramref name="count"/>.
        /// </summary>
        /// <param name="status">The status of the jobs to get.</param>
        /// <param name="count">The maximum number of jobs to get.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        IEnumerable<JobRecord> GetJobs(JobStatus status, int count, DateTime before, IJobStoreTransaction transaction);

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
        IEnumerable<JobRecord> GetJobs(string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize);

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
        IEnumerable<JobRecord> GetJobs(string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize, IJobStoreTransaction transaction);

        /// <summary>
        /// Gets a collection of the most recently scheduled persisted job for each
        /// scheduled job in the given collection.
        /// </summary>
        /// <param name="scheduleNames">A collection of schedule names to get the latest persisted jobs for.</param>
        /// <returns>A collection of recently scheduled jobs.</returns>
        IEnumerable<JobRecord> GetLatestScheduledJobs(IEnumerable<string> scheduleNames);

        /// <summary>
        /// Gets a collection of the most recently scheduled persisted job for each
        /// scheduled job in the given collection.
        /// </summary>
        /// <param name="scheduleNames">A collection of schedule names to get the latest persisted jobs for.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of recently scheduled jobs.</returns>
        IEnumerable<JobRecord> GetLatestScheduledJobs(IEnumerable<string> scheduleNames, IJobStoreTransaction transaction);

        /// <summary>
        /// Initializes the job store from the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to initialize the job store from.</param>
        void Initialize(JobStoreElement element);

        /// <summary>
        /// Saves the given job record, either creating it or updating it.
        /// </summary>
        /// <param name="record">The job to save.</param>
        void SaveJob(JobRecord record);

        /// <summary>
        /// Saves the given job record, either creating it or updating it.
        /// </summary>
        /// <param name="record">The job to save.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        void SaveJob(JobRecord record, IJobStoreTransaction transaction);
    }
}
