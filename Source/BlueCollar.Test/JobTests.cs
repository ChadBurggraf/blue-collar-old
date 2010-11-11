//-----------------------------------------------------------------------
// <copyright file="JobTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using BlueCollar.Test.TestJobs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Job tests.
    /// </summary>
    [TestClass]
    public class JobTests
    {
        /// <summary>
        /// Serialize tests.
        /// </summary>
        [TestMethod]
        public void JobSerialize()
        {
            new TestQuickJob().CreateRecord();
        }
    }
}
