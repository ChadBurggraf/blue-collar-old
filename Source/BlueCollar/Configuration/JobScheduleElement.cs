//-----------------------------------------------------------------------
// <copyright file="JobScheduleElement.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Represents a job schedule in the configuration.
    /// </summary>
    public class JobScheduleElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the schedule's name.
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// Gets or sets the number of hours the schedule's repeat cycle is on.
        /// </summary>
        [ConfigurationProperty("repeatHours", IsRequired = true)]
        public double RepeatHours
        {
            get { return (double)this["repeatHours"]; }
            set { this["repeatHours"] = value; }
        }

        /// <summary>
        /// Gets the jobs that are configured to run as part of this schedule.
        /// </summary>
        [ConfigurationProperty("scheduledJobs", IsRequired = false)]
        public JobScheduledJobElementCollection ScheduledJobs
        {
            get { return (JobScheduledJobElementCollection)(this["scheduledJobs"] ?? (this["scheduledJobs"] = new JobScheduledJobElementCollection())); }
        }

        /// <summary>
        /// Gets or sets the date and time the schedule starts on.
        /// </summary>
        [ConfigurationProperty("startOn", IsRequired = true)]
        public DateTime StartOn
        {
            get { return (DateTime)this["startOn"]; }
            set { this["startOn"] = value; }
        }

        /// <summary>
        /// Gets a value indicating if the System.Configuration.ConfigurationElement object is read-only.
        /// </summary>
        /// <returns>True if the System.Configuration.ConfigurationElement object is read-only, false otherwise.</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}