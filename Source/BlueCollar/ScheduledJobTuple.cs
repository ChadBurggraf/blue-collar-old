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
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ScheduledJobTuple class from the given instance.
        /// </summary>
        /// <param name="tuple">The tuple instance to create this instance from.</param>
        /// <param name="now">The current date, used to calculate whether the tuple should be executed.</param>
        /// <param name="heartbeat">The heartbeat window of the job runner, used to calculate whether the tuple should be executed.</param>
        public ScheduledJobTuple(ScheduledJobTuple tuple, DateTime? now, long? heartbeat)
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

                    DateTime? executeOn;
                    this.ShouldExecute = BlueCollar.ScheduledJob.ShouldExecute(tuple.Schedule, heartbeat.Value, now.Value, out executeOn);
                    this.ExecuteOn = executeOn;
                }
            }
        }

        /// <summary>
        /// Gets or sets the scheduled job's concrete execution date, if applicable.
        /// </summary>
        public DateTime? ExecuteOn { get; set; }

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
        /// <param name="now">The current date.</param>
        /// <param name="heartbeat">The job runner heartbeat, in milliseconds.</param>
        /// <param name="count">The maximum number of tuples to get.</param>
        /// <returns>A collection of executable scheduled job tuples.</returns>
        public static IEnumerable<ScheduledJobTuple> GetExecutableTuples(IEnumerable<ScheduledJobTuple> allScheduledJobs, DateTime now, long heartbeat, int count)
        {
            if (allScheduledJobs == null)
            {
                throw new ArgumentNullException("allScheduledJobs", "allScheduledJobs cannot be null.");
            }

            return (from t in allScheduledJobs.Select(sj => new ScheduledJobTuple(sj, now, heartbeat))
                    where t.ShouldExecute
                    orderby t.ExecuteOn
                    select t).Take(count);
        }
    }
}
