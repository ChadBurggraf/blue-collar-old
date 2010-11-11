//-----------------------------------------------------------------------
// <copyright file="JobScheduledJobElement.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Represents a configured scheduled job.
    /// </summary>
    public class JobScheduledJobElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the scheduled job's type.
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true, IsKey = true)]
        public string JobType
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        /// <summary>
        /// Gets any metadata configured for the scheduled job.
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