//-----------------------------------------------------------------------
// <copyright file="FileSystemWatcherMode.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Console
{
    using System;

    /// <summary>
    /// Defines the possible <see cref="FileSystemWatcher"/> modes.
    /// </summary>
    public enum FileSystemWatcherMode
    {
        /// <summary>
        /// Identifies that only one event per batch should be raised
        /// on the entire directory being watched. The first event raised
        /// by any of the files in the directory will be published.
        /// FileSystemWatcher.Threshold may need to be set to a fairly
        /// large value for this mode to behave as expected.
        /// </summary>
        Directory,

        /// <summary>
        /// Identifies that the standard event behavior should be used,
        /// except that each file operation will only raise a single event
        /// during the threshold window.
        /// </summary>
        IndividualFiles
    }
}
