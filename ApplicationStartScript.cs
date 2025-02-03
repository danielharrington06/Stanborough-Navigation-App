using System;
using System.Runtime.InteropServices;
using UnityEngine;
 
namespace VGS.VLR
{
    public class VR_OnApplicationStart : MonoBehaviour
    {
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
 
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);
 
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
 
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr extraData);
 
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
 
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
 
        private HandleRef unityWindowHandle;
 
        public void Awake()
        {
            if (!Application.isEditor)
            {
                PlayerPrefs.SetInt("Fullscreen mode_h3981298716", 0);
                Screen.fullScreen = false;
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Invoke("MaximizeAppWindow", 0.2f);
            }
        }
 
        public void MaximizeAppWindow()
        {
            EnumWindows(EnumWindowsCallBack, IntPtr.Zero);
            ShowWindow(unityWindowHandle.Handle, SW_MAXIMIZE);
        }
 
        public bool EnumWindowsCallBack(IntPtr hWnd, IntPtr lParam)
        {
            int procid;
            GetWindowThreadProcessId(new HandleRef(this, hWnd), out procid);
 
            int currentPID = System.Diagnostics.Process.GetCurrentProcess().Id;
 
            new HandleRef(this, System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
 
            if (procid == currentPID)
            {
                unityWindowHandle = new HandleRef(this, hWnd);
                return false;
            }
 
            return true;
        }
    }
}