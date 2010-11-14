//-----------------------------------------------------------------------
// <copyright file="Strings.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
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
            if (!String.IsNullOrEmpty(connectionStringName))
            {
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

            return String.Empty;
        }

        /// <summary>
        /// Converts the lower_case_underscore string into a PascalCase or camelCalse string.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The converted string.</returns>
        public static string FromLowercaseUnderscore(this string value)
        {
            return FromLowercaseUnderscore(value, false);
        }

        /// <summary>
        /// Converts the lower_case_underscore string into a PascalCase or camelCalse string.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <param name="camel">A value indicating whether to convert to camelCalse. If false, will convert to PascalCase.</param>
        /// <returns>The converted string.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Explicitly expecting a lowercase string.")]
        public static string FromLowercaseUnderscore(this string value, bool camel)
        {
            value = (value ?? String.Empty).ToLowerInvariant().Trim();

            if (String.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder sb = new StringBuilder();
            int i = 0;
            int wordLetterNumber = 0;

            while (i < value.Length)
            {
                if (Char.IsLetterOrDigit(value, i))
                {
                    wordLetterNumber++;
                }
                else
                {
                    wordLetterNumber = 0;
                }

                if (wordLetterNumber == 1)
                {
                    if (camel && i == 0)
                    {
                        sb.Append(value[i]);
                    }
                    else
                    {
                        sb.Append(value[i].ToString().ToUpperInvariant());
                    }
                }
                else if (value[i] != '_')
                {
                    sb.Append(value[i]);
                }

                i++;
            }

            return sb.ToString();
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

        /// <summary>
        /// Converts the camelCase or PascalCase string to a lower_case_underscore string.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The converted string.</returns>
        public static string ToLowercaseUnderscore(this string value)
        {
            value = (value ?? String.Empty).Trim();

            if (String.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder sb = new StringBuilder();
            int i = 0;
            int wordLetterNumber = 0;
            bool prevUpper = false;

            while (i < value.Length)
            {
                if (Char.IsLetterOrDigit(value, i))
                {
                    wordLetterNumber++;
                }
                else
                {
                    wordLetterNumber = 0;
                }

                if (Char.IsUpper(value, i))
                {
                    if (wordLetterNumber > 1 && !prevUpper)
                    {
                        sb.Append("_");
                    }

                    sb.Append(Char.ToLowerInvariant(value[i]));
                    prevUpper = true;
                }
                else
                {
                    sb.Append(value[i]);
                    prevUpper = false;
                }

                i++;
            }

            return sb.ToString();
        }
    }
}
