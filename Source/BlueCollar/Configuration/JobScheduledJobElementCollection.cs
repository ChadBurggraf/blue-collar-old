﻿//-----------------------------------------------------------------------
// <copyright file="JobScheduledJobElementCollection.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Configuration
{
    using System;
    using System.Configuration;
    using System.Linq;

    /// <summary>
    /// Represents a collection of <see cref="JobScheduledJobElement"/>s in the configuration.
    /// </summary>
    public class JobScheduledJobElementCollection : ConfigurationElementCollection<JobScheduledJobElement>
    {
        /// <summary>
        /// Gets a value indicating whether the collection contains the given item.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the collection contains the item, false otherwise.</returns>
        public override bool Contains(JobScheduledJobElement item)
        {
            return this.Any(sj => sj.JobType.Equals(item.JobType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the unique key of the given element.
        /// </summary>
        /// <param name="element">The element to get the key of.</param>
        /// <returns>The given element's key.</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((JobScheduledJobElement)element).JobType;
        }
    }
}