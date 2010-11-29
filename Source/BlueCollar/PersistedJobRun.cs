//-----------------------------------------------------------------------
// <copyright file="PersistedJobRun.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a <see cref="JobRun"/> that has been persisted to disk.
    /// </summary>
    [Serializable]
    public sealed class PersistedJobRun : ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the PersistedJobRun class.
        /// </summary>
        /// <param name="run">The <see cref="JobRun"/> being persisted.</param>
        public PersistedJobRun(JobRun run)
        {
            if (run == null)
            {
                throw new ArgumentNullException("run", "run cannot be null.");
            }

            this.ExecutionException = run.ExecutionException;
            this.FinishDate = run.FinishDate;
            this.JobId = run.JobId;
            this.JobType = run.Job != null ? run.Job.GetType().AssemblyQualifiedName : null;
            this.JobXml = run.Job != null ? run.Job.Serialize() : null;
            this.ScheduleName = run.ScheduleName;
            this.StartDate = run.StartDate;
        }

        /// <summary>
        /// Initializes a new instance of the PersistedJobRun class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to initialize this instance from.</param>
        /// <param name="context">The source <see cref="StreamingContext"/> for the de-serialization.</param>
        public PersistedJobRun(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info", "info cannot be null.");
            }

            this.ExecutionException = (Exception)info.GetValue("ExecutionException", typeof(Exception));
            this.FinishDate = (DateTime?)info.GetValue("FinishDate", typeof(DateTime?));
            this.JobId = info.GetInt32("JobId");
            this.JobType = info.GetString("JobType");
            this.JobXml = info.GetString("JobXml");
            this.ScheduleName = info.GetString("ScheduleName");
            this.StartDate = (DateTime?)info.GetValue("StartDate", typeof(DateTime?));
        }

        /// <summary>
        /// Gets the job run's execution exception.
        /// </summary>
        public Exception ExecutionException { get; private set; }

        /// <summary>
        /// Gets the job run's finish date.
        /// </summary>
        public DateTime? FinishDate { get; private set; }

        /// <summary>
        /// Gets the job run's job ID.
        /// </summary>
        public int JobId { get; private set; }

        /// <summary>
        /// Gets the job run's job type string.
        /// </summary>
        public string JobType { get; private set; }

        /// <summary>
        /// Gets the job run's serialized job XML string.
        /// </summary>
        public string JobXml { get; private set; }

        /// <summary>
        /// Gets the job run's schedule name.
        /// </summary>
        public string ScheduleName { get; private set; }
        
        /// <summary>
        /// Gets the job run's start date.
        /// </summary>
        public DateTime? StartDate { get; private set; }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data need to serialize the object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination <see cref="StreamingContext"/> for this serialization.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ExecutionException", this.ExecutionException);
            info.AddValue("FinishDate", this.FinishDate);
            info.AddValue("JobId", this.JobId);
            info.AddValue("JobType", this.JobType);
            info.AddValue("JobXml", this.JobXml);
            info.AddValue("ScheduleName", this.ScheduleName);
            info.AddValue("StartDate", this.StartDate);
        }
    }
}
