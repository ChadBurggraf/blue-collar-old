﻿//-----------------------------------------------------------------------
// <copyright file="ProjectInstaller.designer.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar.Service
{
    /// <summary>
    /// Service installer designer.
    /// </summary>
    public partial class ProjectInstaller
    {
        private System.ComponentModel.IContainer components = null;
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Executes Blue Collar job processes.";
            this.serviceInstaller1.DisplayName = "Blue Collar Service";
            this.serviceInstaller1.ServiceName = "BlueCollarJobService";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.serviceProcessInstaller1, this.serviceInstaller1 });
        }
    }
}