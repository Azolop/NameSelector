using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NameSelector.Dialogs;
using NameSelector.Models;
using NameSelector.Services;

namespace NameSelector
{
    /// <summary>
    /// 周次情况记录表窗口：按当前名单生成某一周次的表（同一周次只能有一张），
    /// 表格按小组分组展示，组长 / 组员行 + 背默 / 课堂 / 作业 / 总结列，
    /// 每个交叉单元格均可编辑。每张表独立保存为一个 JSON 文件。
    /// </summary>
    public partial class WeekRecordWindow : Window
    {
        private const double DesignWidth = 1180;
        private const double DesignHeight = 720;

        private readonly AppData _data;
        private List<WeeklyRecordTable> _tables = new List<WeeklyRecordTable>();
        private WeeklyRecordTable _currentTable;

        /// <summary>程序性刷新下拉框 / 重建表格时为 true，避免误触发编辑或重复重建。</summary>
        private bool _loading;

        /// <summary>当前表是否有未保存的修改。</summary>
        private bool _dirty;

        public WeekRecordWindow(AppData data)
        {
            InitializeComponent();
            _data = data;

            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, DesignWidth, DesignHeight);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, DesignWidth, DesignHeight);

            RefreshTableList();
            ClampToWorkArea();
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

        // ---------- 下拉框 ----------

        private void RefreshTableList()
        {
            _tables = WeeklyRecordService.LoadAll();

            _loading = true;
            WeekCombo.ItemsSource = _tables;
            if (_tables.Count > 0)
            {
                WeekCombo.SelectedIndex = 0;
            }
            _loading = false;

            EmptyHint.Visibility = _tables.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SaveButton.IsEnabled = _tables.Count > 0;

            if (_tables.Count == 0)
            {
                TableHost.Content = null;
                _currentTable = null;
                _dirty = false;
                UpdateStatus();
            }
            else
            {
                BuildGrid(_tables[0]);
            }
        }

        private void WeekCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            var table = WeekCombo.SelectedItem as WeeklyRecordTable;
            if (table == null)
            {
                return;
            }

            if (_dirty && _currentTable != null && _currentTable != table)
            {
                bool save = DialogService.Confirm(
                    this,
                    "当前表有未保存的修改，是否保存？",
                    "周次情况记录表",
                    "保存",
                    "放弃修改");
                if (save)
                {
                    SaveCurrent(false);
                }
                else
                {
                    _dirty = false;
                }
            }

