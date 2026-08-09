using System.Windows;
using System.Windows.Input;

namespace NameSelector.Dialogs
{
    /// <summary>
    /// 通用输入对话框：标题 + 说明 + 文本框 + 确定 / 取消。
    /// 风格与 NoticeDialog 一致，大字号适合教室白板。
    /// </summary>
    public partial class InputDialog : Window
    {
        private const double DesignWidth = 520;
        private const double DesignHeight = 360;

        /// <summary>点击「确定」后的输入内容。</summary>
        public string InputText { get; private set; }

        public InputDialog(string title, string message, string defaultValue)
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            InputBox.Text = defaultValue ?? "";
            InputBox.SelectAll();

            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) =>
            {
                Converters.Scale.ApplyNow(this, DesignWidth, DesignHeight);
                InputBox.Focus();
            };
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, DesignWidth, DesignHeight);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
