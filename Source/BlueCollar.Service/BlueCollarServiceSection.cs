//-----------------------------------------------------------------------
// <copyright file="BlueCollarServiceSection.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Service
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Extends <see cref="ConfigurationSection"/> for Blue Collar Service configuration settings.
    /// </summary>
    public class BlueCollarServiceSection : ConfigurationSection
    {
        private static readonly object locker = new object();
        private static BlueCollarServiceSection current;

        /// <summary>
        /// Gets the currently configured <see cref="BlueCollarServiceSection"/>.
        /// </summary>
        public static BlueCollarServiceSection Current
        {
            get
            {
                lock (locker)
                {
                    return current ?? (current = (BlueCollarServiceSection)(ConfigurationManager.GetSection("blueCollarService") ?? new BlueCollarServiceSection()));
                }
            }
        }

        /// <summary>
        /// Gets the applications configuration element collection.
        /// </summary>
        [ConfigurationProperty("applications", IsDefaultCollection = true)]
        public ApplicationElementCollection Applications
        {
            get { return (ApplicationElementCollection)(this["applications"] ?? (this["applications"] = new ApplicationElementCollection())); }
        }

        /// <summary>
        /// Refreshes the currently loaded configuration settings to the latest values from the configuration file.
        /// </summary>
        public static void Refresh()
        {
            ConfigurationManager.RefreshSection("blueCollarService");

            lock (locker)
            {
                current = null;
            }
        }
    }
}
