//-----------------------------------------------------------------------
// <copyright file="SQLiteJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Data;
    using System.Data.Common;
    using System.Data.SQLite;
    using System.IO;
    using BlueCollar.Configuration;

    /// <summary>
    /// Implements <see cref="IJobStore"/> for SQLite.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
    public class SQLiteJobStore : SqlJobStore
    {
        /// <summary>
        /// Initializes a new instance of the SQLiteJobStore class.
        /// </summary>
        public SQLiteJobStore()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SQLiteJobStore class.
        /// </summary>
        /// <param name="connectionString">The connection string to use when connecting to the database.</param>
        public SQLiteJobStore(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Gets the SQL used to select the last inserted record ID.
        /// </summary>
        protected override string SelectLastInsertIdSql
        {
            get { return "SELECT last_insert_rowid();"; }
        }

        /// <summary>
        /// Initializes the job store from the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to initialize the job store from.</param>
        public override void Initialize(JobStoreElement element)
        {
            try
            {
                base.Initialize(element);
            }
            catch (ConfigurationErrorsException)
            {
            }

            string databasePath = null;
            SQLiteConnectionStringBuilder builder;

            if (!String.IsNullOrEmpty(ConnectionString))
            {
                builder = new SQLiteConnectionStringBuilder(ConnectionString);
                databasePath = ResolveDatabaseFilePath(builder.DataSource);
                builder.DataSource = databasePath;
            }
            else
            {
                builder = new SQLiteConnectionStringBuilder();
                databasePath = builder.DataSource = ResolveDatabaseFilePath("BlueCollar.s3db");
            }

            this.ConnectionString = builder.ToString();
            EnsureDatabase(databasePath);
        }

        /// <summary>
        /// Creates and opens a connection to the SQL job store.
        /// </summary>
        /// <returns>The created connection.</returns>
        protected override DbConnection CreateAndOpenConnection()
        {
            SQLiteConnection connection = new SQLiteConnection(this.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Creates a command parameter for the given name and value.
        /// </summary>
        /// <param name="name">The name of the parameter to create.</param>
        /// <param name="value">The value to create the parameter with.</param>
        /// <returns>A command parameter.</returns>
        protected override DbParameter ParameterWithValue(string name, object value)
        {
            return new SQLiteParameter(name, value ?? DBNull.Value);
        }

        /// <summary>
        /// Ensures a Cardini SQLite database at the given path.
        /// </summary>
        /// <param name="databasePath">The database path to ensure.</param>
        private static void EnsureDatabase(string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                using (Stream stream = typeof(SQLiteJobStore).Assembly.GetManifestResourceStream("BlueCollar.BlueCollar.s3db"))
                {
                    byte[] buffer = new byte[4096];
                    int count;

                    using (FileStream file = File.Create(databasePath))
                    {
                        while (0 < (count = stream.Read(buffer, 0, buffer.Length)))
                        {
                            file.Write(buffer, 0, count);
                        }
                    }
                }
            }
        }
    }
}
