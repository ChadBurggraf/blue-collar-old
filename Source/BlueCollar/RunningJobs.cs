﻿//-----------------------------------------------------------------------
// <copyright file="RunningJobs.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;

    /// <summary>
    /// Represents a collection of running jobs that can be flushed to disk.
    /// </summary>
    [DataContract]
    public sealed class RunningJobs
    {
        private static readonly string defaultPersistencPath = Path.Combine(Path.GetTempPath(), "BlueCollarRunningJobs.xml");
        private static readonly object persistenceFileLocker = new object();
        private List<JobRun> runs;

        /// <summary>
        /// Initializes a new instance of the RunningJobs class.
        /// </summary>
        public RunningJobs()
            : this(defaultPersistencPath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RunningJobs class.
        /// </summary>
        /// <param name="persistencePath">The persistence path to use when persisting run data.</param>
        public RunningJobs(string persistencePath)
        {
            if (String.IsNullOrEmpty(persistencePath))
            {
                persistencePath = defaultPersistencPath;
            }

            if (!Path.IsPathRooted(persistencePath))
            {
                persistencePath = Path.GetFullPath(persistencePath);
            }

            this.PersistencePath = persistencePath;
            this.runs = new List<JobRun>(LoadFromPersisted(this.PersistencePath));
        }

        /// <summary>
        /// Gets the number of job runs this instance contains.
        /// </summary>
        public int Count
        {
            get { return this.runs.Count; }
        }

        /// <summary>
        /// Gets the path used to persist the running jobs state.
        /// </summary>
        public string PersistencePath { get; private set; }

        /// <summary>
        /// Clears all fo the job runs this instance is maintaining.
        /// </summary>
        public void Clear()
        {
            lock (this.runs)
            {
                this.runs.Clear();
            }
        }

        /// <summary>
        /// Gets all of the job runs this instance is maintaining.
        /// </summary>
        /// <returns>A collection of job runs.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "I want to encourange caching the results of this call into a variable.")]
        public IEnumerable<JobRun> GetAll()
        {
            lock (this.runs)
            {
                return this.runs.ToArray();
            }
        }

        /// <summary>
        /// Gets all of the job runs marked as not-running this instance is maintaining.
        /// </summary>
        /// <returns>A collection of job runs.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "I want to encourange caching the results of this call into a variable.")]
        public IEnumerable<JobRun> GetNotRunning()
        {
            lock (this.runs)
            {
                return (from j in this.runs
                        where !j.IsRunning
                        select j).ToArray();
            }
        }

        /// <summary>
        /// Gets all of the job runs marked as running this instance is maintaining.
        /// </summary>
        /// <returns>A collection of job runs.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "I want to encourange caching the results of this call into a variable.")]
        public IEnumerable<JobRun> GetRunning()
        {
            lock (this.runs)
            {
                return (from j in this.runs
                        where j.IsRunning
                        select j).ToArray();
            }
        }

        /// <summary>
        /// Adds a job run to this instance.
        /// </summary>
        /// <param name="jobRun">The job run to add.</param>
        public void Add(JobRun jobRun)
        {
            lock (this.runs)
            {
                this.runs.Add(jobRun);
            }
        }

        /// <summary>
        /// Flushes this instance's state to disk.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity", Justification = "Totally reviewed.")]
        public void Flush()
        {
            lock (this.runs)
            {
                lock (persistenceFileLocker)
                {
                    if (CanWriteToPersisted(this.PersistencePath))
                    {
                        var exceptionTypes = (from r in this.runs
                                              where r.ExecutionException != null
                                              select r.ExecutionException.GetType()).Distinct();

                        DataContractSerializer serializer = new DataContractSerializer(typeof(JobRun[]), exceptionTypes);

                        using (FileStream stream = File.Create(this.PersistencePath))
                        {
                            serializer.WriteObject(stream, this.runs.ToArray());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the job run with the specified ID.
        /// </summary>
        /// <param name="jobId">The ID of the job run to remove.</param>
        public void Remove(int jobId)
        {
            lock (this.runs)
            {
                this.runs.RemoveAll(r => r.JobId == jobId);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current appdomain can read from the persistence path.
        /// </summary>
        /// <param name="persistencePath">The persistence path to check read permissions for.</param>
        /// <returns>True if the appdomain can read, false otherwise.</returns>
        private static bool CanReadFromPersisted(string persistencePath)
        {
#if NET35
            return SecurityManager.IsGranted(new FileIOPermission(FileIOPermissionAccess.Read, persistencePath));   
#else
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read, persistencePath));
            return ps.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
#endif
        }

        /// <summary>
        /// Gets a value indicating whether the current appdomain can write to the persistence path.
        /// </summary>
        /// <param name="persistencePath">The persistence path to check read permissions for.</param>
        /// <returns>True if the appdomain can write, false otherwise.</returns>
        private static bool CanWriteToPersisted(string persistencePath)
        {
#if NET35
            return SecurityManager.IsGranted(new FileIOPermission(FileIOPermissionAccess.Write, persistencePath));   
#else
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new FileIOPermission(FileIOPermissionAccess.Write, persistencePath));
            return ps.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
#endif
        }

        /// <summary>
        /// Loads a collection of job runs for this instance's <see cref="PersistencePath"/>.
        /// </summary>
        /// <returns>The loaded job run collection.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "It's not worth it to enumarte all of the possible failure scenarios. We're okay with failing in general.")]
        private static IEnumerable<JobRun> LoadFromPersisted(string persistencePath)
        {
            IEnumerable<JobRun> runs;

            lock (persistenceFileLocker)
            {
                if (CanReadFromPersisted(persistencePath))
                {
                    if (File.Exists(persistencePath))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(JobRun[]));

                        try
                        {
                            using (FileStream stream = File.OpenRead(persistencePath))
                            {
                                runs = (JobRun[])serializer.ReadObject(stream);
                            }
                        }
                        catch
                        {
                            runs = new JobRun[0];
                        }
                    }
                    else
                    {
                        runs = new JobRun[0];
                    }
                }
                else
                {
                    runs = new JobRun[0];
                }
            }

            DateTime now = DateTime.UtcNow;

            foreach (JobRun job in runs)
            {
                job.SetStateForRecovery(now);
            }

            return runs;
        }
    }
}
