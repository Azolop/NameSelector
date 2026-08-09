using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NameSelector.Models;
using NameSelector.Services;
using NameSelector.Dialogs;

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
            NamesBox.Text = string.Join("\r\n", data.Students.Select(FormatLine).ToArray());
            // 比例式自适应：加载后立即应用一次；布局变化时防抖重新应用
            Loaded += (s, e) => Converters.Scale.ApplyNow(this, 560, 520);
            LayoutUpdated += (s, e) => Converters.Scale.RequestApply(this, 560, 520);
        }

        /// <summary>编辑框显示格式：有组名的显示为「组名:姓名」，否则只显示姓名。</summary>
        private static string FormatLine(Student student)
        {
            if (!string.IsNullOrEmpty(student.Group) && student.Group != "未分组")
            {
                return student.Group + ":" + student.Name;
            }
            return student.Name;
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
            var groupSeen = new HashSet<string>();
            int id = 0;
            for (int i = 0; i < names.Count; i++)
            {
                string group = "";
                string name = names[i];
                int colon = FindColon(name);
                if (colon > 0 && colon < name.Length - 1)
                {
                    group = name.Substring(0, colon).Trim();
                    name = name.Substring(colon + 1).Trim();
                }
                if (group.Length == 0)
                {
                    group = "未分组";
                }
                if (name.Length == 0)
                {
                    continue;
                }

                bool isLeader = !groupSeen.Contains(group);
                groupSeen.Add(group);
                _data.Students.Add(new Student
                {
                    Id = ++id,
                    Name = name,
                    Group = group,
                    IsLeader = isLeader,
                    IsCalled = false,
                    Order = 0
                });
            }
            _data.NextOrder = 1;
            _data.SemesterStart = _data.SemesterStart ?? "";

            try
            {
                DataService.Save(_data);
            }
            catch (Exception ex)
            {
                // 保存失败时保持窗口打开以便重试，且不刷新主窗口。
                DialogService.Show(this, Prompt.SaveFailure(ex), "修改名单", NoticeKind.Error);
                return;
            }

            Saved = true;
            System.Media.SystemSounds.Asterisk.Play();
            DialogService.Show(this, "名单已更新", "修改名单", NoticeKind.Success);
            Close();
        }

        /// <summary>同时支持半角冒号和全角冒号，取最先出现的位置。</summary>
        private static int FindColon(string text)
        {
            int half = text.IndexOf(':');
            int full = text.IndexOf('：');
            if (half < 0)
            {
                return full;
            }
            if (full < 0)
            {
                return half;
            }
            return Math.Min(half, full);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
