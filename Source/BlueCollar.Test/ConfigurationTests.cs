//-----------------------------------------------------------------------
// <copyright file="ConfigurationTests.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using BlueCollar.Service;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Configuration tests.
    /// </summary>
    [TestClass]
    public sealed class ConfigurationTests
    {
        /// <summary>
        /// FileSystemWatcher threshold type tests.
        /// </summary>
        [TestMethod]
        public void ConfigurationFileSystemWatcherThresholdType()
        {
            foreach (var application in BlueCollarServiceSection.Current.Applications)
            {
                Assert.IsTrue(0 < application.FileSystemChangeThreshold);
            }
        }
    }
}