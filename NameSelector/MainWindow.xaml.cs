using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NameSelector.Models;
using NameSelector.Services;

namespace NameSelector
{
    /// <summary>
    /// 主窗口：统计信息栏、功能按钮行、8 列名单卡片阵列。
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppData _data;
        private Random _random = new Random();
        private readonly Dictionary<int, DateTime> _lastCardClick = new Dictionary<int, DateTime>();

        public MainWindow()
        {
            InitializeComponent();
            _data = DataService.Load();
            RefreshAll();
            // 比例式自适应：每次布局更新按窗口尺寸重新套用缩放
            LayoutUpdated += (s, e) => Converters.Scale.Apply(this, 1180, 720);

            // 按工作区钳制初始尺寸，避免 1024×768 等小屏上窗口超出屏幕
            Rect workArea = SystemParameters.WorkArea;
            if (Width > workArea.Width)
            {
                Width = workArea.Width;
            }
            if (Height > workArea.Height)
            {
                Height = workArea.Height;
            }
            if (MinWidth > workArea.Width)
            {
                MinWidth = workArea.Width;
            }
            if (MinHeight > workArea.Height)
            {
                MinHeight = workArea.Height;
            }
        }

        private void RefreshAll()
        {
            StudentGrid.ItemsSource = _data.Students;
            UpdateStats();
            EmptyHint.Visibility = _data.Students.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateStats()
        {
            int total = _data.Students.Count;
            int called = _data.Students.Count(s => s.IsCalled);
            TotalCountText.Text = total.ToString();
            CalledCountText.Text = called.ToString();
            UncalledCountText.Text = (total - called).ToString();
        }

        // ---------- 点名卡片 ----------

        private void StudentCard_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            var student = element.DataContext as Student;
            if (student == null)
            {
                return;
            }

            // 防抖：同一张卡片 1 秒内不应再次改变状态，避免双击误操作
            DateTime now = DateTime.Now;
            DateTime last;
            if (_lastCardClick.TryGetValue(student.Id, out last) && (now - last).TotalMilliseconds < 1000)
            {
                return;
            }
            _lastCardClick[student.Id] = now;

            if (student.IsCalled)
            {
                // 已点 → 未点：清除次序，NextOrder 不变。
                student.IsCalled = false;
                student.Order = 0;
            }
            else
            {
                // 未点 → 已点：分配当前次序并递增。
                student.IsCalled = true;
                student.Order = _data.NextOrder;
                _data.NextOrder++;
            }

            UpdateStats();
            DataService.Save(_data);

            // 检查是否所有人都已点完
            int uncalled = _data.Students.Count(s => !s.IsCalled);
            if (uncalled == 0 && _data.Students.Count > 0)
            {
                MessageBox.Show(this, "所有同学都已点名完毕！", "点名完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ---------- 结束本轮点名 ----------

        private void EndRollCall_Click(object sender, RoutedEventArgs e)
        {
            if (_data.Students.Count == 0)
            {
                MessageBox.Show(this, "名单为空，没有可结束的点名。", "结束本轮点名", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                this,
                "确定要结束本轮点名吗？\n所有点名记录将被清除。",
                "结束本轮点名",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var student in _data.Students)
            {
                student.IsCalled = false;
                student.Order = 0;
            }
            _data.NextOrder = 1;

            UpdateStats();
            DataService.Save(_data);

            // 重置随机实例，避免重置后连续抽中同一人
            _random = new Random();
        }

        // ---------- 随机选人 ----------

        private void RandomAll_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomResult(_data.Students.ToList(), "名单为空，请先点击「修改名单」添加学生。");
        }

        private void RandomUncalled_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomResult(_data.Students.Where(s => !s.IsCalled).ToList(), "所有同学都已被点过名了。");
        }

        private void RandomCalled_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomResult(_data.Students.Where(s => s.IsCalled).ToList(), "还没有同学被点过名。");
        }

        private void ShowRandomResult(List<Student> pool, string emptyMessage)
        {
            if (pool.Count == 0)
            {
                MessageBox.Show(this, emptyMessage, "随机选人", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Student picked = pool[_random.Next(pool.Count)];
            var resultWindow = new ResultWindow(picked.Name) { Owner = this };
            resultWindow.ShowDialog();
        }

        // ---------- 修改名单 ----------

        private void EditList_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditListDialog(_data) { Owner = this };
            dialog.ShowDialog();
            if (dialog.Saved)
            {
                RefreshAll();
            }
        }

        // ---------- 关闭窗口时兜底保存 ----------

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DataService.Save(_data);
        }
    }
}
