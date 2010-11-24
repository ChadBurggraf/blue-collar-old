//-----------------------------------------------------------------------
// <copyright file="Service.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Service
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Permissions;
    using System.ServiceProcess;
    using System.Text.RegularExpressions;
    using NLog;
    using NLog.Config;
    using NLog.Targets;

    /// <summary>
    /// <see cref="ServiceBase"/> implementation for the Blue Collar job service.
    /// </summary>
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
    [HostProtectionAttribute(SecurityAction.LinkDemand, SharedState = true, Synchronization = true, ExternalProcessMgmt = true, SelfAffectingProcessMgmt = true)]
    public partial class Service : ServiceBase
    {
        #region Private Fields

        private static string currentDirectory, logsDirectory;
        private static Logger logger;
        private ProcessTuple[] processes = new ProcessTuple[0];
        private bool isRunning;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Service class.
        /// </summary>
        public Service()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Protected Static Properties

        /// <summary>
        /// Gets the path to the assembly's current directory.
        /// </summary>
        protected static string CurrentDirectory
        {
            get
            {
                if (String.IsNullOrEmpty(currentDirectory))
                {
                    currentDirectory = Path.GetDirectoryName(typeof(Service).Assembly.Location);
                }

                return currentDirectory;
            }
        }

        /// <summary>
        /// Gets the logger to use.
        /// </summary>
        protected static Logger Logger
        {
            get
            {
                if (logger == null)
                {
                    if (!Directory.Exists(LogsDirectory))
                    {
                        Directory.CreateDirectory(LogsDirectory);
                    }

                    LogManager.Configuration = CreateLoggingConfiguration(Path.Combine(LogsDirectory, "collar_service.log"));
                    logger = LogManager.GetLogger("BlueCollar.Service");
                }

                return logger;
            }
        }

        /// <summary>
        /// Gets the path to the base logs directory.
        /// </summary>
        protected static string LogsDirectory
        {
            get
            {
                if (String.IsNullOrEmpty(logsDirectory))
                {
                    logsDirectory = Path.Combine(CurrentDirectory, "Logs");
                }

                return logsDirectory;
            }
        }

        #endregion

        #region Protected Instance Methods

        /// <summary>
        /// Runs when a Continue command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service resumes normal functioning after being paused.
        /// </summary>
        protected override void OnContinue()
        {
            lock (this)
            {
                this.isRunning = true;
                this.StopAllProcesses(false);
                this.InitializeProcessTuples();
                this.StartAllProcesses();
            }
        }

        /// <summary>
        /// Executes when a Pause command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service pauses.
        /// </summary>
        protected override void OnPause()
        {
            lock (this)
            {
                this.isRunning = false;
                this.StopAllProcesses(true);
            }
        }

        /// <summary>
        /// Executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Input arguments.</param>
        protected override void OnStart(string[] args)
        {
            lock (this)
            {
                this.isRunning = true;
                this.StopAllProcesses(false);
                this.InitializeProcessTuples();
                this.StartAllProcesses();
            }
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            lock (this)
            {
                this.isRunning = false;
                this.StopAllProcesses(true);
            }
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Creates the configuration to use for logging.
        /// </summary>
        /// <param name="console">A value indicating whether logging to the console is enabled.</param>
        /// <param name="file">A value indicating whether logging to a file is enabled.</param>
        /// <param name="filePath">The path of the log file to write to, if applicable.</param>
        /// <returns>The created logging configuration.</returns>
        private static LoggingConfiguration CreateLoggingConfiguration(string filePath)
        {
            const string Layout = "${date:format=yyyy-MM-dd h\\:mm\\:ss tt} - ${level:uppercase=true} - ${message} ${exception}";

            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget target = new FileTarget();
            target.Layout = Layout;
            target.FileName = filePath;
            target.ArchiveFileName = Path.Combine(Path.GetDirectoryName(filePath), String.Concat(Path.GetFileNameWithoutExtension(filePath), ".{#####}", Path.GetExtension(filePath)));
            target.ArchiveAboveSize = 10485760;
            target.ArchiveNumbering = ArchiveNumberingMode.Sequence;
            target.KeepFileOpen = false;
            config.AddTarget("File", target);

            LoggingRule rule = new LoggingRule("*", LogLevel.Info, target);
            config.LoggingRules.Add(rule);

            return config;
        }

        /// <summary>
        /// Resolves the log path for the application identified by the given configuration element.
        /// </summary>
        /// <param name="element">The configuration element to resolve the log path for.</param>
        /// <returns>A resolved log path.</returns>
        private static string ResolveLogPath(ApplicationElement element)
        {
            string path = element.LogFile;

            if (!String.IsNullOrEmpty(path))
            {
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(CurrentDirectory, path);
                }
            }
            else
            {
                path = Path.Combine(Path.Combine(CurrentDirectory, "Logs"), String.Concat(Regex.Replace(element.Name, @"[^a-zA-Z0-9]+", String.Empty), ".log"));
            }

            return Path.GetFullPath(path);
        }

        #endregion

        #region Private Instance Methods

        /// <summary>
        /// Initializes this instance's <see cref="ProcessTuple"/> collection.
        /// </summary>
        private void InitializeProcessTuples()
        {
            this.processes = new ProcessTuple[BlueCollarServiceSection.Current.Applications.Count];
            string basePath = Path.GetFullPath(Path.GetDirectoryName(GetType().Assembly.Location));
            
            for (int i = 0; i < this.processes.Length; i++)
            {
                var appElement = BlueCollarServiceSection.Current.Applications[i];
                string exePath;
                
                switch (appElement.FrameworkVersion)
                {
                    case FrameworkVersion.FourZero:
                        exePath = Path.Combine(Path.Combine(basePath, "Framework 4.0"), "collar.exe");
                        break;
                    case FrameworkVersion.ThreeFive:
                        exePath = Path.Combine(Path.Combine(basePath, "Framework 3.5"), "collar.exe");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                this.processes[i] = new ProcessTuple()
                {
                    Application = appElement,
                    StartInfo = new ProcessStartInfo()
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        FileName = exePath,
                        Arguments = String.Format(
                            CultureInfo.InvariantCulture,
                            @"-d ""{0}"" -c ""{1}"" -l -lf ""{2}""",
                            appElement.Directory,
                            appElement.CfgFile,
                            ResolveLogPath(appElement))
                    }
                };
            }
        }

        /// <summary>
        /// Raises a process' Exited event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ProcessExited(object sender, EventArgs e)
        {
            Process process = sender as Process;
            ProcessTuple tuple = this.processes.Where(t => t.Process == process).FirstOrDefault();

            tuple.Process.Dispose();
            tuple.Process = null;
            Logger.Info(CultureInfo.InvariantCulture, "A Blue Collar jobs process has exited for application '{0}'.", tuple.Application.Name);
            
            if (this.isRunning)
            {
                this.StartProcess(tuple);
            }
        }

        /// <summary>
        /// Starts all process.
        /// </summary>
        private void StartAllProcesses()
        {
            foreach (ProcessTuple tuple in this.processes)
            {
                this.StartProcess(tuple);
            }
        }

        /// <summary>
        /// Starts the process for the given tuple.
        /// </summary>
        /// <param name="tuple">The tuple to start the process for.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to continue execution no matter what.")]
        private void StartProcess(ProcessTuple tuple)
        {
            try
            {
                if (tuple.Process != null)
                {
                    tuple.Process.Kill();
                    tuple.Process.Dispose();
                }

                tuple.Process = new Process() { StartInfo = tuple.StartInfo, EnableRaisingEvents = true };
                tuple.Process.Exited += new EventHandler(this.ProcessExited);
                tuple.Process.Start();

                Logger.Info(CultureInfo.InvariantCulture, "A Blue Collar jobs process was started for application '{0}'.", tuple.Application.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(CultureInfo.InvariantCulture, "An error occurred starting a Blue Collar jobs process ({0}): {1}", tuple.Application.Name, ex.Message);
                Logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// Stops all processes.
        /// </summary>
        /// <param name="safely">A value indicating whether to issue safe-quit commands.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to continue execution no matter what.")]
        private void StopAllProcesses(bool safely)
        {
            foreach (ProcessTuple tuple in this.processes)
            {
                if (tuple.Process != null)
                {
                    try
                    {
                        if (safely)
                        {
                            tuple.Process.StandardInput.WriteLine("q");
                            Logger.Info(CultureInfo.InvariantCulture, "A Blue Collar jobs process was issued a safe-quit command for application '{0}'.", tuple.Application.Name);
                        }
                        else
                        {
                            try
                            {
                                tuple.Process.Kill();
                                tuple.Process.Dispose();
                                tuple.Process = null;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(CultureInfo.InvariantCulture, "An error occurred when force-quitting a Blue Collar jobs process ({0}): {1}", tuple.Application.Name, ex.Message);
                                Logger.Error(ex.StackTrace);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(CultureInfo.InvariantCulture, "An error occurred stopping a Blue Collar jobs process ({0}): {1}", tuple.Application.Name, ex.Message);
                        Logger.Error(ex.StackTrace);
                    }
                }
            }
        }

        #endregion

        #region ProcessTuple Class

        /// <summary>
        /// Represents a configured job service application and its associated process.
        /// </summary>
        private class ProcessTuple
        {
            /// <summary>
            /// Gets or sets the tuple's <see cref="ApplicationElement"/>.
            /// </summary>
            public ApplicationElement Application { get; set; }

            /// <summary>
            /// Gets or sets the tuple's <see cref="Process"/>.
            /// </summary>
            public Process Process { get; set; }

            /// <summary>
            /// Gets or sets the tuple's <see cref="ProcessStartInfo"/>.
            /// </summary>
            public ProcessStartInfo StartInfo { get; set; }
        }

        #endregion   
    }
}