            BuildGrid(table);
        }

        private void SelectWeek(int week)
        {
            foreach (WeeklyRecordTable table in _tables)
            {
                if (table.WeekNumber == week)
                {
                    WeekCombo.SelectedItem = table;
                    return;
                }
            }
        }

        // ---------- 生成周次表 ----------

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            string existing = string.IsNullOrEmpty(_data.SemesterStart) ? "" : _data.SemesterStart;
            var dateDialog = new InputDialog(
                "设置开学日期",
                "开学日期所在周为第 1 周。\n格式：yyyy-MM-dd，例如 2026-02-16。",
                existing) { Owner = this };
            if (dateDialog.ShowDialog() != true)
            {
                return;
            }

            string dateInput = (dateDialog.InputText ?? "").Trim();
            DateTime startDate;
            if (!DateTime.TryParseExact(dateInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            {
                DialogService.Show(this, "开学日期格式不正确，请使用 yyyy-MM-dd。", "周次情况记录表", NoticeKind.Warning);
                return;
            }

            _data.SemesterStart = startDate.ToString("yyyy-MM-dd");
            SaveAppData();

            int currentWeek = Math.Max(1, ((DateTime.Today - startDate).Days / 7) + 1);
            var weekDialog = new InputDialog(
                "选择周次",
                "请输入要生成的周次（第 1 周为开学日期所在周）。\n本机日期：" +
                DateTime.Today.ToString("yyyy-MM-dd") + "，默认第 " + currentWeek + " 周。",
                currentWeek.ToString()) { Owner = this };
            if (weekDialog.ShowDialog() != true)
            {
                return;
            }

            int week;
            if (!int.TryParse((weekDialog.InputText ?? "").Trim(), out week) || week < 1)
            {
                DialogService.Show(this, "周次必须是大于 0 的整数。", "周次情况记录表", NoticeKind.Warning);
                return;
            }

            if (_data.Students.Count == 0)
            {
                DialogService.Show(this, "名单为空，请先回到主窗口点击「修改名单」添加学生。", "周次情况记录表", NoticeKind.Warning);
                return;
            }

            if (WeeklyRecordService.Exists(week))
            {
                DialogService.Show(this, "第 " + week + " 周的表已经存在。\n同一周次只能有一张表，请通过下拉框选择后打开。", "周次情况记录表", NoticeKind.Warning);
                return;
            }

            try
            {
                WeeklyRecordTable table = WeeklyRecordService.CreateFromStudents(_data, week, _data.SemesterStart);
                WeeklyRecordService.Save(table);
            }
            catch (Exception ex)
            {
                DialogService.Show(this, Prompt.WeeklySaveFailure(ex), "周次情况记录表", NoticeKind.Error);
                return;
            }

            RefreshTableList();
            SelectWeek(week);
        }

        // ---------- 保存 ----------

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrent(true);
        }

        private void SaveCurrent(bool showResult)
        {
            if (_currentTable == null)
            {
                return;
            }

            try
            {
                WeeklyRecordService.Save(_currentTable);
                _dirty = false;
                UpdateStatus();
                if (showResult)
                {
                    DialogService.Show(this, "第" + _currentTable.WeekNumber + "周的表已保存。", "周次情况记录表", NoticeKind.Success);
                }
            }
            catch (Exception ex)
            {
                DialogService.Show(this, Prompt.WeeklySaveFailure(ex), "周次情况记录表", NoticeKind.Error);
            }
        }

        private void SaveAppData()
        {
            try
            {
                DataService.Save(_data);
            }
            catch (Exception ex)
            {
                DialogService.Show(this, Prompt.SaveFailure(ex), "周次情况记录表", NoticeKind.Error);
            }
        }

        private void UpdateStatus()
        {
            if (_currentTable == null)
            {
                StatusText.Text = "";
                return;
            }

            StatusText.Text = "第" + _currentTable.WeekNumber + "周（" + _currentTable.StartDate + " ~ " + _currentTable.EndDate + "）"
                + " · 生成于 " + _currentTable.GeneratedAt
                + (_dirty ? " · 有未保存的修改" : " · 已保存");
        }

        // ---------- 表格构建 ----------

        private void BuildGrid(WeeklyRecordTable table)
        {
            _currentTable = table;
            _dirty = false;

            int colCount = WeeklyColumns.All.Length + 1;
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            for (int d = 0; d < WeeklyColumns.All.Length; d++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // 两行表头
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 表头第一行：姓名（跨两行）+ 大栏目（背默 / 课堂 / 作业 / 总结）
            grid.Children.Add(MakeHeaderCell("姓名", 0, 0, 2, 1, "#2C3E50"));
            int col = 1;
            int i = 0;
            while (i < WeeklyColumns.All.Length)
            {
                string groupHeader = WeeklyColumns.All[i].GroupHeader;
                int span = 1;
                int j = i + 1;
                while (j < WeeklyColumns.All.Length && WeeklyColumns.All[j].GroupHeader == groupHeader)
                {
                    span++;
                    j++;
                }
                grid.Children.Add(MakeHeaderCell(groupHeader, 0, col, 1, span, "#34495E"));
                col += span;
                i = j;
            }

            // 表头第二行：小列名（背默①、课堂①……总结）
            for (int k = 0; k < WeeklyColumns.All.Length; k++)
            {
                grid.Children.Add(MakeHeaderCell(WeeklyColumns.All[k].Header, 1, k + 1, 1, 1, "#566573"));
            }

            // 数据行：小组行 + 组长 / 组员行
            int gridRow = 2;
            for (int r = 0; r < table.Rows.Count; r++)
            {
                WeeklyRecordRow row = table.Rows[r];
                if (r == 0 || row.Group != table.Rows[r - 1].Group)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.Children.Add(MakeGroupRow(row.Group, gridRow, colCount));
                    gridRow++;
                }
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MakeStudentRow(row, gridRow, colCount, grid);
                gridRow++;
            }

            TableHost.Content = grid;
            UpdateStatus();
        }

        private static Border MakeHeaderCell(string text, int row, int col, int rowSpan, int colSpan, string background)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4, 6, 4, 6)
            };
            var textBlock = new TextBlock
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            Converters.Scale.SetFontSize(textBlock, "20,18");
            border.Child = textBlock;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowSpan > 1)
            {
                Grid.SetRowSpan(border, rowSpan);
            }
            if (colSpan > 1)
            {
                Grid.SetColumnSpan(border, colSpan);
            }
            return border;
        }

        private static Border MakeGroupRow(string group, int gridRow, int colCount)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xD6, 0xEA, 0xF8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5)
            };
            var textBlock = new TextBlock
            {
                Text = group,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x52, 0x76))
            };
            Converters.Scale.SetFontSize(textBlock, "22,18");
            border.Child = textBlock;

            Grid.SetRow(border, gridRow);
            Grid.SetColumn(border, 0);
            Grid.SetColumnSpan(border, colCount);
            return border;
        }

        private void MakeStudentRow(WeeklyRecordRow row, int gridRow, int colCount, Grid grid)
        {
            // 姓名列：组长 / 组员 + 姓名
            string roleText = row.IsLeader ? "组长：" : "组员：";
            string background = row.IsLeader ? "#FCF3CF" : "#F8F9F9";
            var nameBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(6, 3, 6, 3)
            };
            var nameText = new TextBlock
            {
                Text = roleText + row.StudentName,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = row.IsLeader ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50))
            };
            Converters.Scale.SetFontSize(nameText, "19,17");
            Converters.Scale.SetHeight(nameBorder, "42,34");
            nameBorder.Child = nameText;
            Grid.SetRow(nameBorder, gridRow);
            Grid.SetColumn(nameBorder, 0);
            grid.Children.Add(nameBorder);

            // 数据列：每个交叉单元格一个可编辑文本框
            for (int c = 0; c < WeeklyColumns.All.Length; c++)
            {
                TextBox box = MakeCellTextBox(row, WeeklyColumns.All[c].Key);
                Grid.SetRow(box, gridRow);
                Grid.SetColumn(box, c + 1);
                grid.Children.Add(box);
            }
        }

        private TextBox MakeCellTextBox(WeeklyRecordRow row, string key)
        {
            string value;
            if (!row.Cells.TryGetValue(key, out value))
            {
                value = "";
                row.Cells[key] = "";
            }

            var box = new TextBox
            {
                Text = value,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)),
                BorderThickness = new Thickness(0.5),
                Background = Brushes.White,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50)),
                Padding = new Thickness(4, 2, 4, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            Converters.Scale.SetFontSize(box, "19,17");
            Converters.Scale.SetHeight(box, "40,32");
            box.Tag = new CellTag { Row = row, Key = key };
            box.TextChanged += CellTextChanged;
            return box;
        }

        private void CellTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            var box = sender as TextBox;
            var tag = box == null ? null : box.Tag as CellTag;
            if (tag == null)
            {
                return;
            }

            string value = box.Text ?? "";
            string old;
            if (tag.Row.Cells.TryGetValue(tag.Key, out old) && old == value)
            {
                return;
            }

            tag.Row.Cells[tag.Key] = value;
            _dirty = true;
            UpdateStatus();
        }

        // ---------- 关闭时兜底 ----------

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_dirty && _currentTable != null)
            {
                bool save = DialogService.Confirm(
                    this,
                    "有未保存的修改，是否保存？",
                    "周次情况记录表",
                    "保存",
                    "不保存");
                if (save)
                {
                    SaveCurrent(false);
                }
            }
        }

        /// <summary>文本框 Tag：定位到对应行与列。</summary>
        private class CellTag
        {
            public WeeklyRecordRow Row;
            public string Key;
        }
    }
}
