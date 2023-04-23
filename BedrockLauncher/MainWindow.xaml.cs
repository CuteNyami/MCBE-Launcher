using System.Windows;
using Blazored.Modal;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System;
using BedrockLauncher.Utils;
using System.Diagnostics;
using System.Linq;
using LauncherAPI;

namespace BedrockLauncher
{
    public partial class MainWindow : Window
    {
        public static WebView2? WebView;

        public static LauncherServer? Server { get; set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();

        public MainWindow()
        {
            Process proc = Process.GetCurrentProcess();
            int count = Process.GetProcesses().Where(p =>
                p.ProcessName == proc.ProcessName).Count();

            if (count > 1)
            {
                System.Environment.Exit(1);
            }
   
            InitializeComponent();

            var services = new ServiceCollection();
            services.AddWpfBlazorWebView();
            services.AddBlazoredModal();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
            AllocConsole();
#endif
            Resources.Add("services", services.BuildServiceProvider());

            Server = new LauncherServer();
        }

        private void ModifyWebView(object? _, BlazorWebViewInitializedEventArgs e)
        {
            WebView = e.WebView;
            WebView.DefaultBackgroundColor = Color.FromArgb(20, 20, 20);
        }

        [LibraryImport("dwmapi.dll")]
        public static partial int DwmSetWindowAttribute(nint hWnd, int attr, [MarshalAs(UnmanagedType.Bool)] ref bool attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            var windowHelper = new WindowInteropHelper(this);
            var value = true;
            var result = DwmSetWindowAttribute(windowHelper.Handle, 20, ref value, Marshal.SizeOf(value));

            var source = HwndSource.FromHwnd(windowHelper.Handle);
            source?.AddHook(WndProc);

            base.OnSourceInitialized(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Server!.StopServer();
            base.OnClosing(e);
        }

        // https://github.com/JiayiSoftware/JiayiLauncher
        private nint WndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg != 0x004A)
            {
                handled = false;
                return 0;
            }

            handled = true;

            var data = Marshal.PtrToStructure<CopyData>(lParam);
            var args = Marshal.PtrToStringUni(data.lpData);
            if (args != null)
            {
                Arguments.Set(args);
                Activate();
            }

            return 0;
        }
    }
}
