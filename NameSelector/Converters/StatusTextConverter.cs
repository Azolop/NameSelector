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

    /// <summary>
    /// 把是否已点 + 点名次序转换为卡片第三行文字：第 N 位（未点时为空）。
    /// </summary>
    public class OrderOnlyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCalled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            int order = 0;
            if (values != null && values.Length > 1 && values[1] is int)
            {
                order = (int)values[1];
            }

            if (isCalled && order > 0)
            {
                return "第" + order + "位";
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
