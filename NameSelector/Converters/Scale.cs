using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace NameSelector.Converters
{
    /// <summary>
    /// 比例式自适应缩放：
    /// 在 XAML 元素上用附加属性声明基准尺寸，窗口在每次布局更新时
    /// 按 min(当前宽/设计宽, 当前高/设计高) 计算缩放比例并统一套用。
    /// 用法：
    ///   converters:Scale.FontSize="22,20"  （字号基准 22，最小 20）
    ///   converters:Scale.Height="88,64"    （最小高度基准 88，最小 64）
    /// </summary>
    public static class Scale
    {
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.RegisterAttached(
                "FontSize",
                typeof(string),
                typeof(Scale),
                new PropertyMetadata(null));

        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.RegisterAttached(
                "Height",
                typeof(string),
                typeof(Scale),
                new PropertyMetadata(null));

        public static void SetFontSize(DependencyObject element, string value)
        {
            element.SetValue(FontSizeProperty, value);
        }

        public static string GetFontSize(DependencyObject element)
        {
            return (string)element.GetValue(FontSizeProperty);
        }

        public static void SetHeight(DependencyObject element, string value)
        {
            element.SetValue(HeightProperty, value);
        }

        public static string GetHeight(DependencyObject element)
        {
            return (string)element.GetValue(HeightProperty);
        }

        private static DateTime _lastApplyTime = DateTime.MinValue;

        /// <summary>
        /// 对窗口整棵视觉树应用缩放。designWidth/designHeight 是该窗口的基准尺寸。
        /// LayoutUpdated 每帧触发，这里按 40ms 节流，避免拖拽窗口时反复整树缩放导致卡顿。
        /// </summary>
        public static void Apply(Window window, double designWidth, double designHeight)
        {
            DateTime now = DateTime.Now;
            if ((now - _lastApplyTime).TotalMilliseconds < 40)
            {
                return;
            }
            _lastApplyTime = now;

            if (window == null)
            {
                return;
            }

            double width = window.ActualWidth;
            double height = window.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                width = window.Width;
                height = window.Height;
                if (width <= 0 || height <= 0)
                {
                    return;
                }
            }

            double scale = Math.Min(width / designWidth, height / designHeight);
            ApplyTo(window, scale);
        }

        private static void ApplyTo(DependencyObject current, double scale)
        {
            string fontSize = GetFontSize(current);
            if (!string.IsNullOrEmpty(fontSize))
            {
                current.SetValue(TextElement.FontSizeProperty, ScaledValue(fontSize, scale));
            }

            string height = GetHeight(current);
            if (!string.IsNullOrEmpty(height))
            {
                var element = current as FrameworkElement;
                if (element != null)
                {
                    element.MinHeight = ScaledValue(height, scale);
                }
            }

            int childCount = VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < childCount; i++)
            {
                ApplyTo(VisualTreeHelper.GetChild(current, i), scale);
            }
        }

        /// <summary>
        /// 解析 "基准,最小" 并返回 基准×缩放，且不小于最小。
        /// </summary>
        private static double ScaledValue(string spec, double scale)
        {
            string[] parts = spec.Split(',');
            double baseValue = 18;
            double minValue = 18;
            if (parts.Length > 0)
            {
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out baseValue);
            }
            if (parts.Length > 1)
            {
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out minValue);
            }
            return Math.Max(baseValue * scale, minValue);
        }
    }
}
