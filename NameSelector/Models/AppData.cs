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
        }

        public List<Student> Students { get; set; }

        /// <summary>下一个点名次序，初始 1。</summary>
        public int NextOrder { get; set; }
    }
}
