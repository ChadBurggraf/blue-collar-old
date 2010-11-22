//-----------------------------------------------------------------------
// <copyright file="JobStoreElement.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Represents a configuration element describing the current <see cref="BlueCollar.IJobStore"/> being used.
    /// </summary>
    public class JobStoreElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets a value indicating whether to delete job records after successful job runs.
        /// </summary>
        [ConfigurationProperty("deleteRecordsOnSuccess", IsRequired = false, DefaultValue = true)]
        public bool DeleteRecordsOnSuccess
        {
            get { return (bool)this["deleteRecordsOnSuccess"]; }
            set { this["deleteRecordsOnSuccess"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="BlueCollar.IJobStore"/> implementation to use when persisting jobs.
        /// When not configured, defaults to "BlueCollar.MemoryJobStore, BlueCollar".
        /// </summary>
        [ConfigurationProperty("type", IsRequired = false, DefaultValue = "BlueCollar.MemoryJobStore, BlueCollar")]
        public string JobStoreType
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        /// <summary>
        /// Gets any metadata configured for the current job store.
        /// </summary>
        [ConfigurationProperty("metadata", IsRequired = false)]
        public KeyValueConfigurationCollection Metadata
        {
            get { return (KeyValueConfigurationCollection)(this["metadata"] ?? (this["metadata"] = new KeyValueConfigurationCollection())); }
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
