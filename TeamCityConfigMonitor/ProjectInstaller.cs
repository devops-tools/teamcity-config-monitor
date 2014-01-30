using System.ComponentModel;
using System.Configuration.Install;

namespace TeamCityConfigMonitor
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
