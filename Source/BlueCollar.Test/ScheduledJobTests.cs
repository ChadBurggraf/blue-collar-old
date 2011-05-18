//-----------------------------------------------------------------------
// <copyright file="ScheduledJobTests.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using BlueCollar.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Scheduled job tests.
    /// </summary>
    [TestClass]
    public sealed class ScheduledJobTests
    {
        /// <summary>
        /// Should execute tests.
        /// </summary>
        [TestMethod]
        public void ScheduledJobShouldExecute()
        {
            JobScheduleElement element = new JobScheduleElement()
            {
                Name = "Test",
                RepeatHours = 24,
                StartOn = DateTime.Now.AddMilliseconds(-500)
            };

            Assert.IsTrue(ScheduledJob.ShouldExecute(element, 1000, DateTime.UtcNow));

            element.StartOn = DateTime.Now.AddMilliseconds(-1001);
            Assert.IsFalse(ScheduledJob.ShouldExecute(element, 1000, DateTime.UtcNow));

            element.StartOn = DateTime.Now.AddHours(1);
            Assert.IsFalse(ScheduledJob.ShouldExecute(element, 1000, DateTime.UtcNow));

            element.StartOn = DateTime.Now;
            Assert.IsTrue(ScheduledJob.ShouldExecute(element, 1000, DateTime.UtcNow));
        }
    }
}
