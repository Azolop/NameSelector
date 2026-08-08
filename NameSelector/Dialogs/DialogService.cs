using System.Windows;

namespace NameSelector.Dialogs
{
    /// <summary>
    /// 统一的对话框入口：内部使用 NoticeDialog，调用方无需关心窗口细节。
    /// 所有提示与确认都走这里，保证全程序风格一致、字号统一。
    /// </summary>
    public static class DialogService
    {
        /// <summary>单按钮提示。</summary>
        public static void Show(
            Window owner,
            string message,
            string title,
            NoticeKind kind = NoticeKind.Information,
            string buttonText = "确定")
        {
            var dialog = new NoticeDialog(message, title, kind, buttonText, null);
            Prepare(dialog, owner);
            dialog.ShowDialog();
        }

        /// <summary>双按钮确认，返回是否点击了主按钮。</summary>
        public static bool Confirm(
            Window owner,
            string message,
            string title,
            string confirmText = "确定",
            string cancelText = "取消",
            NoticeKind kind = NoticeKind.Question)
        {
            var dialog = new NoticeDialog(message, title, kind, confirmText, cancelText);
            Prepare(dialog, owner);
            dialog.ShowDialog();
            return dialog.Confirmed;
        }

        /// <summary>
        /// 有宿主窗口时居中于宿主，否则（如启动期全局异常）居中于屏幕。
        /// </summary>
        private static void Prepare(NoticeDialog dialog, Window owner)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen;
        }
    }
}
