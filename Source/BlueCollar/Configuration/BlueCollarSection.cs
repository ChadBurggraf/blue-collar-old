//-----------------------------------------------------------------------
// <copyright file="BlueCollarSection.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Extends <see cref="ConfigurationSection"/> for Blue Collar configuration settings.
    /// </summary>
    public class BlueCollarSection : ConfigurationSection
    {
        private static BlueCollarSection section = (BlueCollarSection)(ConfigurationManager.GetSection("blueCollar") ?? new BlueCollarSection());

        /// <summary>
        /// Gets the currently configured <see cref="BlueCollarSection"/>.
        /// </summary>
        public static BlueCollarSection Current
        {
            get { return section; }
        }

        /// <summary>
        /// Gets or sets the heartbeat timeout (in miliseconds) to use for the job runner. The runner will 
        /// pause for this duration at the end of each cancel/finish/timeout/dequeue loop.
        /// When not configured, defaults to 10,000 (10 seconds).
        /// </summary>
        [ConfigurationProperty("heartbeat", IsRequired = false, DefaultValue = 10000)]
        public int Heartbeat
        {
            get { return (int)this["heartbeat"]; }
            set { this["heartbeat"] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of jobs that are allowed to be
        /// running simultaneously. When not configured, defaults to 25.
        /// </summary>
        [ConfigurationProperty("maximumConcurrency", IsRequired = false, DefaultValue = 25)]
        public int MaximumConcurrency
        {
            get { return (int)this["maximumConcurrency"]; }
            set { this["maximumConcurrency"] = value; }
        }

        /// <summary>
        /// Gets the configured collection of scheduled jobs.
        /// </summary>
        [ConfigurationProperty("schedules", IsRequired = false)]
        public JobScheduleElementCollection Schedules
        {
            get { return (JobScheduleElementCollection)(this["schedules"] ?? (this["schedules"] = new JobScheduleElementCollection())); }
        }

        /// <summary>
        /// Gets the configuration definition for the current <see cref="BlueCollar.IJobStore"/> to use.
        /// </summary>
        [ConfigurationProperty("store", IsRequired = false)]
        public JobStoreElement Store
        {
            get { return (JobStoreElement)(this["store"] ?? (this["store"] = new JobStoreElement())); }
        }

        /// <summary>
        /// Gets a value indicating whether the configuration section is read only.
        /// </summary>
        /// <returns>True if the section is read only, false otherwise.</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}