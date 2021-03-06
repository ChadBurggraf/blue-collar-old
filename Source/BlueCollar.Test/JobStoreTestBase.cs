﻿//-----------------------------------------------------------------------
// <copyright file="JobStoreTestBase.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Linq;
    using BlueCollar.Configuration;
    using BlueCollar.Test.TestJobs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// Base class for <see cref="IJobStore"/> tests.
    /// </summary>
    public abstract class JobStoreTestBase
    {
        /// <summary>
        /// Initializes a new instance of the JobStoreTestBase class.
        /// </summary>
        /// <param name="store">The store to use for the tests.</param>
        protected JobStoreTestBase(IJobStore store)
        {
            this.Store = store;
        }

        /// <summary>
        /// Gets the store to use for the tests.
        /// </summary>
        protected IJobStore Store { get; private set; }

        /// <summary>
        /// Executes the delete jobs tests.
        /// </summary>
        protected virtual void ExecuteDeleteJobs()
        {
            if (this.Store != null)
            {
                IJobStoreTransaction trans;

                var job1 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);
                this.Store.SaveJob(job1);
                Assert.IsNotNull(this.Store.GetJob(job1.Id.Value));
                this.Store.DeleteJob(job1.Id.Value);
                Assert.IsNull(this.Store.GetJob(job1.Id.Value));

                var job2 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);
                this.Store.SaveJob(job2);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.DeleteJob(job2.Id.Value, trans);
                    trans.Rollback();
                    Assert.IsNotNull(this.Store.GetJob(job2.Id.Value));
                }

                var job3 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);
                this.Store.SaveJob(job3);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.DeleteJob(job3.Id.Value, trans);
                    trans.Commit();
                    Assert.IsNull(this.Store.GetJob(job3.Id.Value));
                }
            }
        }

        /// <summary>
        /// Executes the delete jobs older than tests.
        /// </summary>
        protected virtual void ExecuteDeleteJobsOlderThan()
        {
            if (this.Store != null)
            {
                IJobStoreTransaction trans;

                var job1 = this.CreateRecord(new TestIdJob(), JobStatus.Succeeded);
                this.Store.SaveJob(job1);
                
                var job2 = this.CreateRecord(new TestIdJob(), JobStatus.Succeeded);
                job2.QueueDate = DateTime.UtcNow.AddDays(-2);
                this.Store.SaveJob(job2);

                this.Store.DeleteJobs(DateTime.UtcNow.AddDays(-1));
                Assert.IsNotNull(this.Store.GetJob(job1.Id.Value));
                Assert.IsNull(this.Store.GetJob(job2.Id.Value));

                job2 = this.CreateRecord(new TestIdJob(), JobStatus.Succeeded);
                job2.QueueDate = DateTime.UtcNow.AddDays(-2);
                this.Store.SaveJob(job2);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.DeleteJobs(DateTime.UtcNow.AddDays(-1), trans);
                    trans.Rollback();
                    Assert.IsNotNull(this.Store.GetJob(job2.Id.Value));
                }

                job2 = this.CreateRecord(new TestIdJob(), JobStatus.Succeeded);
                job2.QueueDate = DateTime.UtcNow.AddDays(-2);
                this.Store.SaveJob(job2);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.DeleteJobs(DateTime.UtcNow.AddDays(-1), trans);
                    trans.Commit();
                    Assert.IsNull(this.Store.GetJob(job2.Id.Value));
                }
            }
        }

        /// <summary>
        /// Executes the get jobs tests.
        /// </summary>
        protected virtual void ExecuteGetJobs()
        {
            if (this.Store != null)
            {
                DateTime now = DateTime.MaxValue;

                int queuedCount = this.Store.GetJobs(JobStatus.Queued, 0, now).Count();
                int finishedCount = this.Store.GetJobs(JobStatus.Succeeded, 0, now).Count();
                int testIdJobCount = this.Store.GetJobCount(new TestIdJob().Name, null, null);

                var job1 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);
                var job2 = this.CreateRecord(new TestIdJob(), JobStatus.Succeeded);

                this.Store.SaveJob(job1);
                this.Store.SaveJob(job2);

                var jobs = this.Store.GetJobs(new int[] { job1.Id.Value, job2.Id.Value });

                Assert.AreEqual(2, jobs.Count());
                Assert.IsTrue(jobs.Any(j => j.Id.Value == job1.Id.Value));
                Assert.IsTrue(jobs.Any(j => j.Id.Value == job2.Id.Value));

                Assert.AreEqual(queuedCount + 1, this.Store.GetJobs(JobStatus.Queued, 0, now).Count());
                Assert.AreEqual(finishedCount + 1, this.Store.GetJobs(JobStatus.Succeeded, 0, now).Count());
                Assert.IsNotNull(this.Store.GetJob(job1.Id.Value));
                Assert.AreEqual(0, this.Store.GetJobs(new int[0]).Count());

                Assert.AreEqual(1, this.Store.GetJobs(null, null, null, JobRecordResultsOrderBy.QueueDate, true, 1, 1).Count());
                Assert.IsTrue(1 < this.Store.GetJobs(null, null, null, JobRecordResultsOrderBy.QueueDate, true, 1, 50).Count());
                Assert.AreEqual(testIdJobCount + 2, this.Store.GetJobCount(new TestIdJob().Name, null, null));
            }
        }

        /// <summary>
        /// Executes the save jobs tests.
        /// </summary>
        protected virtual void ExecuteSaveJobs()
        {
            if (this.Store != null)
            {
                IJobStoreTransaction trans;

                var job1 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);
                Assert.IsNull(job1.Id);
                this.Store.SaveJob(job1);
                Assert.IsNotNull(job1.Id);
                Assert.IsNotNull(this.Store.GetJob(job1.Id.Value));

                var job2 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.SaveJob(job2, trans);
                    trans.Rollback();

                    if (job2.Id != null)
                    {
                        Assert.IsNull(this.Store.GetJob(job2.Id.Value));
                    }
                }

                var job3 = this.CreateRecord(new TestIdJob(), JobStatus.Queued);

                using (trans = this.Store.BeginTransaction())
                {
                    this.Store.SaveJob(job3, trans);
                    trans.Commit();
                    Assert.IsNotNull(job3.Id);
                    Assert.IsNotNull(this.Store.GetJob(job3.Id.Value));
                }
            }
        }

        /// <summary>
        /// Creates and saves a <see cref="JobRecord"/> indicating that a job succeeded for a schedule.
        /// </summary>
        /// <param name="job">The job to create the record with.</param>
        /// <param name="queueDate">The queue date to create the record with.</param>
        /// <param name="scheduleName">The schedule name to create the record with.</param>
        /// <returns>The created record.</returns>
        private JobRecord CreateAndSaveScheduledSucceeded(IJob job, DateTime queueDate, string scheduleName)
        {
            JobRecord record = this.CreateRecord(job, JobStatus.Succeeded);
            record.QueueDate = queueDate;
            record.ScheduleName = scheduleName;
            this.Store.SaveJob(record);

            return record;
        }

        /// <summary>
        /// Creates a new <see cref="JobRecord"/> for testing with.
        /// </summary>
        /// <param name="job">The job to create the record with.</param>
        /// <param name="status">The status to create the record with.</param>
        /// <returns>The created record.</returns>
        private JobRecord CreateRecord(IJob job, JobStatus status)
        {
            JobRecord record = job.CreateRecord();
            record.Status = status;

            return record;
        }
    }
}
