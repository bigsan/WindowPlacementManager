/*
 icon source: http://www.softicons.com/free-icons/application-icons/black-icons-by-mike-demetriou/app-expose-icon
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace App.WindowPlacementManager
{
    public partial class Form1 : Form
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private const string WINDOW_STATE_FILENAME = @"WindowPlacement.xml";
        private const int TIMEOUT = 10000;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.ShowBalloonTip(TIMEOUT, "Window Placement Manager is running.", " ", ToolTipIcon.Info);
            this.Hide();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(sender, null);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void restoreStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            restoreState(true);
        }

        private void saveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveState(true);
        }

        private void openDataFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var store = IsolatedStorageFile.GetUserStoreForAssembly();
            var dir = (string)store.GetType().GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store, null);
            Process.Start(dir);
        }

        private void saveState(bool showBalloonTip = false)
        {
            _log.Debug("SaveState");

            var store = IsolatedStorageFile.GetUserStoreForAssembly();
            using (var stream = new IsolatedStorageFileStream(WINDOW_STATE_FILENAME, System.IO.FileMode.Create, store))
            {
                San.Toolkit.WindowPlacementManager.SaveWindowPlacementState(stream);
            }
            
            if (showBalloonTip) notifyIcon1.ShowBalloonTip(TIMEOUT, "Window Placement Saved.", " ", ToolTipIcon.Info);
        }

        private void restoreState(bool showBalloonTip = false)
        {
            _log.Debug("RestoreState");

            var store = IsolatedStorageFile.GetUserStoreForAssembly();
            if (store.FileExists(WINDOW_STATE_FILENAME))
            {
                using (var stream = new IsolatedStorageFileStream(WINDOW_STATE_FILENAME, System.IO.FileMode.Open, store))
                {
                    San.Toolkit.WindowPlacementManager.RestoreWindowPlacementState(stream);
                }

                if (showBalloonTip) notifyIcon1.ShowBalloonTip(TIMEOUT, "Window Placement Restored.", " ", ToolTipIcon.Info);
            }
        }
    }
}
