//-----------------------------------------------------------------------
// <copyright file="Job.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using BlueCollar.Configuration;

    /// <summary>
    /// Base <see cref="IJob"/> implementation.
    /// </summary>
    [DataContract(Namespace = Job.XmlNamespace)]
    public abstract class Job : IJob
    {
        #region Public Fields

        /// <summary>
        /// Gets the XML namespace used during job serialization.
        /// </summary>
        public const string XmlNamespace = "http://tastycodes.com/blue-collar/";

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets the job's display name.
        /// </summary>
        [IgnoreDataMember]
        public abstract string Name { get; }

        /// <summary>
        /// Gets the number of times the job can be retried if it fails.
        /// When not overridden, defaults to 0 (no retries).
        /// </summary>
        [IgnoreDataMember]
        public virtual int Retries
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the timeout, in miliseconds, the job is allowed to run for.
        /// When not overridden, defaults to 60,000 (1 minute).
        /// </summary>
        [IgnoreDataMember]
        public virtual long Timeout 
        { 
            get { return 60000; }
        }

        /// <summary>
        /// Gets or sets the try number the job is executing for.
        /// </summary>
        [IgnoreDataMember]
        public int TryNumber { get; set; }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Deserializes the given job type string and serialized data string into an <see cref="IJob"/> instance.
        /// </summary>
        /// <param name="jobType">The job type string to deserialize.</param>
        /// <param name="data">The data string to deserialize.</param>
        /// <returns>An <see cref="IJob"/> instance.</returns>
        public static IJob Deserialize(string jobType, string data)
        {
            if (String.IsNullOrEmpty(jobType))
            {
                throw new ArgumentNullException("jobType", "jobType must contain a value.");
            }

            if (String.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException("data", "data must contain a value.");
            }

            DataContractSerializer serializer = new DataContractSerializer(Type.GetType(jobType, true));

            using (StringReader sr = new StringReader(data))
            {
                using (XmlReader xr = new XmlTextReader(sr))
                {
                    return (IJob)serializer.ReadObject(xr);
                }
            }
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Creates a new job record representing an enqueue-able state for this instance.
        /// </summary>
        /// <returns>The created job record.</returns>
        public virtual JobRecord CreateRecord()
        {
            return new JobRecord()
            {
                Data = this.Serialize(),
                JobType = JobRecord.JobTypeString(this),
                Name = this.Name,
                QueueDate = DateTime.UtcNow,
                Status = JobStatus.Queued
            };
        }

        /// <summary>
        /// Enqueues the job for execution.
        /// </summary>
        /// <returns>The job record that was persisted.</returns>
        public virtual JobRecord Enqueue()
        {
            return this.Enqueue(JobStore.Current);
        }

        /// <summary>
        /// Enqueues the job for execution using the given <see cref="IJobStore"/>.
        /// </summary>
        /// <param name="store">The job store to use when queueing the job.</param>
        /// <returns>The job record that was persisted.</returns>
        public virtual JobRecord Enqueue(IJobStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store", "store cannot be null.");
            }

            JobRecord record = this.CreateRecord();
            store.SaveJob(record);

            return record;
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Serializes the job state for enqueueing.
        /// </summary>
        /// <returns>The serialized job data.</returns>
        public virtual string Serialize()
        {
            DataContractSerializer serializer = new DataContractSerializer(this.GetType());
            StringBuilder sb = new StringBuilder();

            using (StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                using (XmlWriter xw = new XmlTextWriter(sw))
                {
                    serializer.WriteObject(xw, this);
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
