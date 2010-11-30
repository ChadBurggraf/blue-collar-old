//-----------------------------------------------------------------------
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
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security;
    using System.Security.Permissions;
    using BlueCollar.Configuration;

    /// <summary>
    /// Represents a collection of running jobs that can be flushed to disk.
    /// </summary>
    public sealed class RunningJobs
    {
        private readonly object persistenceFileLocker = new object();
        private string persistencePath;
        private List<JobRun> runs;

        /// <summary>
        /// Initializes a new instance of the RunningJobs class.
        /// </summary>
        public RunningJobs()
            : this(BlueCollarSection.Current.PersistencePathResolved)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RunningJobs class.
        /// </summary>
        /// <param name="persistencePath">The persistence path to use when persisting run data.</param>
        public RunningJobs(string persistencePath)
        {
            this.PersistencePath = persistencePath;
            this.runs = new List<JobRun>(LoadFromPersisted());
        }

        /// <summary>
        /// Gets the number of job runs this instance contains.
        /// </summary>
        public int Count
        {
            get { return this.runs.Count; }
        }

        /// <summary>
        /// Gets or sets the path used to persist the running jobs state.
        /// </summary>
        public string PersistencePath
        {
            get
            {
                lock (this.persistenceFileLocker)
                {
                    return this.persistencePath;
                }
            }

            set
            {
                lock (this.persistenceFileLocker)
                {
                    this.persistencePath = value;

                    if (String.IsNullOrEmpty(persistencePath))
                    {
                        this.persistencePath = BlueCollarSection.Current.PersistencePath;
                    }

                    if (!Path.IsPathRooted(persistencePath))
                    {
                        this.persistencePath = Path.GetFullPath(persistencePath);
                    }
                }
            }
        }

        /// <summary>
        /// Aborts all running jobs this instance is tracking.
        /// </summary>
        public void AbortAll()
        {
            lock (this.runs)
            {
                foreach (JobRun run in this.runs)
                {
                    run.Abort();
                }
            }
        }

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
        /// Deletes this instance's persisted state from disk.
        /// </summary>
        public void Delete()
        {
            lock (this.persistenceFileLocker)
            {
                if (CanWriteToPersisted(this.PersistencePath) && File.Exists(this.PersistencePath))
                {
                    File.Delete(this.PersistencePath);
                }
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
                lock (this.persistenceFileLocker)
                {
                    if (CanWriteToPersisted(this.PersistencePath))
                    {
                        string directory = Path.GetDirectoryName(this.PersistencePath);

                        if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        BinaryFormatter formatter = new BinaryFormatter();

                        using (FileStream stream = File.Create(this.PersistencePath))
                        {
                            formatter.Serialize(stream, this.runs.Select(r => new PersistedJobRun(r)).ToArray());
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
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity", Justification = "Totally reviewed.")]
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
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity", Justification = "Totally reviewed.")]
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
        /// Loads a collection of job runs from this instance' persistence path.
        /// </summary>
        /// <returns>The loaded job run collection.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "It's not worth it to enumarte all of the possible failure scenarios. We're okay with failing in general.")]
        private IEnumerable<JobRun> LoadFromPersisted()
        {
            IEnumerable<JobRun> runs;

            lock (this.persistenceFileLocker)
            {
                if (CanReadFromPersisted(this.PersistencePath))
                {
                    if (File.Exists(this.PersistencePath))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        
                        try
                        {
                            using (FileStream stream = File.OpenRead(this.PersistencePath))
                            {
                                runs = ((PersistedJobRun[])formatter.Deserialize(stream)).Select(p => new JobRun(p)).ToArray();
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

            return runs;
        }
    }
}
