﻿//-----------------------------------------------------------------------
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
    public static class BlueCollarConsole
    {
        private static readonly Regex PathQuotesExp = new Regex(@"^[""']?([^""']*)[""']?$", RegexOptions.Compiled);
        private static object bootstrapsLocker = new object();
        private static string config, directory;
        private static bool autoReload;
        private static int pullUpFailCount;
        private static JobRunnerBootstraps bootstaps;
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
                { "h|help", "display usage help.", v => { ++help; } }
            };

            try 
            {
                options.Parse(args);
                directory = PathQuotesExp.Replace(directory ?? String.Empty, "$1");
                config = PathQuotesExp.Replace(config ?? String.Empty, "$1");
                logPath = PathQuotesExp.Replace(logPath ?? String.Empty, "$1");
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

            autoReload = true;
            exitHandle = new ManualResetEvent(false);
            CreateAndPullUpBootstraps();

            if (bootstaps != null && bootstaps.IsLoaded)
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
                logger.Info("The job runner is restarting.");
                CreateAndPullUpBootstraps();
            }
            else
            {
                logger.Info("All jobs have finished running. Stay classy, San Diego.");

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
            logger.Info("Canceled '{0}' ({1})", e.Record.Name, e.Record.Id);
        }

        /// <summary>
        /// Raises the boostraper's ChangeDetected event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsChangeDetected(object sender, FileSystemEventArgs e)
        {
            logger.Info("A change was detected in '{0}'. The job runner is shutting down (it will be automatically re-started).", e.FullPath);
        }

        /// <summary>
        /// Raises the boostraper's DequeueJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsDequeueJob(object sender, JobRecordEventArgs e)
        {
            logger.Info("Dequeued '{0}' ({1}).", e.Record.Name, e.Record.Id);
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
                logger.Error("An error occurred during the run loop for '{0}' ({1}). The message received was: '{2}'\n{3}", e.Record.Name, e.Record.Id, message, stackTrace);
            }
            else if (!String.IsNullOrEmpty(e.Record.Name))
            {
                logger.Error("An error occurred during the run loop for '{0}' ({1}).", e.Record.Name, e.Record.Id);
            }
            else if (e.Exception != null)
            {
                logger.Error("An error occurred during the run loop. The message received was: '{0}'\n{1}", e.Exception.Message, e.Exception.StackTrace);
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
            logger.Info("Started execution of '{0}' ({1}) for schedule '{2}'.", e.Record.Name, e.Record.Id, e.Record.ScheduleName);
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
                logger.Info("'{0}' ({1}) completed successfully.", e.Record.Name, e.Record.Id);
            }
            else
            {
                string message = ExceptionXElement.Parse(e.Record.Exception).Descendants("Message").First().Value;
                logger.Error("'{0}' ({1}) failed with the message: {2}.", e.Record.Name, e.Record.Id, message);
            }
        }

        /// <summary>
        /// Raises the boostraper's TimeoutJob event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void BootstrapsTimeoutJob(object sender, JobRecordEventArgs e)
        {
            logger.Error("Timed out '{0}' ({1}) because it was taking too long to finish.", e.Record.Name, e.Record.Id);
        }

        /// <summary>
        /// Creates the application's <see cref="JobRunnerBootstraps"/> object, sets up the events and executes the pull-up.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to retry the pull-up operation no matter the reason for failure.")]
        private static void CreateAndPullUpBootstraps()
        {
            lock (bootstrapsLocker)
            {
                if (bootstaps != null)
                {
                    bootstaps.Dispose();
                }

                try
                {
                    bootstaps = new JobRunnerBootstraps(directory, config);
                    bootstaps.AllFinished += new EventHandler(BootstrapsAllFinished);
                    bootstaps.CancelJob += new EventHandler<JobRecordEventArgs>(BootstrapsCancelJob);
                    bootstaps.ChangeDetected += new EventHandler<FileSystemEventArgs>(BootstrapsChangeDetected);
                    bootstaps.DequeueJob += new EventHandler<JobRecordEventArgs>(BootstrapsDequeueJob);
                    bootstaps.Error += new EventHandler<JobErrorEventArgs>(BootstrapsError);
                    bootstaps.ExecuteScheduledJob += new EventHandler<JobRecordEventArgs>(BootstrapsExecuteScheduledJob);
                    bootstaps.FinishJob += new EventHandler<JobRecordEventArgs>(BootstrapsFinishJob);
                    bootstaps.TimeoutJob += new EventHandler<JobRecordEventArgs>(BootstrapsTimeoutJob);
                    bootstaps.PullUp();

                    pullUpFailCount = 0;
                    logger.Info("The job runner is active.\nPress Q+Enter to safely shut down or Ctl+C to exit immediately.");
                }
                catch (Exception ex)
                {
                    pullUpFailCount++;
                    TimeoutAndRetryPullUp(ex.Message + "\n" + ex.StackTrace);
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
            const string Layout = "${date:format=yyyy-MM-dd h\\:mm\\:ss tt} (${level}) - ${message} ${exception}";

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
                target.ArchiveFileName = Path.Combine(Path.GetDirectoryName(filePath), String.Concat(Path.GetFileNameWithoutExtension(filePath), ".{#####}.", Path.GetExtension(filePath)));
                target.ArchiveAboveSize = 1024;
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
        /// Times out the current thread because of the given error message and retries
        /// <see cref="CreateAndPullUpBootstraps()"/> after the timeout is complete.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        private static void TimeoutAndRetryPullUp(string message)
        {
            if (pullUpFailCount < 10)
            {
                logger.Error("Failed to bootstrap a job runner at the destination with the message: {0}\nTrying again in 10 seconds.", message);

                Thread.Sleep(10000);
                CreateAndPullUpBootstraps();
            }
            else
            {
                logger.Fatal("Failed to bootstrap a job runner at the destination application 10 times. I'm giving up.");

                inputThread.Abort();
                exitHandle.Set();
            }
        }

        /// <summary>
        /// <see cref="ThreadStart"/> delegate used to wait for console input without blocking.
        /// </summary>
        /// <param name="obj">The <see cref="ThreadStart"/> state object.</param>
        private static void WaitForInput(object arg)
        {
            while (true)
            {
                bool isLoaded = false;

                lock (bootstrapsLocker)
                {
                    isLoaded = bootstaps != null && bootstaps.IsLoaded;
                }

                if (isLoaded)
                {
                    byte[] buffer = new byte[1];

                    System.Console.OpenStandardInput().BeginRead(
                        buffer,
                        0,
                        buffer.Length,
                        (IAsyncResult result) =>
                        {
                            string input = Encoding.ASCII.GetString(buffer).Trim();

                            if ("q".Equals(input, StringComparison.OrdinalIgnoreCase))
                            {
                                logger.Info("The job runner is shutting down.");
                                autoReload = false;
                                bootstaps.PushDown(true);
                            }
                        },
                        null);
                }

                Thread.Sleep(250);
            }
        }
    }
}
