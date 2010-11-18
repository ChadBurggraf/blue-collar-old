//-----------------------------------------------------------------------
// <copyright file="PostgresJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics.CodeAnalysis;
    using Npgsql;

    /// <summary>
    /// Implements <see cref="IJobStore"/> for PostgreSQL.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
    public class PostgresJobStore : SqlJobStore
    {
        /// <summary>
        /// Initializes a new instance of the PostgresJobStore class.
        /// </summary>
        public PostgresJobStore()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PostgresJobStore class.
        /// </summary>
        /// <param name="connectionString">The connection string to use when connecting to the database.</param>
        public PostgresJobStore(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Gets the SQL used to select the last inserted record ID.
        /// </summary>
        protected override string SelectLastInsertIdSql
        {
            get { return "SELECT currval('blue_collar_id_seq');"; }
        }

        /// <summary>
        /// Gets the name of the table in the database where <see cref="JobRecord"/>s are stored.
        /// </summary>
        protected override string TableName
        {
            get { return "\"blue_collar\""; }
        }

        /// <summary>
        /// Gets the name of the column in the database table for the given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the column name for.</param>
        /// <returns>A column name.</returns>
        protected override string ColumnName(string propertyName)
        {
            return String.Concat("\"", propertyName.ToLowercaseUnderscore(), "\"");
        }

        /// <summary>
        /// Creates and opens a connection to the SQL job store.
        /// </summary>
        /// <returns>The created connection.</returns>
        protected override DbConnection CreateAndOpenConnection()
        {
            NpgsqlConnection connection = new NpgsqlConnection(this.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Get the name of the parameter to use for the given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the parameter name for.</param>
        /// <returns>A parameter name.</returns>
        protected override string ParameterName(string propertyName)
        {
            return String.Concat(":", propertyName.ToLowercaseUnderscore());
        }

        /// <summary>
        /// Creates a command parameter for the given name and value.
        /// </summary>
        /// <param name="name">The name of the parameter to create.</param>
        /// <param name="value">The value to create the parameter with.</param>
        /// <returns>A command parameter.</returns>
        protected override DbParameter ParameterWithValue(string name, object value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }

        /// <summary>
        /// Gets the name of the <see cref="JobRecord"/> property for the given database column name.
        /// </summary>
        /// <param name="columnName">The column name to get the property name for.</param>
        /// <returns>A property name.</returns>
        protected override string PropertyName(string columnName)
        {
            return columnName.FromLowercaseUnderscore();
        }
    }
}
