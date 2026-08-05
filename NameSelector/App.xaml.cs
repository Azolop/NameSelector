using System;
using System.Threading;
using System.Windows;

namespace NameSelector
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>单实例互斥锁：防止多开导致 namelist.json 互相覆盖。</summary>
        private static Mutex _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 单实例：同一会话内只允许一个实例，多开时提示后退出。
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "{6528F575-B344-42BE-B3B0-4E3879E65814}", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "点名分组工具已经在运行中，请勿重复打开。",
                    "点名分组工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // 全局异常兜底：UI 线程异常弹窗提示并继续，避免闪退丢数据。
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    "程序发生未处理异常：\n\n" + args.Exception.Message +
                    "\n\n数据已在每次操作时保存到 namelist.json，若界面出现异常请重启程序。",
                    "点名分组工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            // 非 UI 线程致命错误：弹窗提示后进程结束。
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show(
                    "程序发生致命错误：\n\n" + (ex != null ? ex.Message : args.ExceptionObject.ToString()) +
                    "\n\n程序即将退出，最近的数据已保存在 namelist.json。",
                    "点名分组工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // 未持有时忽略
                }
                // .NET 3.5 中 WaitHandle.Dispose() 为显式接口实现，需强转。
                ((IDisposable)_singleInstanceMutex).Dispose();
                _singleInstanceMutex = null;
            }
            base.OnExit(e);
        }
    }
}
