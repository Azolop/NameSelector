using System.Collections.Generic;

namespace NameSelector.Models
{
    /// <summary>
    /// namelist.json 的根数据。
    /// </summary>
    public class AppData
    {
        public AppData()
        {
            Students = new List<Student>();
            NextOrder = 1;
            SemesterStart = "";
        }

        public List<Student> Students { get; set; }

        /// <summary>下一个点名次序，初始 1。</summary>
        public int NextOrder { get; set; }

        /// <summary>开学日期，格式 yyyy-MM-dd；开学日期所在周为第 1 周。</summary>
        public string SemesterStart { get; set; }
    }
}
