//-----------------------------------------------------------------------
// <copyright file="BlueCollarSection.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;
    using System.IO;

    /// <summary>
    /// Extends <see cref="ConfigurationSection"/> for Blue Collar configuration settings.
    /// </summary>
    public class BlueCollarSection : ConfigurationSection
    {
        private static BlueCollarSection current = (BlueCollarSection)(ConfigurationManager.GetSection("blueCollar") ?? new BlueCollarSection());

        /// <summary>
        /// Gets the currently configured <see cref="BlueCollarSection"/>.
        /// </summary>
        public static BlueCollarSection Current
        {
            get { return current; }
        }

        /// <summary>
        /// Gets or sets a value indicating dequeueing new jobs by the job runner is enabled.
        /// </summary>
        [ConfigurationProperty("enabled", IsRequired = false, DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
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
        /// Gets or sets the default path to use for the running jobs state file.
        /// </summary>
        [ConfigurationProperty("persistencePath", IsRequired = false, DefaultValue = "BlueCollarRunningJobs.bin")]
        public string PersistencePath
        {
            get { return (string)this["persistencePath"]; }
            set { this["persistencePath"] = value; }
        }

        /// <summary>
        /// Gets the resolved value of <see cref="PersistencePath"/>, relative to the configuration file, if the path is not rooted.
        /// </summary>
        public string PersistencePathResolved
        {
            get
            {
                if (!String.IsNullOrEmpty(this.PersistencePath) && !Path.IsPathRooted(this.PersistencePath))
                {
                    return Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), this.PersistencePath);
                }

                return this.PersistencePath;
            }
        }

        /// <summary>
        /// Gets or sets the timeout (in milliseconds) to use when re-enqueing failed
        /// jobs that have retries available. When not configured, defaults to 60,000 (1 minute).
        /// </summary>
        [ConfigurationProperty("retryTimeout", IsRequired = false, DefaultValue = 60000)]
        public int RetryTimeout
        {
            get { return (int)this["retryTimeout"]; }
            set { this["retryTimeout"] = value; }
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