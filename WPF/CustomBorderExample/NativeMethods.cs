// WPF custom border example using WM_NCCALCSIZE and WM_NCHITTEST
// *** WinAPI function declarations ***
// Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight)
// Based on: https://docs.microsoft.com/en-us/windows/win32/dwm/customframe
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CustomBorderExample
{
    static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyBottomHeight;
            public int cyTopHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public RECT[] rgrc;
            public IntPtr lppos;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("dwmapi.dll")]
        public static extern bool DwmDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref IntPtr plResult);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(uint smIndex);

        public static int GET_X_LPARAM(IntPtr lp)
        {
            short loword = (short)((ulong)lp & 0xffff);
            return loword;
        }

        public static int GET_Y_LPARAM(IntPtr lp)
        {
            short hiword = (short)((((ulong)lp) >> 16) & 0xffff);
            return hiword;
        }

        public const uint WM_NCCALCSIZE = 0x0083;
        public const uint WM_NCHITTEST = 0x0084;
        public const uint WM_NCACTIVATE = 0x0086;

        public const uint WS_OVERLAPPED = 0x00000000;
        public const uint WS_CAPTION = 0x00C00000;
        public const uint WS_SYSMENU = 0x00080000;
        public const uint WS_THICKFRAME = 0x00040000;
        public const uint WS_MINIMIZEBOX = 0x00020000;
        public const uint WS_MAXIMIZEBOX = 0x00010000;
        public const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU |
              WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

        public const uint HTTOPLEFT = 13;
        public const uint HTTOPRIGHT = 14;
        public const uint HTTOP = 12;
        public const uint HTCAPTION = 2;
        public const uint HTLEFT = 10;
        public const uint HTNOWHERE = 0;
        public const uint HTRIGHT = 11;
        public const uint HTBOTTOM = 15;
        public const uint HTBOTTOMLEFT = 16;
        public const uint HTBOTTOMRIGHT = 17;

        //GetSystemMetrics constants
        public const uint SM_CXSIZEFRAME = 32;
        public const uint SM_CYSIZEFRAME = 33;
    }
}
