using System;
using System.ComponentModel;
using System.Linq;

namespace TeamCityConfigMonitor
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;

            // 
            // serviceInstaller
            // 
            this.serviceInstaller.DelayedAutoStart = true;

            this.serviceInstaller.Description = string.Join(", ", Enum.GetValues(typeof(MonitorServiceHost.Service)).Cast<MonitorServiceHost.Service>().Where(x => x != MonitorServiceHost.Service.ServiceHost).Select(x => string.Concat(x.Get("Name"), ": ", x.Get("Description"))));
            this.serviceInstaller.ServiceName = MonitorServiceHost.Service.ServiceHost.Get("Name");
            this.serviceInstaller.DisplayName = MonitorServiceHost.Service.ServiceHost.Get("Description");
            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.serviceProcessInstaller, this.serviceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceInstaller;
    }
}