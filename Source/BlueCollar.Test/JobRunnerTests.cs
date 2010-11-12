//-----------------------------------------------------------------------
// <copyright file="JobRunnerTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Configuration;
    using System.Threading;
    using BlueCollar.Configuration;
    using BlueCollar.Test.TestJobs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// Job runner tests.
    /// </summary>
    [TestClass]
    public class JobRunnerTests
    {
        private const int heartbeat = 1000;
        private IJobStore jobStore;
        private JobRunner jobRunner;

        /// <summary>
        /// Initializes a new instance of the JobRunnerTests class.
        /// </summary>
        public JobRunnerTests()
        {
            //this.jobStore = new SQLiteJobStore("data source=" + Guid.NewGuid().ToString() + ".s3db");
            //this.jobStore = new MemoryJobStore();
            this.jobStore = new SqlServerJobStore(ConfigurationManager.AppSettings["SqlServerConnectionString"]);
            this.jobStore.Initialize(null);
            this.jobStore.DeleteAllJobs();

            this.jobRunner = new JobRunner(this.jobStore, Guid.NewGuid().ToString() + ".xml");
            this.jobRunner.Heartbeat = heartbeat;
            this.jobRunner.MaximumConcurrency = 25;
            this.jobRunner.Error += new EventHandler<JobErrorEventArgs>(this.JobRunnerError);
            this.jobRunner.Start();
        }

        /// <summary>
        /// Cancel jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerCancelJobs()
        {
            this.jobRunner.Start();

            var id = new TestSlowJob().Enqueue(this.jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            var record = this.jobStore.GetJob(id);
            Assert.AreEqual(JobStatus.Started, record.Status);

            record.Status = JobStatus.Canceling;
            this.jobStore.SaveJob(record);
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Canceled, this.jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Dequeue jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerDequeueJobs()
        {
            this.jobRunner.Start();

            var id = new TestSlowJob().Enqueue(this.jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Started, this.jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Execute scheduled jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerExecuteScheduledJobs()
        {
            JobScheduleElement sched1 = new JobScheduleElement()
            {
                Name = "___TEST_SCHED_1___" + Guid.NewGuid().ToString(),
                RepeatHours = 24,
                StartOn = DateTime.UtcNow.AddYears(-1)
            };

            JobScheduleElement sched2 = new JobScheduleElement()
            {
                Name = "___TEST_SCHED_2___" + Guid.NewGuid().ToString(),
                RepeatHours = .5,
                StartOn = DateTime.UtcNow.AddDays(-1)
            };

            JobScheduleElement sched3 = new JobScheduleElement()
            {
                Name = "___TEST_SCHED_3___" + Guid.NewGuid().ToString(),
                RepeatHours = .5,
                StartOn = DateTime.UtcNow.AddDays(1)
            };

            JobScheduledJobElement job1 = new JobScheduledJobElement()
            {
                JobType = JobRecord.JobTypeString(typeof(TestIdJob))
            };

            JobScheduledJobElement job2 = new JobScheduledJobElement()
            {
                JobType = JobRecord.JobTypeString(typeof(TestScheduledJob))
            };

            sched1.ScheduledJobs.Add(job1);
            sched1.ScheduledJobs.Add(job2);
            sched2.ScheduledJobs.Add(job2);
            sched3.ScheduledJobs.Add(job1);

            this.jobRunner.SetSchedules(new JobScheduleElement[] { sched1, sched2, sched3 });
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(2, this.jobStore.GetJobCount(null, null, sched1.Name));
            Assert.AreEqual(1, this.jobStore.GetJobCount(null, null, sched2.Name));
            Assert.AreEqual(0, this.jobStore.GetJobCount(null, null, sched3.Name));
        }

        /// <summary>
        /// Finish jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerFinishJobs()
        {
            this.jobRunner.Start();

            var id = new TestQuickJob().Enqueue(this.jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Succeeded, this.jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Timeout jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerTimeoutJobs()
        {
            this.jobRunner.Start();

            var id = new TestTimeoutJob().Enqueue(this.jobStore).Id.Value;
            Thread.Sleep(heartbeat * 3);

            Assert.AreEqual(JobStatus.TimedOut, this.jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Raises the job runner's Error event.
        /// </summary>
        /// <param name="sender">The evnt sender.</param>
        /// <param name="e">The event arguments.</param>
        private void JobRunnerError(object sender, JobErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                Assert.Fail(e.Exception.Message + "\n" + e.Exception.StackTrace);
            }

            Assert.Fail(Environment.StackTrace);
        }
    }
}
