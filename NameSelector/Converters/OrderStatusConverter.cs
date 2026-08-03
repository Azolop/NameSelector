using System;
using System.Globalization;
using System.Windows.Data;

namespace NameSelector.Converters
{
    /// <summary>
    /// 把「是否已点 + 点名次序」转换为卡片上的状态文字。
    /// </summary>
    public class OrderStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCalled = values != null && values.Length > 0 && values[0] is bool b && b;
            int order = 0;
            if (values != null && values.Length > 1 && values[1] is int i)
            {
                order = i;
            }

            if (isCalled && order > 0)
            {
                return "已点 · 第" + order + "位";
            }
            return isCalled ? "已点" : "未点";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
