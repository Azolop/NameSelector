using System.Windows;
using System.Windows.Media;

namespace NameSelector.Dialogs
{
    /// <summary>对话框类型，决定标题与主按钮的主题色。</summary>
    public enum NoticeKind
    {
        /// <summary>普通提示，蓝色。</summary>
        Information,

        /// <summary>成功提示，绿色。</summary>
        Success,

        /// <summary>警告提示，橙色。</summary>
        Warning,

        /// <summary>错误提示，红色。</summary>
        Error,

        /// <summary>询问确认，蓝色。</summary>
        Question
    }

    /// <summary>
    /// 通用提示 / 确认对话框：大字号、大按钮，适合教室白板展示。
    /// 单按钮为提示（点击或回车 / ESC 关闭），双按钮为确认（主按钮返回 Confirmed=true）。
    /// </summary>
    public partial class NoticeDialog : Window
    {
        private const double DesignWidth = 800;
        private const double DesignHeight = 420;

        /// <summary>是否点击了主按钮（确认）。</summary>
        public bool Confirmed { get; private set; }

        public NoticeDialog(string message, string title, NoticeKind kind, string primaryText, string secondaryText)
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;

            Color accent = GetAccentColor(kind);
            TitleText.Foreground = new SolidColorBrush(accent);
            PrimaryButton.Content = primaryText;
            PrimaryButton.Background = new SolidColorBrush(accent);
            PrimaryButton.IsDefault = true;

            if (string.IsNullOrEmpty(secondaryText))
            {
                // 单按钮：回车或 ESC 均可关闭
                PrimaryButton.IsCancel = true;
            }
            else
            {
                SecondaryButton.Content = secondaryText;
                SecondaryButton.Background = new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D));
                SecondaryButton.Visibility = Visibility.Visible;
                SecondaryButton.IsCancel = true;
            }

            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, DesignWidth, DesignHeight);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, DesignWidth, DesignHeight);
        }

        private static Color GetAccentColor(NoticeKind kind)
        {
            switch (kind)
            {
                case NoticeKind.Success:
                    return Color.FromRgb(0x27, 0xAE, 0x60);
                case NoticeKind.Warning:
                    return Color.FromRgb(0xE6, 0x7E, 0x22);
                case NoticeKind.Error:
                    return Color.FromRgb(0xE7, 0x4C, 0x3C);
                default:
                    return Color.FromRgb(0x29, 0x80, 0xB9);
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }
    }
}
