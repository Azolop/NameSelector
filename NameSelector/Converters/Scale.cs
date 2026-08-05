using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace NameSelector.Converters
{
    /// <summary>
    /// 比例式自适应缩放：
    /// 在 XAML 元素上用附加属性声明基准尺寸，窗口加载（Loaded）时立即按当前尺寸套用一次，
    /// 之后任何布局变化（缩放窗口、新增卡片等）通过 40ms 防抖重新套用。
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

        /// <summary>
        /// 立即按窗口当前尺寸应用一次缩放。应在窗口 Loaded 时调用：
        /// 此时尺寸已稳定、视觉树（含名单卡片）已就绪，一次套用即可保证首帧显示正确字号。
        /// </summary>
        public static void ApplyNow(Window window, double designWidth, double designHeight)
        {
            if (window == null || !window.IsLoaded)
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

            // 低于阈值视为尚未就绪的瞬时尺寸（三个窗口的最小宽高都远大于此）。
            if (width < 300 || height < 200)
            {
                return;
            }

            double scale = Math.Min(width / designWidth, height / designHeight);
            ApplyTo(window, scale);
        }

        /// <summary>
        /// 防抖请求：布局变化（缩放窗口、新增卡片等）时调用，40ms 后应用一次最终尺寸。
        /// 避免 LayoutUpdated 每帧触发时反复整树缩放；尾随机制保证最终尺寸一定会被应用。
        /// </summary>
        public static void RequestApply(Window window, double designWidth, double designHeight)
        {
            if (window == null || !window.IsLoaded)
            {
                return;
            }

            DispatcherTimer timer;
            if (_trailingTimers.TryGetValue(window, out timer))
            {
                return;
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(40);

            // 窗口关闭时主动停止并清理，避免静态字典/事件在窗口关闭瞬间还持有窗口引用。
            EventHandler closedHandler = null;
            closedHandler = (s, e) => CleanupTimer(window, timer, closedHandler);

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                window.Closed -= closedHandler;
                CleanupTimer(window, timer, null);
                ApplyNow(window, designWidth, designHeight);
            };
            window.Closed += closedHandler;
            _trailingTimers[window] = timer;
            timer.Start();
        }

        // 每个窗口最多一个待执行的尾随计时器；窗口关闭或计时器触发时都会主动清理。
        private static readonly Dictionary<Window, DispatcherTimer> _trailingTimers = new Dictionary<Window, DispatcherTimer>();

        private static void CleanupTimer(Window window, DispatcherTimer timer, EventHandler closedHandler)
        {
            timer.Stop();
            _trailingTimers.Remove(window);
            if (closedHandler != null)
            {
                window.Closed -= closedHandler;
            }
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
