﻿//-----------------------------------------------------------------------
// <copyright file="SqlJobStoreTransaction.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Data;
    using System.Data.Common;

    /// <summary>
    /// Implements <see cref="IJobStoreTransaction"/> for SQL databases.
    /// </summary>
    public class SqlJobStoreTransaction : IJobStoreTransaction
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the SqlJobStoreTransaction class.
        /// </summary>
        /// <param name="connection">The connection to initialize the transaction with.</param>
        /// <param name="transaction">The concrete transaction to initialize the transaction with.</param>
        public SqlJobStoreTransaction(DbConnection connection, DbTransaction transaction)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection", "connection cannot be null.");
            }

            if (transaction == null)
            {
                throw new ArgumentNullException("transaction", "transaction cannot be null.");
            }

            this.Connection = connection;
            this.Transaction = transaction;
            this.IsActive = true;
        }

        /// <summary>
        /// Finalizes an instance of the SqlJobStoreTransaction class.
        /// </summary>
        ~SqlJobStoreTransaction()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the connection the transaction is for.
        /// </summary>
        public virtual DbConnection Connection { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transaction is active.
        /// </summary>
        public virtual bool IsActive { get; protected set; }

        /// <summary>
        /// Gets or sets the concrete transaction.
        /// </summary>
        public virtual DbTransaction Transaction { get; protected set; }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public virtual void Commit()
        {
            if (this.IsActive)
            {
                this.IsActive = false;
                this.Transaction.Commit();
            }
        }

        /// <summary>
        /// Disposes of resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Rolls the transaction back.
        /// </summary>
        public virtual void Rollback()
        {
            if (this.IsActive)
            {
                this.IsActive = false;
                this.Transaction.Rollback();
            }
        }

        /// <summary>
        /// Disposes of resources used by this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether to dispose of managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.Transaction != null)
                    {
                        if (this.IsActive)
                        {
                            this.Commit();
                        }

                        this.Transaction.Dispose();
                        this.Transaction = null;
                    }

                    if (this.Connection != null)
                    {
                        this.Connection.Dispose();
                        this.Connection = null;
                    }
                }

                this.disposed = true;
            }
        }
    }
}
