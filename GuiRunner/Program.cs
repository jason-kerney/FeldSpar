using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using GuiRunner;

namespace FeldSparGuiCSharp
{
    public static class Program
    {
        private class Launcher : MarshalByRefObject
        {
            public void Launch()
            {
                var app = new App {MainWindow = new TestAssembliesWindow()};
                app.MainWindow.ShowDialog();
                app.Shutdown(0);
            }
        }

        [STAThread]
        public static void Main()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            const string applicationName = "FeldSparGui";

            var appDomain = AppDomain.CreateDomain(applicationName, null, path, path, true);

            var launcherType = typeof (Launcher);
            var sandBoxAssemblyName = launcherType.Assembly.FullName;
            var sandBoxTypeName = launcherType.FullName;

            var sandbox = (Launcher) appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName);

            sandbox.Launch();
        }
    }
}