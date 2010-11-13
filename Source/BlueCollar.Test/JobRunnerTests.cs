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
        private static int originalHeartbeat;
        private static IJobStore jobStore;
        private static JobRunner jobRunner;

        /// <summary>
        /// Cleans up resources used by the test run.
        /// </summary>
        [ClassCleanup]
        public static void Cleanup()
        {
            jobRunner.Stop(false);
            BlueCollarSection.Current.Heartbeat = originalHeartbeat;
        }

        /// <summary>
        /// Initializes the class for the given test context.
        /// </summary>
        /// <param name="context">The test context to initialize the class for.</param>
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            originalHeartbeat = BlueCollarSection.Current.Heartbeat;
            BlueCollarSection.Current.Heartbeat = heartbeat;

            jobStore = new MemoryJobStore();
            jobStore.Initialize(null);
            jobStore.DeleteAllJobs();

            jobRunner = new JobRunner(jobStore, Guid.NewGuid().ToString() + ".xml");
            jobRunner.Heartbeat = heartbeat;
            jobRunner.MaximumConcurrency = 25;
            jobRunner.SetSchedules(null);
            jobRunner.Error += new EventHandler<JobErrorEventArgs>(JobRunnerError);
            jobRunner.Start();
        }

        /// <summary>
        /// Cancel jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerCancelJobs()
        {
            var id = new TestSlowJob().Enqueue(jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            var record = jobStore.GetJob(id);
            Assert.AreEqual(JobStatus.Started, record.Status);

            record.Status = JobStatus.Canceling;
            jobStore.SaveJob(record);
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Canceled, jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Dequeue jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerDequeueJobs()
        {
            var id = new TestSlowJob().Enqueue(jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Started, jobStore.GetJob(id).Status);
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

            jobRunner.SetSchedules(new JobScheduleElement[] { sched1, sched2, sched3 });
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(2, jobStore.GetJobCount(null, null, sched1.Name));
            Assert.AreEqual(1, jobStore.GetJobCount(null, null, sched2.Name));
            Assert.AreEqual(0, jobStore.GetJobCount(null, null, sched3.Name));
        }

        /// <summary>
        /// Finish jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerFinishJobs()
        {
            var id = new TestQuickJob().Enqueue(jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.Succeeded, jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Timeout jobs tests.
        /// </summary>
        [TestMethod]
        public void JobRunnerTimeoutJobs()
        {
            var id = new TestTimeoutJob().Enqueue(jobStore).Id.Value;
            Thread.Sleep(heartbeat * 2);

            Assert.AreEqual(JobStatus.TimedOut, jobStore.GetJob(id).Status);
        }

        /// <summary>
        /// Raises the job runner's Error event.
        /// </summary>
        /// <param name="sender">The evnt sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void JobRunnerError(object sender, JobErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                Assert.Fail(e.Exception.Message + "\n" + e.Exception.StackTrace);
            }

            Assert.Fail(Environment.StackTrace);
        }
    }
}
