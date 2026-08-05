using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NameSelector.Models;
using NameSelector.Services;

namespace NameSelector
{
    /// <summary>
    /// 修改名单窗口：加载时显示当前名单，保存时全量重写并重置点名状态。
    /// </summary>
    public partial class EditListDialog : Window
    {
        private readonly AppData _data;

        /// <summary>保存成功后为 true，主窗口据此刷新。</summary>
        public bool Saved { get; private set; }

        public EditListDialog(AppData data)
        {
            InitializeComponent();
            _data = data;
            NamesBox.Text = string.Join("\r\n", data.Students.Select(s => s.Name).ToArray());
            // 比例式自适应：每次布局更新按窗口尺寸重新套用缩放
            LayoutUpdated += (s, e) => Converters.Scale.Apply(this, 560, 520);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var names = new List<string>();
            string[] lines = NamesBox.Text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    names.Add(trimmed);
                }
            }

            // 全量替换：重新编号，重置所有点名状态。
            _data.Students = new List<Student>();
            for (int i = 0; i < names.Count; i++)
            {
                _data.Students.Add(new Student
                {
                    Id = i + 1,
                    Name = names[i],
                    IsCalled = false,
                    Order = 0
                });
            }
            _data.NextOrder = 1;

            if (DataService.Save(_data))
            {
                Saved = true;
                System.Media.SystemSounds.Asterisk.Play();
                MessageBox.Show(this, "名单已更新", "修改名单", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            // 保存失败时 DataService 已弹错误框，此处保持窗口打开以便重试，且不刷新主窗口。
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
