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
            // 比例式自适应：每次布局更新按窗口尺寸重新套用缩放
            LayoutUpdated += (s, e) => Converters.Scale.Apply(this, 660, 400);
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
