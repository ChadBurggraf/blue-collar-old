//-----------------------------------------------------------------------
// <copyright file="IJobStoreTransaction.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;

    /// <summary>
    /// Identifies the interface for <see cref="IJobStore"/> transactions.
    /// </summary>
    public interface IJobStoreTransaction : IDisposable
    {
        /// <summary>
        /// Commits the transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        void Rollback();
    }
}
