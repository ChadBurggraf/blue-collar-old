//-----------------------------------------------------------------------
// <copyright file="BlueCollarConsole.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Console
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using NDesk.Options;
    using NLog;
    using NLog.Config;
    using NLog.Targets;

    /// <summary>
    /// Blue Collar console main application class.
    /// </summary>
    [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public static class BlueCollarConsole
    {
        private static readonly Regex PathQuotesExp = new Regex(@"^[""']?([^""']*)[""']?$", RegexOptions.Compiled);
        private static object bootstrapsLocker = new object();
        private static string config, directory, persistencePath;
        private static bool autoReload;
        private static int pullUpFailCount;
        private static JobRunnerBootstraps bootstraps;
        private static ManualResetEvent exitHandle;
        private static Thread inputThread;
        private static Logger logger;
        
        /// <summary>
        /// Main application entry point.
        /// </summary>
        /// <param name="args">Input arguments.</param>
        /// <returns>The application exit code.</returns>
        public static int Main(string[] args)
        {
            string logPath = null;
            int enableLogging = 0, verbose = 0, help = 0;

            var options = new OptionSet()
            {
                { "d|directory=", "(required) the path to the application directory of the application to run jobs for.", v => directory = v },
                { "c|config=", "the path to the configuration file to use.", v => config = v },
                { "v|verbose", "write session output to the console.", v => { ++verbose; } },
                { "l|log", "write session output to a log file.", v => { ++enableLogging; } },
                { "lf|logfile=", "the path to the log file to write to.", v => logPath = v },
                { "p|persistence=", "the path to the running jobs persistence path to create/user.", v => persistencePath = v },
                { "h|help", "display usage help.", v => { ++help; } }
            };

            try 
            {
                options.Parse(args);
                directory = PathQuotesExp.Replace(directory ?? String.Empty, "$1");
                config = PathQuotesExp.Replace(config ?? String.Empty, "$1");
                logPath = PathQuotesExp.Replace(logPath ?? String.Empty, "$1");
                persistencePath = PathQuotesExp.Replace(persistencePath ?? String.Empty, "$1");
            }
            catch (OptionException ex)
            {
                ShowHelp(options, ex);
                return 1;
            }

            if (help > 0)
            {
                ShowHelp(options, null);
                return 0;
            }

            if (enableLogging > 0 && String.IsNullOrEmpty(logPath))
            {
                logPath = Path.GetFullPath("collar.log");
            }

            LogManager.Configuration = CreateLoggingConfiguration(verbose > 0, enableLogging > 0, logPath);
            logger = LogManager.GetLogger("BlueCollar");

            if (String.IsNullOrEmpty(directory))
            {
                ShowHelp(options, new ArgumentException("You must specify the directory of an application to run jobs for."));
                return 1;
            }
            else if (!Directory.Exists(directory))
            {
                string message = String.Format(CultureInfo.InvariantCulture, "The application directory '{0}' does not exist.", directory);

                if (verbose > 0)
                {
                    logger.Fatal(message);
                }
                else
                {
                    Console.Error.WriteLine(message);
                }

                return 1;
            }

            if (!String.IsNullOrEmpty(config) && !File.Exists(config))
            {
                string message = String.Format(CultureInfo.InvariantCulture, "The configuration file '{0}' does not exist.", config);

                if (verbose > 0)
                {
                    logger.Fatal(message);
                }
                else
                {
                    Console.Error.WriteLine(message);
                }

                return 1;
            }

            if (String.IsNullOrEmpty(persistencePath))
            {
                string above = Path.GetDirectoryName(directory);

                if (!String.IsNullOrEmpty(above))
                {
                    persistencePath = Path.GetFullPath(directory.Substring(above.Length + 1) + ".bin");
                    logger.Info(CultureInfo.InvariantCulture, "Using defaulted running jobs persistence file at '{0}'.", persistencePath);
                }
            }

            autoReload = true;
            exitHandle = new ManualResetEvent(false);
            CreateAndPullUpBootstraps();

            if (bootstraps != null && bootstraps.IsLoaded)
            {
                inputThread = new Thread(new ParameterizedThreadStart(WaitForInput));
                inputThread.Start();

                WaitHandle.WaitAll(new[] { exitHandle });
            }
            else
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Raises the boostraper's AllFinished event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsAllFinished(object sender, EventArgs e)
        {
            if (autoReload)
            {
                logger.Info(CultureInfo.InvariantCulture, "The job runner is restarting.");
                CreateAndPullUpBootstraps();
            }
            else
            {
                logger.Info(CultureInfo.InvariantCulture, "All jobs have finished running. Stay classy, San Diego.");

                inputThread.Abort();
                exitHandle.Set();
            }
        }

        /// <summary>
        /// Raises the boostraper's CancelJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsCancelJob(object sender, JobRecordEventArgs e)
        {
            logger.Info(CultureInfo.InvariantCulture, "Canceled '{0}' ({1})", e.Record.Name, e.Record.Id);
        }

        /// <summary>
        /// Raises the boostraper's ChangeDetected event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsChangeDetected(object sender, FileSystemEventArgs e)
        {
            logger.Info(CultureInfo.InvariantCulture, "A change was detected in '{0}'. The job runner is shutting down (it will be automatically re-started).", e.FullPath);
        }

        /// <summary>
        /// Raises the boostraper's DequeueJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsDequeueJob(object sender, JobRecordEventArgs e)
        {
            logger.Info(CultureInfo.InvariantCulture, "Dequeued '{0}' ({1}).", e.Record.Name, e.Record.Id);
        }

        /// <summary>
        /// Raises the boostraper's Error event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsError(object sender, JobErrorEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Record.Name) && !String.IsNullOrEmpty(e.Record.Exception))
            {
                var element = ExceptionXElement.Parse(e.Record.Exception);
                string message = element.Descendants("Message").First().Value;
                string stackTrace = element.Descendants("StackTrace").First().Value;
                logger.Error(CultureInfo.InvariantCulture, "An error occurred during the run loop for '{0}' ({1}). The message received was: '{2}'", e.Record.Name, e.Record.Id, message);
                logger.Error(stackTrace);
            }
            else if (!String.IsNullOrEmpty(e.Record.Name))
            {
                logger.Error(CultureInfo.InvariantCulture, "An error occurred during the run loop for '{0}' ({1}).", e.Record.Name, e.Record.Id);
            }
            else if (e.Exception != null)
            {
                logger.Error(CultureInfo.InvariantCulture, "An error occurred during the run loop. The message received was: '{0}'", e.Exception.Message);
                logger.Error(e.Exception.StackTrace);
            }
            else
            {
                logger.Error("An unspecified error occurred during the run loop.");
            }
        }

        /// <summary>
        /// Raises the bootstrapper's ExecuteScheduledJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsExecuteScheduledJob(object sender, JobRecordEventArgs e)
        {
            logger.Info(CultureInfo.InvariantCulture, "Started execution of '{0}' ({1}) for schedule '{2}'.", e.Record.Name, e.Record.Id, e.Record.ScheduleName);
        }

        /// <summary>
        /// Raises the boostraper's FinishJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsFinishJob(object sender, JobRecordEventArgs e)
        {
            if (e.Record.Status == JobStatus.Succeeded)
            {
                logger.Info(CultureInfo.InvariantCulture, "'{0}' ({1}) completed successfully.", e.Record.Name, e.Record.Id);
            }
            else
            {
                string message = ExceptionXElement.Parse(e.Record.Exception).Descendants("Message").First().Value;
                logger.Error(CultureInfo.InvariantCulture, "'{0}' ({1}) failed with the message: {2}.", e.Record.Name, e.Record.Id, message);
            }
        }

        /// <summary>
        /// Raises the boostraper's RetryEnqueued event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsRetryEnqueued(object sender, JobRecordEventArgs e)
        {
            logger.Info(CultureInfo.InvariantCulture, "Enqueued retry number {1} of job '{0}' ({2}). The retry is queued to execute in {3:N2} seconds.", e.Record.TryNumber, e.Record.Name, e.Record.Id, DateTime.UtcNow.Subtract(e.Record.QueueDate).TotalSeconds);
        }

        /// <summary>
        /// Raises the boostraper's TimeoutJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsTimeoutJob(object sender, JobRecordEventArgs e)
        {
            logger.Error(CultureInfo.InvariantCulture, "Timed out '{0}' ({1}) because it was taking too long to finish.", e.Record.Name, e.Record.Id);
        }

        /// <summary>
        /// Creates the application's <see cref="JobRunnerBootstraps"/> object, sets up the events and executes the pull-up.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to retry the pull-up operation no matter the reason for failure.")]
        private static void CreateAndPullUpBootstraps()
        {
            lock (bootstrapsLocker)
            {
                if (bootstraps != null)
                {
                    bootstraps.Dispose();
                }

                try
                {
                    bootstraps = new JobRunnerBootstraps(directory, config, persistencePath);
                    bootstraps.AllFinished += new EventHandler(BootstrapsAllFinished);
                    bootstraps.CancelJob += new EventHandler<JobRecordEventArgs>(BootstrapsCancelJob);
                    bootstraps.ChangeDetected += new EventHandler<FileSystemEventArgs>(BootstrapsChangeDetected);
                    bootstraps.DequeueJob += new EventHandler<JobRecordEventArgs>(BootstrapsDequeueJob);
                    bootstraps.Error += new EventHandler<JobErrorEventArgs>(BootstrapsError);
                    bootstraps.ExecuteScheduledJob += new EventHandler<JobRecordEventArgs>(BootstrapsExecuteScheduledJob);
                    bootstraps.FinishJob += new EventHandler<JobRecordEventArgs>(BootstrapsFinishJob);
                    bootstraps.RetryEnqueued += new EventHandler<JobRecordEventArgs>(BootstrapsRetryEnqueued);
                    bootstraps.TimeoutJob += new EventHandler<JobRecordEventArgs>(BootstrapsTimeoutJob);
                    bootstraps.PullUp();

                    pullUpFailCount = 0;

                    string info = String.Format(CultureInfo.InvariantCulture, "The job runner is active at '{0}'", bootstraps.BasePath);

                    if (!String.IsNullOrEmpty(bootstraps.ConfigurationFilePath))
                    {
                        info += String.Format(CultureInfo.InvariantCulture, ", using the configuration file at '{0}'.", bootstraps.ConfigurationFilePath);
                    }

                    logger.Info(info);
                    logger.Info("Press Q+Enter to safely shut down or Ctl+C to exit immediately.");
                }
                catch (Exception ex)
                {
                    pullUpFailCount++;
                    logger.Error(CultureInfo.InvariantCulture, "Failed to bootstrap a job runner at the destination with the message: {0}", ex.Message);
                    logger.Error(ex.StackTrace);
                    TimeoutAndRetryPullUp();
                }
            }
        }

        /// <summary>
        /// Creates the configuration to use for logging.
        /// </summary>
        /// <param name="console">A value indicating whether logging to the console is enabled.</param>
        /// <param name="file">A value indicating whether logging to a file is enabled.</param>
        /// <param name="filePath">The path of the log file to write to, if applicable.</param>
        /// <returns>The created logging configuration.</returns>
        private static LoggingConfiguration CreateLoggingConfiguration(bool console, bool file, string filePath)
        {
            const string Layout = "${date:format=yyyy-MM-dd h\\:mm\\:ss tt} - ${level:uppercase=true} - ${message} ${exception}";

            LoggingConfiguration config = new LoggingConfiguration();

            if (console)
            {
                ColoredConsoleTarget target = new ColoredConsoleTarget();
                target.Layout = Layout;
                config.AddTarget("Console", target);

                LoggingRule rule = new LoggingRule("*", LogLevel.Info, target);
                config.LoggingRules.Add(rule);
            }

            if (file)
            {
                if (String.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentNullException("filePath", "filePath must contain a value if file is true.");
                }

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
            }

            return config;
        }

        /// <summary>
        /// Writes the help message to the console.
        /// </summary>
        /// <param name="options">The options describing usage.</param>
        /// <param name="ex">The exception to write, if applicable.</param>
        private static void ShowHelp(OptionSet options, Exception ex)
        {
            bool error = ex != null;

            if (error)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                options.WriteOptionDescriptions(Console.Error);
            }
            else
            {
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        /// <summary>
        /// Times out the current thread and retries <see cref="CreateAndPullUpBootstraps()"/> after the timeout is complete.
        /// </summary>
        private static void TimeoutAndRetryPullUp()
        {
            if (pullUpFailCount < 10)
            {
                logger.Info("Trying again in 10 seconds.");
                Thread.Sleep(10000);
                CreateAndPullUpBootstraps();
            }
            else
            {
                logger.Fatal("I'm giving up.");
                inputThread.Abort();
                exitHandle.Set();
            }
        }

        /// <summary>
        /// <see cref="ThreadStart"/> delegate used to wait for console input without blocking.
        /// </summary>
        /// <param name="arg">The <see cref="ThreadStart"/> state object.</param>
        private static void WaitForInput(object arg)
        {
            while (true)
            {
                string input = (Console.ReadLine() ?? String.Empty).Trim();

                lock (bootstrapsLocker)
                {
                    if (bootstraps != null && bootstraps.IsLoaded && "q".Equals(input, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Info("The job runner is shutting down.");
                        autoReload = false;
                        bootstraps.PushDown(true);
                        break;
                    }
                }
            }
        }
    }
}
