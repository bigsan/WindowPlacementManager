/*
 reference: http://blogs.msdn.com/b/davidrickard/archive/2010/03/09/saving-window-size-and-location-in-wpf-and-winforms.aspx
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace San.Toolkit
{
    public class WindowPlacementManager
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        #region Serializer helper functions
        
        private static readonly System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(WINDOWPLACEMENT));
        
        private static string serialize(object data)
        {
            var sw = new StringWriter();
            serializer.Serialize(sw, data);
            return sw.ToString();
        }

        private static WINDOWPLACEMENT deserialize(string data)
        {
            return (WINDOWPLACEMENT)serializer.Deserialize(new StringReader(data));
        }

        #endregion

        public static void SetPlacement(int windowHandle, string placementData, bool raiseExceptionOnFail = false)
        {
            if (string.IsNullOrEmpty(placementData)) return;

            WINDOWPLACEMENT placement = deserialize(placementData);

            if (!SetWindowPlacement(windowHandle, ref placement) && raiseExceptionOnFail)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static string GetPlacement(int windowHandle)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(windowHandle, out placement);

            return serialize(placement);
        }

        public static void SaveWindowPlacementState(Stream stream, Predicate<XElement> windowFilter = null)
        {
            var xml = new XElement("WindowPlacementList", new XAttribute("created", DateTime.Now));

            EnumWindows((handle, lparam) =>
            {
                var sb = new StringBuilder(255);
                GetWindowText(handle, sb, sb.Capacity);
                string title = sb.ToString();

                if (!String.IsNullOrWhiteSpace(title) && IsWindowVisible(handle))
                {
                    int processId;
                    GetWindowThreadProcessId(handle, out processId);
                    string processName = Process.GetProcessById(processId).ProcessName;

                    var placementElement = XElement.Parse(GetPlacement(handle));
                    placementElement.RemoveAttributes();

                    var win = new XElement("Window",
                        new XElement("Handle", handle),
                        new XElement("ProcessName", processName),
                        new XElement("WindowTitle", title),
                        placementElement
                    );

                    if (windowFilter == null || windowFilter(win))
                    {
                        xml.Add(win);
                    }
                }

                return true;
            }, 0);

            xml.Save(stream);
        }

        public static void RestoreWindowPlacementStateStrict(Stream stream, Predicate<XElement> windowFilter = null)
        {
            var xml = XElement.Load(stream);
            xml.Elements("Window").ToList().ForEach(x =>
            {
                int handle = Convert.ToInt32(x.Element("Handle").Value);

                if (windowFilter == null || windowFilter(x))
                {
                    SetPlacement(handle, x.Element("WINDOWPLACEMENT").ToString());
                }
            });
        }

        public static void RestoreWindowPlacementState(Stream stream, Predicate<XElement> windowFilter = null)
        {
            var xml = XElement.Load(stream);
            EnumWindows((handle, lparam) =>
            {
                // find window by handle
                var win = xml.Elements("Window").FirstOrDefault(w => w.Element("Handle").Value == handle.ToString());

                // if handle not found, find window by process name & title
                if (win == null)
                {
                    int processId;
                    GetWindowThreadProcessId(handle, out processId);
                    string processName = Process.GetProcessById(processId).ProcessName;

                    var sb = new StringBuilder(255);
                    GetWindowText(handle, sb, sb.Capacity);
                    string title = sb.ToString();

                    win = xml.Elements("Window").FirstOrDefault(w => w.Element("ProcessName").Value == processName && w.Element("WindowTitle").Value == title);
                }

                if (win != null && (windowFilter == null || windowFilter(win)))
                {
                    var expectedHandle = Convert.ToInt32(win.Element("Handle").Value);
                    var processName = win.Element("ProcessName").Value;
                    var title = win.Element("WindowTitle").Value;
                    _log.Debug("*** Handle Found: {0}, Process: {1}, Title: {2}", expectedHandle == handle, processName, title);

                    SetPlacement(handle, win.Element("WINDOWPLACEMENT").ToString());
                }

                return true;
            }, 0);
        }

        public delegate bool EnumWindowsProc(int hwnd, int lparam);


        // RECT structure required by WINDOWPLACEMENT structure
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        // POINT structure required by WINDOWPLACEMENT structure
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        // WINDOWPLACEMENT stores the position, size, and state of a window
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT minPosition;
            public POINT maxPosition;
            public RECT normalPosition;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void GetWindowText(int h, StringBuilder s, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindowVisible(int h);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(int handle, out int processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPlacement(int hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowPlacement(int hWnd, out WINDOWPLACEMENT lpwndpl);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms632611(v=vs.85).aspx
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWNORMAL = 1;
    }
}
