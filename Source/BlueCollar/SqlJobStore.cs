//-----------------------------------------------------------------------
// <copyright file="SqlJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using BlueCollar.Configuration;

    /// <summary>
    /// Extends <see cref="JobStore"/> to serve as the base class for SQL <see cref="IJobStore"/> implementations.
    /// </summary>
    public abstract class SqlJobStore : JobStore
    {
        #region Private Fields

        private static readonly PropertyInfo[] allJobRecordProperties = typeof(JobRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();
        private static readonly PropertyInfo[] readJobRecordProperties = allJobRecordProperties.Where(p => p.CanRead && !p.Name.Equals("Id", StringComparison.Ordinal)).ToArray();
        private static readonly PropertyInfo[] writeJobRecordProperties = allJobRecordProperties.Where(p => p.CanWrite).ToArray();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the SqlJobStore class.
        /// </summary>
        protected SqlJobStore()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SqlJobStore class.
        /// </summary>
        /// <param name="connectionString">The connection string to use when connecting to the database.</param>
        protected SqlJobStore(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the connection string to use when connecting to the database.
        /// </summary>
        public string ConnectionString { get; protected set; }

        #endregion

        #region Protected Instance Properties

        /// <summary>
        /// Gets the SQL used to select the last inserted record ID.
        /// </summary>
        protected abstract string SelectLastInsertIdSql { get; }

        /// <summary>
        /// Gets the name of the table in the database where <see cref="JobRecord"/>s are stored.
        /// </summary>
        protected virtual string TableName
        {
            get { return "[BlueCollar]"; }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Resolves the path of the given database file, expanding out the |DataDirectory| directive if present.
        /// </summary>
        /// <param name="path">The database file path to resolve.</param>
        /// <returns>The resolved path.</returns>
        public static string ResolveDatabaseFilePath(string path)
        {
            const string DataDirectoryDirective = "|DataDirectory|";

            if (!String.IsNullOrEmpty(path))
            {
                if (path.StartsWith(DataDirectoryDirective, StringComparison.OrdinalIgnoreCase))
                {
                    path = Regex.Replace(path.Substring(DataDirectoryDirective.Length), "@\\|/", Path.DirectorySeparatorChar.ToString());
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("App_Data", path));
                }

                path = Path.GetFullPath(path);
            }

            return path;
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A new <see cref="IJobStoreTransaction"/>.</returns>
        public override IJobStoreTransaction BeginTransaction()
        {
            DbConnection connection = this.CreateAndOpenConnection();
            return new SqlJobStoreTransaction(this, connection, connection.BeginTransaction(IsolationLevel.ReadCommitted));
        }

        /// <summary>
        /// Deletes all jobs in the job store.
        /// </summary>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public override void DeleteAllJobs(IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;

            try
            {
                if (trans != null)
                {
                    command = this.CreateDeleteCommand(trans.Connection);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateDeleteCommand(connection);
                }

                command.ExecuteNonQuery();
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }
        }

        /// <summary>
        /// Deletes a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public override void DeleteJob(int id, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;

            try
            {
                if (trans != null)
                {
                    command = this.CreateDeleteCommand(trans.Connection, id);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateDeleteCommand(connection, id);
                }

                command.ExecuteNonQuery();
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }
        }

        /// <summary>
        /// Disposes the given database connection created by this instance.
        /// </summary>
        /// <param name="connection">The database connection to dispose.</param>
        public virtual void DisposeConnection(DbConnection connection)
        {
            if (connection != null)
            {
                connection.Dispose();
            }
        }

        /// <summary>
        /// Initializes the job store from the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to initialize the job store from.</param>
        public override void Initialize(JobStoreElement element)
        {
            base.Initialize(element);

            if (String.IsNullOrEmpty(this.ConnectionString) && element != null)
            {
                this.ConnectionString = Strings.ConfiguredConnectionString(element.Metadata);

                if (String.IsNullOrEmpty(this.ConnectionString))
                {
                    throw new ConfigurationErrorsException(@"Failed to find a connection string to initialze the job store with. Either you're missing the definition in blueCollar/store/metadata[key = ""ConnectionStringName""], or the name found does not identify a connection string under connectionStrings or appSettings.", element.ElementInformation.Source, element.ElementInformation.LineNumber);
                }
            }
        }

        /// <summary>
        /// Gets a job by ID.
        /// </summary>
        /// <param name="id">The ID of the job to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The job with the given ID.</returns>
        public override JobRecord GetJob(int id, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            JobRecord record = null;

            try
            {
                if (trans != null)
                {
                    command = this.CreateSelectCommand(trans.Connection, id);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateSelectCommand(connection, id);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        record = this.CreateRecord(reader);
                    }
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }

            return record;
        }

        /// <summary>
        /// Gets the number of jobs in the store that match the given filter.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>The number of jobs that match the given filter.</returns>
        public override int GetJobCount(string likeName, JobStatus? withStatus, string inSchedule, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            int count = 0;

            try
            {
                if (trans != null)
                {
                    command = this.CreateCountCommand(trans.Connection, likeName, withStatus, inSchedule);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateCountCommand(connection, likeName, withStatus, inSchedule);
                }

                count = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }

            return count;
        }

        /// <summary>
        /// Gets a collection of jobs that match the given collection of IDs.
        /// </summary>
        /// <param name="ids">The IDs of the jobs to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public override IEnumerable<JobRecord> GetJobs(IEnumerable<int> ids, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            List<JobRecord> records = new List<JobRecord>();

            try
            {
                if (trans != null)
                {
                    command = this.CreateSelectCommand(trans.Connection, ids);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateSelectCommand(connection, ids);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(this.CreateRecord(reader));
                    }
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }

            return records;
        }

        /// <summary>
        /// Gets a collection of jobs with the given status, returning
        /// at most the number of jobs identified by <paramref name="count"/>.
        /// </summary>
        /// <param name="status">The status of the jobs to get.</param>
        /// <param name="count">The maximum number of jobs to get.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public override IEnumerable<JobRecord> GetJobs(JobStatus status, int count, DateTime before, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            List<JobRecord> records = new List<JobRecord>();

            try
            {
                if (trans != null)
                {
                    command = this.CreateSelectCommand(trans.Connection, status, count, before);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateSelectCommand(connection, status, count, before);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(this.CreateRecord(reader));
                    }
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }

            return records;
        }

        /// <summary>
        /// Gets a collection of jobs that match the given filter parameters, ordered by the given sort parameters.
        /// </summary>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="orderBy">A field to order the resultset by.</param>
        /// <param name="sortDescending">A value indicating whether to order the resultset in descending order.</param>
        /// <param name="pageNumber">The page number to get.</param>
        /// <param name="pageSize">The size of the pages to get.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        /// <returns>A collection of jobs.</returns>
        public override IEnumerable<JobRecord> GetJobs(string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            List<JobRecord> records = new List<JobRecord>();

            try
            {
                if (trans != null)
                {
                    command = this.CreateSelectCommand(trans.Connection, likeName, withStatus, inSchedule, orderBy, sortDescending, pageNumber, pageSize);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateSelectCommand(connection, likeName, withStatus, inSchedule, orderBy, sortDescending, pageNumber, pageSize);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(this.CreateRecord(reader));
                    }
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }

            return records;
        }

        /// <summary>
        /// Saves the given job record, either creating it or updating it.
        /// </summary>
        /// <param name="record">The job to save.</param>
        /// <param name="transaction">The transaction to execute the command in.</param>
        public override void SaveJob(JobRecord record, IJobStoreTransaction transaction)
        {
            SqlJobStoreTransaction trans = transaction as SqlJobStoreTransaction;
            DbConnection connection = null;
            DbCommand command = null;
            bool creating = !record.Id.HasValue;

            try
            {
                if (trans != null)
                {
                    command = this.CreateSaveCommand(trans.Connection, record);
                    command.Transaction = trans.Transaction;
                }
                else
                {
                    connection = this.CreateAndOpenConnection();
                    command = this.CreateSaveCommand(connection, record);
                }

                if (creating)
                {
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            record.Id = Convert.ToInt32(reader[0], CultureInfo.InvariantCulture);
                        }
                    }
                }
                else
                {
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                this.DisposeConnection(connection);
            }
        }

        #endregion

        #region Protected Instance Methods

        /// <summary>
        /// Gets the name of the column in the database table for the given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the column name for.</param>
        /// <returns>A column name.</returns>
        protected virtual string ColumnName(string propertyName)
        {
            return String.Concat("[", propertyName, "]");
        }

        /// <summary>
        /// Creates and opens a connection to the SQL job store.
        /// </summary>
        /// <returns>The created connection.</returns>
        protected abstract DbConnection CreateAndOpenConnection();

        /// <summary>
        /// Creates a command that can be used to fetch the number of records matching the given filter parameters.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <returns>A select command.</returns>
        protected virtual DbCommand CreateCountCommand(DbConnection connection, string likeName, JobStatus? withStatus, string inSchedule)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "SELECT COUNT({0}) FROM {1} WHERE {2} LIKE {3}",
                this.ColumnName("Id"),
                this.TableName,
                this.ColumnName("Name"),
                this.ParameterName("Name"));

            if (withStatus != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", this.ColumnName("Status"), this.ParameterName("Status"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Status"), withStatus.Value.ToString()));
            }

            if (!String.IsNullOrEmpty(inSchedule))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", this.ColumnName("ScheduleName"), this.ParameterName("ScheduleName"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("ScheduleName"), inSchedule));
            }

            sb.Append(";");
            command.CommandText = sb.ToString();
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Name"), String.Concat("%", (likeName ?? String.Empty).Trim(), "%")));

            return command;
        }

        /// <summary>
        /// Creates a delete command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <returns>A delete command.</returns>
        protected virtual DbCommand CreateDeleteCommand(DbConnection connection)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM {0};", this.TableName);
            return command;
        }

        /// <summary>
        /// Creates a delete command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>A delete command.</returns>
        protected virtual DbCommand CreateDeleteCommand(DbConnection connection, int id)
        {
            const string Sql = "DELETE FROM {0} WHERE {1} = {2};";

            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = String.Format(
                CultureInfo.InvariantCulture,
                Sql,
                this.TableName,
                this.ColumnName("Id"),
                this.ParameterName("Id"));

            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Id"), id));
            return command;
        }

        /// <summary>
        /// Creates a new <see cref="JobRecord"/> instance from the data at the current position
        /// of the given reader.
        /// </summary>
        /// <param name="reader">The reader to create the record from.</param>
        /// <returns>The created record.</returns>
        protected virtual JobRecord CreateRecord(DbDataReader reader)
        {
            JobRecord record = new JobRecord();
            int fieldCount = reader.FieldCount;

            while (fieldCount > 0)
            {
                int ordinal = fieldCount - 1;
                string fieldName = reader.GetName(ordinal);
                string propertyName = this.PropertyName(fieldName);
                PropertyInfo info = writeJobRecordProperties.Where(p => p.Name.Equals(propertyName, StringComparison.Ordinal)).FirstOrDefault();

                if (info != null)
                {
                    if (!reader.IsDBNull(ordinal))
                    {
                        object value = reader[ordinal];

                        if (typeof(DateTime?).IsAssignableFrom(info.PropertyType))
                        {
                            info.SetValue(record, new DateTime(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks, DateTimeKind.Utc), null);
                        }
                        else if (typeof(JobStatus?).IsAssignableFrom(info.PropertyType))
                        {
                            info.SetValue(record, Enum.Parse(typeof(JobStatus), Convert.ToString(value, CultureInfo.InvariantCulture)), null);
                        }
                        else if (typeof(int?).IsAssignableFrom(info.PropertyType))
                        {
                            info.SetValue(record, Convert.ToInt32(value, CultureInfo.InvariantCulture), null);
                        }
                        else if (typeof(string).IsAssignableFrom(info.PropertyType))
                        {
                            info.SetValue(record, Convert.ToString(value, CultureInfo.InvariantCulture), null);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                --fieldCount;
            }

            return record;
        }

        /// <summary>
        /// Creates an insert or update command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="record">The record to insert or update.</param>
        /// <returns>An insert or update command.</returns>
        protected virtual DbCommand CreateSaveCommand(DbConnection connection, JobRecord record)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            bool creating = !record.Id.HasValue;

            if (creating)
            {
                sb1.AppendFormat(CultureInfo.InvariantCulture, "INSERT INTO {0}(", this.TableName);
                sb2.Append(" VALUES(");
            }
            else
            {
                sb1.AppendFormat(CultureInfo.InvariantCulture, "UPDATE {0} SET ", this.TableName);
            }

            for (int i = 0; i < readJobRecordProperties.Length; i++)
            {
                string name = readJobRecordProperties[i].Name;
                object value = readJobRecordProperties[i].GetValue(record, null);

                if (i > 0)
                {
                    sb1.Append(",");

                    if (creating)
                    {
                        sb2.Append(",");
                    }
                }

                if (creating)
                {
                    sb1.Append(this.ColumnName(name));
                    sb2.Append(this.ParameterName(name));
                }
                else
                {
                    sb1.AppendFormat(CultureInfo.InvariantCulture, "{0} = {1}", this.ColumnName(name), this.ParameterName(name));
                }

                if (value != null && value.GetType().IsEnum)
                {
                    value = value.ToString();
                }

                command.Parameters.Add(this.ParameterWithValue(this.ParameterName(name), value));
            }

            if (creating)
            {
                sb1.Append(")");
                sb2.AppendFormat(CultureInfo.InvariantCulture, "); {0}", this.SelectLastInsertIdSql);
            }
            else
            {
                sb1.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0} = {1};", this.ColumnName("Id"), this.ParameterName("Id"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Id"), record.Id.Value));
            }

            command.CommandText = String.Concat(sb1, sb2);
            return command;
        }

        /// <summary>
        /// Creates a select command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="id">The ID of the result to fetch.</param>
        /// <returns>A select command.</returns>
        protected virtual DbCommand CreateSelectCommand(DbConnection connection, int id)
        {
            const string Sql = "SELECT * FROM {0} WHERE {1} = {2};";

            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = String.Format(
                CultureInfo.InvariantCulture,
                Sql,
                this.TableName,
                this.ColumnName("Id"),
                this.ParameterName("Id"));

            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Id"), id));

            return command;
        }

        /// <summary>
        /// Creates a select command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="ids">The IDs to restrict the result set to.</param>
        /// <returns>A select command.</returns>
        protected virtual DbCommand CreateSelectCommand(DbConnection connection, IEnumerable<int> ids)
        {
            const string SqlStart = "SELECT * FROM {0} WHERE {1} IN (";
            const string SqlEnd = ");";

            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, SqlStart, this.TableName, this.ColumnName("Id"));

            if (ids != null && ids.Count() > 0)
            {
                int i = 0;

                foreach (int id in ids)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    string parameterName = this.ParameterName("Id" + i++);
                    sb.Append(parameterName);
                    command.Parameters.Add(this.ParameterWithValue(parameterName, id));
                }
            }
            else
            {
                sb.Append("NULL");   
            }

            sb.Append(SqlEnd);
            command.CommandText = sb.ToString();

            return command;
        }

        /// <summary>
        /// Creates a select command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="status">The job status to filter results on.</param>
        /// <param name="count">The maximum number of results to select.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <returns>A select command.</returns>
        protected virtual DbCommand CreateSelectCommand(DbConnection connection, JobStatus status, int count, DateTime before)
        {
            const string SqlStart = "SELECT * FROM {0} WHERE {1} = {2} AND {3} < {4} ORDER BY {3}";
            const string SqlEnd = " LIMIT {0}";

            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Status"), status.ToString()));
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Before"), before));

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                SqlStart,
                this.TableName,
                this.ColumnName("Status"),
                this.ParameterName("Status"),
                this.ColumnName("QueueDate"),
                this.ParameterName("Before"));

            if (count > 0)
            {
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Count"), count));
                sb.AppendFormat(CultureInfo.InvariantCulture, SqlEnd, this.ParameterName("Count"));
            }

            sb.Append(";");
            command.CommandText = sb.ToString();

            return command;
        }

        /// <summary>
        /// Creates a select command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="likeName">A string representing a full or partial job name to filter on.</param>
        /// <param name="withStatus">A <see cref="JobStatus"/> to filter on, or null if not applicable.</param>
        /// <param name="inSchedule">A schedule name to filter on, if applicable.</param>
        /// <param name="orderBy">A field to order the resultset by.</param>
        /// <param name="sortDescending">A value indicating whether to order the resultset in descending order.</param>
        /// <param name="pageNumber">The page number to get.</param>
        /// <param name="pageSize">The size of the pages to get.</param>
        /// <returns>A select command.</returns>
        protected virtual DbCommand CreateSelectCommand(DbConnection connection, string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize)
        {
            const string Sql = @"SELECT * FROM {0} WHERE {1} LIKE {2}";

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 0)
            {
                pageSize = 0;
            }

            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Name"), String.Concat("%", (likeName ?? String.Empty).Trim(), "%")));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                Sql,
                this.TableName,
                this.ColumnName("Name"),
                this.ParameterName("Name"));

            if (withStatus != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", this.ColumnName("Status"), this.ParameterName("Status"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Status"), withStatus.Value.ToString()));
            }

            if (!String.IsNullOrEmpty(inSchedule))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", this.ColumnName("ScheduleName"), this.ParameterName("ScheduleName"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("ScheduleName"), withStatus.Value.ToString()));
            }

            sb.AppendFormat(
                CultureInfo.InvariantCulture, 
                " ORDER BY {0} {1} LIMIT {2} OFFSET {3};", 
                this.GetOrderByColumnName(orderBy), 
                sortDescending ? "DESC" : "ASC",
                pageSize,
                (pageNumber - 1) * pageSize);

            command.CommandText = sb.ToString();
            return command;
        }

        /// <summary>
        /// Gets the formatted column name to use for the given <see cref="JobRecordResultsOrderBy"/> value.
        /// </summary>
        /// <param name="orderBy">The order by value to get the column name for.</param>
        /// <returns>A column name.</returns>
        protected virtual string GetOrderByColumnName(JobRecordResultsOrderBy orderBy)
        {
            string orderByColumn = null;

            switch (orderBy)
            {
                case JobRecordResultsOrderBy.FinishDate:
                    orderByColumn = this.ColumnName("FinishDate");
                    break;
                case JobRecordResultsOrderBy.JobType:
                    orderByColumn = this.ColumnName("JobType");
                    break;
                case JobRecordResultsOrderBy.Name:
                    orderByColumn = this.ColumnName("Name");
                    break;
                case JobRecordResultsOrderBy.QueueDate:
                    orderByColumn = this.ColumnName("QueueDate");
                    break;
                case JobRecordResultsOrderBy.ScheduleName:
                    orderByColumn = this.ColumnName("ScheduleName");
                    break;
                case JobRecordResultsOrderBy.StartDate:
                    orderByColumn = this.ColumnName("StartDate");
                    break;
                case JobRecordResultsOrderBy.Status:
                    orderByColumn = this.ColumnName("Status");
                    break;
                default:
                    throw new NotImplementedException();
            }

            return orderByColumn;
        }

        /// <summary>
        /// Get the name of the parameter to use for the given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the parameter name for.</param>
        /// <returns>A parameter name.</returns>
        protected virtual string ParameterName(string propertyName)
        {
            return String.Concat("@", propertyName);
        }

        /// <summary>
        /// Creates a command parameter for the given name and value.
        /// </summary>
        /// <param name="name">The name of the parameter to create.</param>
        /// <param name="value">The value to create the parameter with.</param>
        /// <returns>A command parameter.</returns>
        protected abstract DbParameter ParameterWithValue(string name, object value);

        /// <summary>
        /// Gets the name of the <see cref="JobRecord"/> property for the given database column name.
        /// </summary>
        /// <param name="columnName">The column name to get the property name for.</param>
        /// <returns>A property name.</returns>
        protected virtual string PropertyName(string columnName)
        {
            return columnName;
        }

        #endregion
    }
}
