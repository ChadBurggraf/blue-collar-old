//-----------------------------------------------------------------------
// <copyright file="Strings.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Provides extensions and helpers to <see cref="System.String"/>.
    /// </summary>
    public static class Strings
    {
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
