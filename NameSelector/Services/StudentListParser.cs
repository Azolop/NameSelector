using System;
using System.Collections.Generic;
using NameSelector.Models;

namespace NameSelector.Services
{
    /// <summary>
    /// 名单文本解析：每行「名称,组号,是否组长」，组号与是否组长可省略。
    /// 规则：每组最多一名组长（同一组内第一个标记为组长的人生效）；
    /// 单人群组自动成为组长；一组没有显式组长时，第一个成员自动成为组长。
    /// </summary>
    public static class StudentListParser
    {
        public static List<Student> Parse(string text)
        {
            var entries = new List<NameEntry>();
            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                string[] parts = trimmed.Replace('，', ',').Split(',');
                string name = parts.Length > 0 ? parts[0].Trim() : "";
                if (name.Length == 0)
                {
                    continue;
                }

                int group = 0;
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1].Trim(), out group);
                    if (group < 0)
                    {
                        group = 0;
                    }
                }

                bool? leader = parts.Length > 2 ? ParseLeaderFlag(parts[2]) : null;
                entries.Add(new NameEntry { Name = name, GroupNumber = group, Leader = leader });
            }

            var students = new List<Student>();
            var leaderGroups = new HashSet<int>();
            int id = 0;
            foreach (var entry in entries)
            {
                // 每组最多一个组长：同一组内只有第一个标记为组长的成员生效。
                bool isLeader = entry.Leader == true && !leaderGroups.Contains(entry.GroupNumber);
                if (isLeader)
                {
                    leaderGroups.Add(entry.GroupNumber);
                }
                students.Add(new Student
                {
                    Id = ++id,
                    Name = entry.Name,
                    GroupNumber = entry.GroupNumber,
                    IsLeader = isLeader,
                    IsCalled = false,
                    Order = 0
                });
            }

            // 每组没有显式组长时，第一个成员自动成为组长；单人群组同样自动为组长。
            foreach (var student in students)
            {
                if (!leaderGroups.Contains(student.GroupNumber))
                {
                    student.IsLeader = true;
                    leaderGroups.Add(student.GroupNumber);
                }
            }

            return students;
        }

        /// <summary>解析组长标记：true/1/是 为组长，false/0/否 为非组长，其他视为未标记。</summary>
        private static bool? ParseLeaderFlag(string text)
        {
            string value = text.Trim().ToLowerInvariant();
            if (value == "true" || value == "1" || value == "是")
            {
                return true;
            }
            if (value == "false" || value == "0" || value == "否")
            {
                return false;
            }
            return null;
        }

        /// <summary>名单编辑的中间数据。</summary>
        private class NameEntry
        {
            public string Name;
            public int GroupNumber;
            public bool? Leader;
        }
    }
}
