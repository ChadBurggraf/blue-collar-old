//-----------------------------------------------------------------------
// <copyright file="SqlServerJobStore.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Implements <see cref="IJobStore"/> for Microsoft SQL Server.
    /// </summary>
    public class SqlServerJobStore : SqlJobStore
    {
        /// <summary>
        /// Initializes a new instance of the SqlServerJobStore class.
        /// </summary>
        public SqlServerJobStore()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SqlServerJobStore class.
        /// </summary>
        /// <param name="connectionString">The connection string to use when connecting to the database.</param>
        public SqlServerJobStore(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Gets the SQL used to select the last inserted record ID.
        /// </summary>
        protected override string SelectLastInsertIdSql
        {
            get { return "SELECT SCOPE_IDENTITY();"; }
        }

        /// <summary>
        /// Creates and opens a connection to the SQL job store.
        /// </summary>
        /// <returns>The created connection.</returns>
        protected override DbConnection CreateAndOpenConnection()
        {
            SqlConnection connection = new SqlConnection(this.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Creates a select command.
        /// </summary>
        /// <param name="connection">The connection to create the command with.</param>
        /// <param name="status">The job status to filter results on.</param>
        /// <param name="count">The maximum number of results to select.</param>
        /// <param name="before">The queued-after date to filter on.</param>
        /// <returns>A select command.</returns>
        protected override DbCommand CreateSelectCommand(DbConnection connection, JobStatus status, int count, DateTime before)
        {
            const string SqlStart = "SELECT{0} * FROM {1} WHERE {2} = {3} AND {4} < {5} ORDER BY {4};";
            
            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Status"), status.ToString()));
            command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Before"), before));

            string countString = String.Empty;

            if (count > 0)
            {
                countString = String.Format(CultureInfo.InvariantCulture, " TOP ({0})", this.ParameterName("Count"));
                command.Parameters.Add(this.ParameterWithValue(this.ParameterName("Count"), count));
            }

            command.CommandText = String.Format(
                CultureInfo.InvariantCulture,
                SqlStart,
                countString,
                this.TableName,
                this.ColumnName("Status"),
                this.ParameterName("Status"),
                this.ColumnName("QueueDate"),
                this.ParameterName("Before"));

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
        protected override DbCommand CreateSelectCommand(DbConnection connection, string likeName, JobStatus? withStatus, string inSchedule, JobRecordResultsOrderBy orderBy, bool sortDescending, int pageNumber, int pageSize)
        {
            const string Sql = @"SELECT * FROM (SELECT *, ROW_NUMBER() OVER(ORDER BY {0} {1}) AS {2} FROM {3} WHERE {4} LIKE {5}";

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
            command.Parameters.Add(this.ParameterWithValue(ParameterName("Name"), String.Concat("%", (likeName ?? String.Empty).Trim(), "%")));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                Sql,
                GetOrderByColumnName(orderBy),
                sortDescending ? "DESC" : "ASC",
                ColumnName("RowNumber"),
                TableName,
                ColumnName("Name"),
                ParameterName("Name"));

            if (withStatus != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", ColumnName("Status"), ParameterName("Status"));
                command.Parameters.Add(this.ParameterWithValue(ParameterName("Status"), withStatus.Value.ToString()));
            }

            if (!String.IsNullOrEmpty(inSchedule))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AND {0} = {1}", ColumnName("ScheduleName"), ParameterName("ScheduleName"));
                command.Parameters.Add(this.ParameterWithValue(ParameterName("ScheduleName"), withStatus.Value.ToString()));
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, ") t WHERE {0} > {1} AND {0} <= {2};", ColumnName("RowNumber"), ParameterName("SkipFrom"), ParameterName("SkipTo"));
            command.CommandText = sb.ToString();

            int skipFrom = (pageNumber - 1) * pageSize;
            command.Parameters.Add(this.ParameterWithValue(ParameterName("SkipFrom"), skipFrom));
            command.Parameters.Add(this.ParameterWithValue(ParameterName("SkipTo"), skipFrom + pageSize));

            return command;
        }

        /// <summary>
        /// Creates a command parameter for the given name and value.
        /// </summary>
        /// <param name="name">The name of the parameter to create.</param>
        /// <param name="value">The value to create the parameter with.</param>
        /// <returns>A command parameter.</returns>
        protected override DbParameter ParameterWithValue(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }
    }
}
