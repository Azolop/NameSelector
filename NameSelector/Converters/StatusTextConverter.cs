using System;
using System.Globalization;
using System.Windows.Data;

namespace NameSelector.Converters
{
    /// <summary>
    /// 把是否已点转换为卡片第二行状态文字：已点 / 未点。
    /// </summary>
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCalled = value is bool && (bool)value;
            return isCalled ? "已点" : "未点";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
