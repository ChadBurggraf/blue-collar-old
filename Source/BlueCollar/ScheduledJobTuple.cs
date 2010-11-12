//-----------------------------------------------------------------------
// <copyright file="ScheduledJobTuple.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlueCollar.Configuration;

    /// <summary>
    /// Represents an expanded tuple of information about an individual scheduled job.
    /// </summary>
    public class ScheduledJobTuple
    {
        /// <summary>
        /// Initializes a new instance of the ScheduledJobTuple class.
        /// </summary>
        public ScheduledJobTuple()
            : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ScheduledJobTuple class from the given instance.
        /// Does not copy over the <see cref="ScheduledJobTuple.Record"/> property.
        /// </summary>
        /// <param name="tuple">The tuple instance to create this instance from.</param>
        /// <param name="record">The new tuple's <see cref="JobRecord"/>.</param>
        /// <param name="now">The current date, used to calculate whether the tuple should be executed.</param>
        /// <param name="heartbeat">The heartbeat window of the job runner, used to calculate whether the tuple should be executed.</param>
        public ScheduledJobTuple(ScheduledJobTuple tuple, JobRecord record, DateTime? now, long? heartbeat)
        {
            if (tuple != null)
            {
                this.Schedule = tuple.Schedule;
                this.ScheduledJob = tuple.ScheduledJob;

                if (now != null && heartbeat != null)
                {
                    if (now.Value.Kind != DateTimeKind.Utc)
                    {
                        throw new ArgumentException("now must be in UTC.", "now");
                    }

                    if (heartbeat <= 0)
                    {
                        throw new ArgumentException("heartbeat must be greater than 0.", "heartbeat");
                    }

                    this.ShouldExecute = BlueCollar.ScheduledJob.ShouldExecute(tuple.Schedule, heartbeat.Value, now.Value);
                }
            }

            this.Record = record;

            if (record != null)
            {
                this.LastExecuteDate = record.QueueDate;
            }
        }

        /// <summary>
        /// Gets or sets the scheduled job's last execute date.
        /// </summary>
        public DateTime LastExecuteDate { get; set; }

        /// <summary>
        /// Gets or sets scheduled job's previous run record.
        /// </summary>
        public JobRecord Record { get; set; }

        /// <summary>
        /// Gets or sets the scheduled job's owner schedule.
        /// </summary>
        public JobScheduleElement Schedule { get; set; }

        /// <summary>
        /// Gets or sets the scheduled job's definition.
        /// </summary>
        public JobScheduledJobElement ScheduledJob { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tuple should be executed.
        /// </summary>
        public bool ShouldExecute { get; set; }

        /// <summary>
        /// Gets a collection of executable <see cref="ScheduledJobTuple"/> based on the given projection of all available scheduled
        /// tuples and latest scheduled job runs.
        /// </summary>
        /// <param name="allScheduledJobs">The projection of all available scheduled job tuples.</param>
        /// <param name="latestScheduledJobs">A collection of the latest job run for each schedule and job type.</param>
        /// <param name="now">The current date.</param>
        /// <param name="heartbeat">The job runner heartbeat, in milliseconds.</param>
        /// <param name="count">The maximum number of tuples to get.</param>
        /// <returns>A collection of executable scheduled job tuples.</returns>
        public static IEnumerable<ScheduledJobTuple> GetExecutableTuples(IEnumerable<ScheduledJobTuple> allScheduledJobs, IEnumerable<JobRecord> latestScheduledJobs, DateTime now, long heartbeat, int count)
        {
            if (allScheduledJobs == null)
            {
                throw new ArgumentNullException("allScheduledJobs", "allScheduledJobs cannot be null.");
            }

            if (latestScheduledJobs == null)
            {
                throw new ArgumentNullException("latestScheduledJobs", "latestScheduledJobs cannot be null.");
            }

            return (from t in
                        (from sj in allScheduledJobs
                         from r in
                             (from r in latestScheduledJobs
                              where !String.IsNullOrEmpty(r.JobType) &&
                                    sj.Schedule.Name.Equals(r.ScheduleName, StringComparison.OrdinalIgnoreCase) &&
                                    r.JobType.StartsWith(sj.ScheduledJob.JobType, StringComparison.OrdinalIgnoreCase)
                              select r).DefaultIfEmpty()
                         select new ScheduledJobTuple(sj, r, now, heartbeat))
                    where t.ShouldExecute && t.LastExecuteDate < now.AddMilliseconds(-1 * heartbeat)
                    orderby t.LastExecuteDate
                    select t).Take(count);
        }
    }
}
