//-----------------------------------------------------------------------
// <copyright file="JobRecordResultsOrderBy.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;

    /// <summary>
    /// Defines the possible order-by fields when 
    /// selecting a collection of <see cref="JobRecord"/>s
    /// </summary>
    public enum JobRecordResultsOrderBy
    {
        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.FinishDate"/>.
        /// </summary>
        FinishDate,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.JobType"/>.
        /// </summary>
        JobType,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.Name"/>.
        /// </summary>
        Name,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.QueueDate"/>.
        /// </summary>
        QueueDate,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.ScheduleName"/>.
        /// </summary>
        ScheduleName,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.StartDate"/>.
        /// </summary>
        StartDate,

        /// <summary>
        /// Identifies that the results are ordered by <see cref="JobRecord.Status"/>.
        /// </summary>
        Status
    }
}
