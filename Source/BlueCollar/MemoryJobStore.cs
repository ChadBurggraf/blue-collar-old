//-----------------------------------------------------------------------
// <copyright file="MemoryJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.SQLite;
    using System.IO;
    using BlueCollar.Configuration;

    /// <summary>
    /// Implements <see cref="IJobStore"/> to use an in-memory SQLite database.
    /// </summary>
    public class MemoryJobStore : SQLiteJobStore
    {
        private static string connectionString = CreateConnectionString();
        private static SQLiteConnection connection = CreateConnection(connectionString);

        /// <summary>
        /// Initializes a new instance of the MemoryJobStore class.
        /// </summary>
        public MemoryJobStore()
            : base(connectionString)
        {
        }

        /// <summary>
        /// Disposes the given database connection created by this instance.
        /// </summary>
        /// <param name="connection">The database connection to dispose.</param>
        public override void DisposeConnection(DbConnection connection)
        {
        }

        /// <summary>
        /// Initializes the job store from the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to initialize the job store from.</param>
        public override void Initialize(JobStoreElement element)
        {
        }

        /// <summary>
        /// Creates and opens a connection to the SQL job store.
        /// </summary>
        /// <returns>The created connection.</returns>
        protected override DbConnection CreateAndOpenConnection()
        {
            return connection;
        }

        /// <summary>
        /// Creates and opens the connection to use for the in-memory database.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <returns>A connection.</returns>
        private static SQLiteConnection CreateConnection(string connectionString)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;

                using (Stream stream = typeof(SQLiteJobStore).Assembly.GetManifestResourceStream("BlueCollar.Sql.BlueCollar-SQLite.sql"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        command.CommandText = reader.ReadToEnd();
                    }
                }

                command.ExecuteNonQuery();
            }

            return connection;
        }

        /// <summary>
        /// Creates the connection string to use when connecting to the in-memory database.
        /// </summary>
        /// <returns>A connection string.</returns>
        private static string CreateConnectionString()
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = ":memory:";
            return builder.ToString();
        }
    }
}
