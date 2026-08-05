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

        // 每个窗口最近一次应用缩放的时间，以及待执行的尾随计时器。
        // 节流 + 尾随：拖拽期间不逐帧全树缩放，但保证最终尺寸一定会被应用，
        // 避免最后一次 LayoutUpdated 被节流吞掉导致字号停留在旧值。
        private static readonly Dictionary<Window, DateTime> _lastApplyTime = new Dictionary<Window, DateTime>();
        private static readonly Dictionary<Window, DispatcherTimer> _trailingTimers = new Dictionary<Window, DispatcherTimer>();

        /// <summary>
        /// 对窗口整棵视觉树应用缩放。designWidth/designHeight 是该窗口的基准尺寸。
        /// LayoutUpdated 每帧触发，这里按 40ms 节流避免拖拽时反复整树缩放；
        /// 节流窗口内的再次触发会安排一次尾随应用，确保窗口尺寸稳定后仍会执行最后一次。
        /// </summary>
        public static void Apply(Window window, double designWidth, double designHeight)
        {
            if (window == null)
            {
                return;
            }

            // 清理已关闭窗口的残留条目，避免字典强引用泄漏窗口对象及其视觉树。
            PruneClosedWindows();

            // 尺寸未稳定（启动首帧、高 DPI 缩放协商中、最小化等）时先不套用字号，
            // 安排稍后重试；否则会把字号按瞬时极小尺寸压到最小值，等窗口定下真实尺寸后
            // 再跳变回正确值，表现为“字体先小后大”。
            if (!HasUsableSize(window))
            {
                ScheduleTrailing(window, designWidth, designHeight);
                return;
            }

            DateTime now = DateTime.Now;
            DateTime last;
            if (!_lastApplyTime.TryGetValue(window, out last) ||
                (now - last).TotalMilliseconds >= 40)
            {
                _lastApplyTime[window] = now;
                CancelTrailing(window);
                ApplyNow(window, designWidth, designHeight);
                return;
            }

            // 40ms 内再次触发：安排一次尾随应用。
            ScheduleTrailing(window, designWidth, designHeight);
        }

        /// <summary>
        /// 判断窗口是否已有可用尺寸。三个窗口的最小宽高（900×560、420×380、660×400）
        /// 都远大于该阈值，因此低于阈值只可能是尚未就绪的瞬时尺寸。
        /// </summary>
        private static bool HasUsableSize(Window window)
        {
            double width = window.ActualWidth;
            double height = window.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                width = window.Width;
                height = window.Height;
            }
            return width >= 300 && height >= 200;
        }

        /// <summary>
        /// 清理已关闭窗口在字典中的残留条目（键为强引用，不清理会阻止窗口对象回收）。
        /// 条目数很少，每次 Apply 顺带扫描一次的开销可忽略。
        /// </summary>
        private static void PruneClosedWindows()
        {
            var closedKeys = new List<Window>();
            foreach (var pair in _lastApplyTime)
            {
                if (!pair.Key.IsLoaded)
                {
                    closedKeys.Add(pair.Key);
                }
            }
            foreach (var key in closedKeys)
            {
                _lastApplyTime.Remove(key);
            }

            var closedTimers = new List<Window>();
            foreach (var pair in _trailingTimers)
            {
                if (!pair.Key.IsLoaded)
                {
                    closedTimers.Add(pair.Key);
                }
            }
            foreach (var key in closedTimers)
            {
                _trailingTimers[key].Stop();
                _trailingTimers.Remove(key);
            }
        }

        private static void ApplyNow(Window window, double designWidth, double designHeight)
        {
            if (!window.IsLoaded)
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

        private static void ScheduleTrailing(Window window, double designWidth, double designHeight)
        {
            DispatcherTimer timer;
            if (_trailingTimers.TryGetValue(window, out timer))
            {
                return;
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(40);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _trailingTimers.Remove(window);
                if (!window.IsLoaded)
                {
                    return;
                }
                _lastApplyTime.Remove(window); // 强制重新应用一次，套用最终尺寸
                Apply(window, designWidth, designHeight);
            };
            _trailingTimers[window] = timer;
            timer.Start();
        }

        private static void CancelTrailing(Window window)
        {
            DispatcherTimer timer;
            if (_trailingTimers.TryGetValue(window, out timer))
            {
                timer.Stop();
                _trailingTimers.Remove(window);
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
