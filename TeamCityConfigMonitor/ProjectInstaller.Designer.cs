namespace TeamCityConfigMonitor
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.watchServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.watchServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // watchServiceProcessInstaller
            // 
            this.watchServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.watchServiceProcessInstaller.Password = null;
            this.watchServiceProcessInstaller.Username = null;
            // 
            // watchServiceInstaller
            // 
            this.watchServiceInstaller.DelayedAutoStart = true;
            this.watchServiceInstaller.Description = "Monitors the TeamCity configuration directory and commits changes to Git source control.";
            this.watchServiceInstaller.DisplayName = "TeamCity Config Monitor";
            this.watchServiceInstaller.ServiceName = "WatchService";
            this.watchServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.watchServiceProcessInstaller,
            this.watchServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller watchServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller watchServiceInstaller;
    }
}