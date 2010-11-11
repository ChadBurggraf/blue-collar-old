//-----------------------------------------------------------------------
// <copyright file="Strings.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Configuration;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Provides extensions and helpers to <see cref="System.String"/>.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Gets the configured connection string identified in the given metadata collection
        /// by a key of "ConnectionStringName", defaulting to "LocalSqlServer" if nothing is found.
        /// </summary>
        /// <param name="metadata">The metadata collection identifying the connection string to get.</param>
        /// <returns>A connection string, or <see cref="String.Empty"/> if none was found.</returns>
        public static string ConfiguredConnectionString(KeyValueConfigurationCollection metadata)
        {
            var keyValue = metadata["ConnectionStringName"];
            return ConfiguredConnectionString(keyValue != null ? keyValue.Value : null);
        }

        /// <summary>
        /// Gets the configured connection string with the given name, defaulting to "LocalSqlServer" if
        /// the given name is empty.
        /// </summary>
        /// <param name="connectionStringName">The name of the connection string to get.</param>
        /// <returns>A connection string, or <see cref="String.Empty"/> if none was found.</returns>
        public static string ConfiguredConnectionString(string connectionStringName)
        {
            connectionStringName = !String.IsNullOrEmpty(connectionStringName) ? connectionStringName : "LocalSqlServer";
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionString != null)
            {
                return connectionString.ConnectionString;
            }
            else
            {
                return ConfigurationManager.AppSettings[connectionStringName];
            }
        }

        /// <summary>
        /// Computes the SHA1 hash of the string.
        /// </summary>
        /// <param name="value">The value to hash.</param>
        /// <returns>The hashed text.</returns>
        public static string Hash(this string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            return BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(buffer)).Replace("-", String.Empty);
        }
    }
}
