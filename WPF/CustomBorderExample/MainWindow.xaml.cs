// WPF custom border example using WM_NCCALCSIZE and WM_NCHITTEST
// *** Main window codebehind ***
// Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight)
// Based on: https://docs.microsoft.com/en-us/windows/win32/dwm/customframe
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace CustomBorderExample
{
    public partial class MainWindow : Window
    {
        IntPtr Handle;
        int xborder;
        int yborder;

        public MainWindow()
        {
            InitializeComponent();
        }

        //process mouse clicks for non-client area
        IntPtr HitTestNCA(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // Get the point coordinates for the hit test.
            Point ptMouse = new Point(NativeMethods.GET_X_LPARAM(lParam), NativeMethods.GET_Y_LPARAM(lParam));
                        
            // Get the window rectangle.
            NativeMethods.RECT rcWindow;
            NativeMethods.GetWindowRect(hWnd, out rcWindow);

            // Get the frame rectangle, adjusted for the style without a caption.
            NativeMethods.RECT rcFrame = new NativeMethods.RECT();
            NativeMethods.AdjustWindowRectEx(ref rcFrame, NativeMethods.WS_OVERLAPPEDWINDOW & ~NativeMethods.WS_CAPTION, false, 0);

            // Determine if the hit test is for resizing. Default middle (1,1).
            ushort uRow = 1;
            ushort uCol = 1;
            bool fOnResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (ptMouse.Y >= rcWindow.top && ptMouse.Y < rcWindow.top + yborder + title.ActualHeight)
            {
                fOnResizeBorder = (ptMouse.Y < (rcWindow.top - rcFrame.top));
                uRow = 0;
            }
            else if (ptMouse.Y < rcWindow.bottom && ptMouse.Y >= rcWindow.bottom - yborder)
            {
                uRow = 2;
            }

            // Determine if the point is at the left or right of the window.
            if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + xborder)
            {
                uCol = 0; // left side
            }
            else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - xborder)
            {
                uCol = 2; // right side
            }

            if (uRow == 0 && uCol == 1 && !fOnResizeBorder)
            {
                Point p = grid.PointFromScreen(ptMouse);
                IInputElement elem = grid.InputHitTest(p);

                //we only allow moving window by dragging over title TextBlock
                if (elem != title) return (IntPtr)NativeMethods.HTNOWHERE;
            }

            // Hit test (HTTOPLEFT, ... HTBOTTOMRIGHT)
            IntPtr[,] hitTests = new IntPtr[,]
            {
                { (IntPtr)NativeMethods.HTTOPLEFT, fOnResizeBorder? (IntPtr)NativeMethods.HTTOP : (IntPtr)NativeMethods.HTCAPTION, (IntPtr)NativeMethods.HTTOPRIGHT },
                { (IntPtr)NativeMethods.HTLEFT,  (IntPtr)NativeMethods.HTNOWHERE, (IntPtr)NativeMethods.HTRIGHT},
                { (IntPtr)NativeMethods.HTBOTTOMLEFT, (IntPtr)NativeMethods.HTBOTTOM, (IntPtr)NativeMethods.HTBOTTOMRIGHT },
            };

            return hitTests[uRow, uCol];
        }
                
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            bool fCallDWP = true;
            IntPtr lRet = IntPtr.Zero;

            if (msg == NativeMethods.WM_NCCALCSIZE)
            {
                if (wParam != (IntPtr)0)
                {
                    //remove top edge of the standard non-client area
                    lRet = IntPtr.Zero;

                    NativeMethods.NCCALCSIZE_PARAMS pars = (NativeMethods.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(
                        lParam, typeof(NativeMethods.NCCALCSIZE_PARAMS)
                        );

                    pars.rgrc[0].top = pars.rgrc[0].top;
                    pars.rgrc[0].left = pars.rgrc[0].left + xborder;
                    pars.rgrc[0].right = pars.rgrc[0].right - xborder * 2;
                    pars.rgrc[0].bottom = pars.rgrc[0].bottom - yborder;

                    Marshal.StructureToPtr(pars, lParam, false);

                    handled = true;
                    return lRet;
                }
            }

            if (msg == NativeMethods.WM_NCACTIVATE)
            {
                lRet = (IntPtr)1;
                handled = true;
                return lRet;
            }

            fCallDWP = !NativeMethods.DwmDefWindowProc(hwnd, msg, wParam, lParam, ref lRet);

            if (msg == NativeMethods.WM_NCHITTEST && lRet == IntPtr.Zero)
            {
                //process mouse click on non-client area
                lRet = HitTestNCA(hwnd, wParam, lParam);

                if (lRet != (IntPtr)NativeMethods.HTNOWHERE)
                {
                    fCallDWP = false;
                }
            }

            handled = !fCallDWP;
            return lRet;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper h = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(h.Handle);
            Handle = h.Handle;
            source.AddHook(new HwndSourceHook(WndProc));
            xborder = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSIZEFRAME);
            yborder = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSIZEFRAME);
        }

        private void bMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void bMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal) this.WindowState = System.Windows.WindowState.Maximized;
            else this.WindowState = System.Windows.WindowState.Normal;
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Button 1");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Button 2");
        }
    }
}