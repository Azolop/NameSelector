using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using NameSelector.Models;
using NameSelector.Services;

namespace NameSelector
{
    /// <summary>
    /// 主窗口：统计信息栏、功能按钮行、8 列名单卡片阵列。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>比例式自适应的设计基准尺寸（与 XAML 默认窗口尺寸一致）。</summary>
        private const double DesignWidth = 1180;
        private const double DesignHeight = 720;

        /// <summary>同一张卡片两次点击的最小间隔，避免双击误操作。</summary>
        private const int CardClickDebounceMs = 1000;

        private readonly AppData _data;
        private readonly Random _random = new Random();
        private readonly Dictionary<Student, DateTime> _lastCardClick = new Dictionary<Student, DateTime>();

        public MainWindow()
        {
            InitializeComponent();
            _data = LoadData();
            RefreshAll();
            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, DesignWidth, DesignHeight);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, DesignWidth, DesignHeight);
            // 启动后检查上一轮点名是否已结束
            Loaded += (s, e) => CheckUnfinishedRollCall();

            // 按工作区钳制初始尺寸，避免 1024×768 等小屏上窗口超出屏幕
            ClampToWorkArea();
        }

        private AppData LoadData()
        {
            try
            {
                return DataService.LoadOrCreate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Prompt.LoadFailure(ex), "点名分组工具", MessageBoxButton.OK, MessageBoxImage.Warning);
                return DataService.CreateDefault();
            }
        }

        private void ClampToWorkArea()
        {
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
            // 名单重写后旧对象不再参与界面，清空防抖记录，避免引用滞留或误拦新卡片。
            _lastCardClick.Clear();
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
            if (_lastCardClick.TryGetValue(student, out last) && (now - last).TotalMilliseconds < CardClickDebounceMs)
            {
                return;
            }
            _lastCardClick[student] = now;

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
            SaveData();

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

            EndRollCallCore();
        }

        /// <summary>
        /// 结束本轮点名的核心逻辑：清除全部点名记录并把次序重置为初始状态。
        /// </summary>
        private void EndRollCallCore()
        {
            foreach (var student in _data.Students)
            {
                student.IsCalled = false;
                student.Order = 0;
            }
            _data.NextOrder = 1;

            UpdateStats();
            SaveData();
        }

        // ---------- 启动时未结束检测 ----------

        /// <summary>
        /// 当前点名次序（NextOrder）初始为 1；若不是 1，说明上一轮点名尚未结束，
        /// 询问用户是否开启新的点名。
        /// </summary>
        private void CheckUnfinishedRollCall()
        {
            if (_data.NextOrder == 1)
            {
                return;
            }

            var dialog = new UnfinishedRollCallDialog { Owner = this };
            dialog.ShowDialog();
            if (dialog.StartNew)
            {
                EndRollCallCore();
            }
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
            SaveData();
        }

        private void SaveData()
        {
            try
            {
                DataService.Save(_data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Prompt.SaveFailure(ex), "点名分组工具", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
