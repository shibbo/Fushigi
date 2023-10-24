using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Fushigi.windowing
{
    internal static class WindowsDarkmodeUtil
    {
        #region WINAPI
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        const string REGISTRY_KEY_WIN_THEME = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string REGISTRY_VAL_USE_LIGHT_THEME = "SystemUsesLightTheme";

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern bool DwmSetWindowAttribute(IntPtr handle, int param, in int value, int size);

        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern bool DwmGetWindowAttribute(IntPtr handle, int param, out int value, int size);


        [DllImport("uxtheme.dll", SetLastError = true)]
        private static extern bool SetWindowTheme(IntPtr handle, string? subAppName, string? subIDList);
        #endregion

        public static void SetDarkmodeAware(IntPtr handle)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10))
                return; //only works on windows 10+


            //Support dark mode on Windows
            //ported from https://github.com/libsdl-org/SDL/issues/4776#issuecomment-926976455

            static void ToggleDarkmode(IntPtr handle, bool value)
            {
                if (!DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, value ? 1 : 0, sizeof(int)))
                    DwmSetWindowAttribute(handle, 19, value ? 1 : 0, sizeof(int));
            }

            SetWindowTheme(handle, "DarkMode_Explorer", null);


            bool isDarkMode = false;

            var windowHandle = handle;

            void CheckDarkmode()
            {
                if (Registry.GetValue(REGISTRY_KEY_WIN_THEME, REGISTRY_VAL_USE_LIGHT_THEME, null) is int value)
                {
                    bool shouldBeDarkMode = value == 0;

                    if (isDarkMode != shouldBeDarkMode)
                    {
                        ToggleDarkmode(windowHandle, shouldBeDarkMode);
                        isDarkMode = shouldBeDarkMode;
                    }
                }
            }

            CheckDarkmode();

            //check every second
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (o, e) => CheckDarkmode();

            timer.Start();
        }
    }
}
