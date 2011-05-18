//-----------------------------------------------------------------------
// <copyright file="LimitTests.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Test
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Limit tests.
    /// </summary>
    [TestClass]
    public sealed class LimitTests
    {
        /// <summary>
        /// Aborted tests.
        /// </summary>
        [TestMethod]
        public void LimitAborted()
        {
            try
            {
                Limit.Invoke(
                    () =>
                    {
                        Thread.Sleep(600);
                        Assert.Fail();
                    },
                    500);
            }
            catch (TimeoutException)
            {
            }
        }

        /// <summary>
        /// Not aborted tests.
        /// </summary>
        [TestMethod]
        public void LimitNotAborted()
        {
            try
            {
                Limit.Invoke(
                    () =>
                    {
                        Thread.Sleep(400);
                    }, 500);
            }
            catch (TimeoutException)
            {
                Assert.Fail();
            }
        }
    }
}
