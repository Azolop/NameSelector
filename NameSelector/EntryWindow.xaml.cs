using System;
using System.Windows;
using NameSelector.Dialogs;
using NameSelector.Models;
using NameSelector.Services;

namespace NameSelector
{
    /// <summary>
    /// 临时功能入口窗口：分流已有的点名窗口与新的周次情况记录表。
    /// 两个功能均为模态打开，关闭后回到本入口。
    /// </summary>
    public partial class EntryWindow : Window
    {
        private const double DesignWidth = 760;
        private const double DesignHeight = 460;

        public EntryWindow()
        {
            InitializeComponent();

            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, DesignWidth, DesignHeight);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, DesignWidth, DesignHeight);
        }

        private void RollCall_Click(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow { Owner = this };
            window.ShowDialog();
        }

        private void Weekly_Click(object sender, RoutedEventArgs e)
        {
            var window = new WeekRecordWindow(LoadData()) { Owner = this };
            window.ShowDialog();
        }

        private AppData LoadData()
        {
            try
            {
                return DataService.LoadOrCreate();
            }
            catch (Exception ex)
            {
                DialogService.Show(this, Prompt.LoadFailure(ex), "功能入口", NoticeKind.Warning);
                return DataService.CreateDefault();
            }
        }
    }
}
