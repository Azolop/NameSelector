using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace NameSelector.Models
{
    /// <summary>
    /// 一周的情况记录表。每一张表独立保存为一个 JSON 文件，
    /// 文件命名规则：日期跨度_周次，如 2026-02-23_2026-03-01_week2.json。
    /// </summary>
    public class WeeklyRecordTable
    {
        public WeeklyRecordTable()
        {
            Rows = new List<WeeklyRecordRow>();
        }

        /// <summary>周次，第 1 周为开学日期所在周。</summary>
        public int WeekNumber { get; set; }

        /// <summary>开学日期，格式 yyyy-MM-dd。</summary>
        public string SemesterStart { get; set; }

        /// <summary>本周起始日期，格式 yyyy-MM-dd。</summary>
        public string StartDate { get; set; }

        /// <summary>本周结束日期，格式 yyyy-MM-dd。</summary>
        public string EndDate { get; set; }

        /// <summary>生成时间，格式 yyyy-MM-dd HH:mm:ss。</summary>
        public string GeneratedAt { get; set; }

        public List<WeeklyRecordRow> Rows { get; set; }

        /// <summary>下拉框显示文本（只读，不写入 JSON）。</summary>
        [ScriptIgnore]
        public string DisplayName
        {
            get { return "第" + WeekNumber + "周（" + StartDate + " ~ " + EndDate + "）"; }
        }
    }

    /// <summary>
    /// 表中的一行：对应一名学生（组长或组员）。
    /// </summary>
    public class WeeklyRecordRow
    {
        public WeeklyRecordRow()
        {
            Cells = new Dictionary<string, string>();
        }

        /// <summary>小组号（数字），未分组为 0。</summary>
        public int GroupNumber { get; set; }

        /// <summary>是否组长。</summary>
        public bool IsLeader { get; set; }

        /// <summary>学生编号，与名单一致。</summary>
        public int StudentId { get; set; }

        /// <summary>学生姓名。</summary>
        public string StudentName { get; set; }

        /// <summary>
        /// 各交叉单元格内容，键为列 Key（如 Back1、Class1、Homework1、Summary）。
        /// </summary>
        public Dictionary<string, string> Cells { get; set; }
    }

    /// <summary>
    /// 周次表的列定义：背默 2 列、课堂 5 列、作业 3 列、总结 1 列。
    /// </summary>
    public static class WeeklyColumns
    {
        /// <summary>一列的定义。</summary>
        public class ColumnDef
        {
            public ColumnDef(string key, string header, string groupHeader)
            {
                Key = key;
                Header = header;
                GroupHeader = groupHeader;
            }

            /// <summary>单元格字典使用的键。</summary>
            public string Key { get; private set; }

            /// <summary>小列名，如「背默①」。</summary>
            public string Header { get; private set; }

            /// <summary>大栏目名，如「背默」。</summary>
            public string GroupHeader { get; private set; }
        }

        public static readonly ColumnDef[] All = new ColumnDef[]
        {
            new ColumnDef("Back1", "背默①", "背默"),
            new ColumnDef("Back2", "背默②", "背默"),
            new ColumnDef("Class1", "课堂①", "课堂"),
            new ColumnDef("Class2", "课堂②", "课堂"),
            new ColumnDef("Class3", "课堂③", "课堂"),
            new ColumnDef("Class4", "课堂④", "课堂"),
            new ColumnDef("Class5", "课堂⑤", "课堂"),
            new ColumnDef("Homework1", "作业①", "作业"),
            new ColumnDef("Homework2", "作业②", "作业"),
            new ColumnDef("Homework3", "作业③", "作业"),
            new ColumnDef("Summary", "总结", "总结")
        };
    }
}
