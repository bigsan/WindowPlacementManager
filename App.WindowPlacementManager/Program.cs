using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace App.WindowPlacementManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;

            var appMutexName = "WindowPlacementManager_D1380F19-4DB4-44E2-9F8F-17151DAF7EDC";
            Mutex m = new Mutex(true, appMutexName, out createdNew);

            if (!createdNew)
            {
                // app is already running…
                //MessageBox.Show("Only one instance of this application is allowed at a time.");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
