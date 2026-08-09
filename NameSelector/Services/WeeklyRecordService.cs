using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using NameSelector.Models;

namespace NameSelector.Services
{
    /// <summary>
    /// 周次情况记录表的读写。每张表独立保存为 weekly 目录下的一个 JSON 文件，
    /// 文件命名规则：日期跨度_周次，如 2026-02-23_2026-03-01_week2.json。
    /// </summary>
    public static class WeeklyRecordService
    {
        private const string FolderName = "weekly";

        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static string FolderPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderName); }
        }

        /// <summary>按日期跨度 + 周次规则生成文件名（不使用中文命名）。</summary>
        public static string FilePathFor(WeeklyRecordTable table)
        {
            return Path.Combine(FolderPath, table.StartDate + "_" + table.EndDate + "_week" + table.WeekNumber + ".json");
        }

        public static bool Exists(int week)
        {
            foreach (WeeklyRecordTable table in LoadAll())
            {
                if (table.WeekNumber == week)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 读取全部周次表，按周次从大到小排序。
        /// 单个文件损坏时跳过，不影响其他周次表。
        /// </summary>
        public static List<WeeklyRecordTable> LoadAll()
        {
            var tables = new List<WeeklyRecordTable>();
            if (!Directory.Exists(FolderPath))
            {
                return tables;
            }

            foreach (string file in Directory.GetFiles(FolderPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    WeeklyRecordTable table = Serializer.Deserialize<WeeklyRecordTable>(json);
                    if (table == null || table.Rows == null)
                    {
                        continue;
                    }

                    // 以文件名为准：从日期跨度_周次中解析周次，避免文件被移动后内容与名字不一致。
                    Match match = Regex.Match(Path.GetFileNameWithoutExtension(file), @"week(\d+)$");
                    if (match.Success)
                    {
                        table.WeekNumber = int.Parse(match.Groups[1].Value);
                    }
                    if (table.WeekNumber > 0)
                    {
                        tables.Add(table);
                    }
                }
                catch
                {
                    // 跳过损坏文件
                }
            }

            tables.Sort((a, b) => b.WeekNumber.CompareTo(a.WeekNumber));
            return tables;
        }

        public static void Save(WeeklyRecordTable table)
        {
            Directory.CreateDirectory(FolderPath);
            string json = Serializer.Serialize(table);
            File.WriteAllText(FilePathFor(table), json, new UTF8Encoding(false));
        }

        /// <summary>
        /// 按当前名单生成某周的空表（仅含行结构与空单元格，不落盘）。
        /// 组内组长行在前，组员按名单顺序。
        /// </summary>
        public static WeeklyRecordTable CreateFromStudents(AppData data, int week, string semesterStart)
        {
            DateTime start;
            if (!DateTime.TryParseExact(semesterStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            {
                start = DateTime.Today;
            }

            DateTime weekStart = start.AddDays((week - 1) * 7);
            DateTime weekEnd = weekStart.AddDays(6);

            var table = new WeeklyRecordTable
            {
                WeekNumber = week,
                SemesterStart = semesterStart,
                StartDate = weekStart.ToString("yyyy-MM-dd"),
                EndDate = weekEnd.ToString("yyyy-MM-dd"),
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 小组按名单首次出现顺序排列；组内组长在前，其余按名单顺序。
            var groups = new List<string>();
            var groupOrder = new Dictionary<string, int>();
            foreach (var student in data.Students)
            {
                string group = GroupOf(student);
                if (!groupOrder.ContainsKey(group))
                {
                    groupOrder[group] = groups.Count;
                    groups.Add(group);
                }
            }

            var sortedStudents = new List<Student>(data.Students);
            sortedStudents.Sort((a, b) =>
            {
                int cmp = groupOrder[GroupOf(a)].CompareTo(groupOrder[GroupOf(b)]);
                if (cmp != 0)
                {
                    return cmp;
                }
                if (a.IsLeader != b.IsLeader)
                {
                    return a.IsLeader ? -1 : 1;
                }
                return a.Id.CompareTo(b.Id);
            });

            foreach (var student in sortedStudents)
            {
                var row = new WeeklyRecordRow
                {
                    Group = GroupOf(student),
                    IsLeader = student.IsLeader,
                    StudentId = student.Id,
                    StudentName = student.Name
                };
                foreach (var column in WeeklyColumns.All)
                {
                    row.Cells[column.Key] = "";
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private static string GroupOf(Student student)
        {
            return string.IsNullOrEmpty(student.Group) ? "未分组" : student.Group;
        }
    }
}
