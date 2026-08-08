using System.Windows;

namespace NameSelector
{
    /// <summary>
    /// 启动时检测到上一轮点名未结束时弹出的确认对话框。
    /// </summary>
    public partial class UnfinishedRollCallDialog : Window
    {
        /// <summary>用户是否选择开启新的点名。</summary>
        public bool StartNew { get; private set; }

        public UnfinishedRollCallDialog()
        {
            InitializeComponent();
            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, 520, 260);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, 520, 260);
        }

        private void StartNew_Click(object sender, RoutedEventArgs e)
        {
            StartNew = true;
            Close();
        }

        private void KeepCurrent_Click(object sender, RoutedEventArgs e)
        {
            StartNew = false;
            Close();
        }
    }
}
