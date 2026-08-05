using System.Windows;
using System.Windows.Input;

namespace NameSelector
{
    /// <summary>
    /// 选人结果窗口：中央大号显示名字，点击任意处关闭。
    /// 仅展示，不修改任何点名状态。
    /// </summary>
    public partial class ResultWindow : Window
    {
        public ResultWindow(string studentName)
        {
            InitializeComponent();
            ResultNameText.Text = studentName;
            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, 660, 400);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, 660, 400);
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Close();
        }
    }
}
