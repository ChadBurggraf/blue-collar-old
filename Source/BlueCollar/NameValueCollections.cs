//-----------------------------------------------------------------------
// <copyright file="NameValueCollections.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;

    /// <summary>
    /// Provides extensions and helpers to <see cref="NameValueCollection"/>.
    /// </summary>
    public static class NameValueCollections
    {
        /// <summary>
        /// Clears and then fills the collection with the key/value pairs in the given <see cref="KeyValueConfigurationCollection"/>.
        /// </summary>
        /// <param name="collection">The collection to fill.</param>
        /// <param name="configCollection">The <see cref="KeyValueConfigurationCollection"/> to use as a fill source.</param>
        public static void FillWith(this NameValueCollection collection, KeyValueConfigurationCollection configCollection)
        {
            collection.Clear();

            foreach (KeyValueConfigurationElement element in configCollection)
            {
                collection.Add(element.Key, element.Value);
            }
        }
    }
}
