//-----------------------------------------------------------------------
// <copyright file="BlueCollarService.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Service
{
    using System;
    using System.ServiceProcess;

    /// <summary>
    /// The main execution handler for collar_service.exe.
    /// </summary>
    public static class BlueCollarService
    {
        /// <summary>
        /// Main execution method.
        /// </summary>
        public static void Main()
        {
            ServiceBase.Run(new ServiceBase[] { new Service() });
        }
    }
}
