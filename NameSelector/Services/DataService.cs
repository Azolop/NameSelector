using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using NameSelector.Models;

namespace NameSelector.Services
{
    /// <summary>
    /// namelist.json 的读写。文件始终位于 exe 所在目录。
    /// 读写失败时抛出异常，由界面层负责提示用户。
    /// </summary>
    public static class DataService
    {
        /// <summary>数据文件名。</summary>
        public const string FileName = "namelist.json";

        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static string DataFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName); }
        }

        /// <summary>
        /// 读取名单数据；文件不存在或内容为空时创建默认名单并落盘。
        /// 文件损坏、编码异常或首次写入失败时抛出异常。
        /// </summary>
        public static AppData LoadOrCreate()
        {
            if (File.Exists(DataFilePath))
            {
                string json = File.ReadAllText(DataFilePath, Encoding.UTF8);
                AppData data = Serializer.Deserialize<AppData>(json);
                if (data != null)
                {
                    Normalize(data);
                    return data;
                }
            }

            // 未发现 namelist.json 或文件内容为空：初始化默认名单并保存。
            AppData defaults = CreateDefault();
            Save(defaults);
            return defaults;
        }

        /// <summary>
        /// 默认名单。
        /// </summary>
        public static AppData CreateDefault()
        {
            var data = new AppData();
            string[] defaultNames = { "Tom", "Jerry", "Spike" };
            for (int i = 0; i < defaultNames.Length; i++)
            {
                data.Students.Add(new Student
                {
                    Id = i + 1,
                    Name = defaultNames[i],
                    IsCalled = false,
                    Order = 0
                });
            }
            data.NextOrder = 1;
            return data;
        }

        public static void Save(AppData data)
        {
            string json = Serializer.Serialize(data);
            File.WriteAllText(DataFilePath, json, new UTF8Encoding(false));
        }

        /// <summary>
        /// 修正异常数据：清掉非法负次序；NextOrder 不得小于“已点最大次序+1”，避免次序重复。
        /// </summary>
        private static void Normalize(AppData data)
        {
            if (data.Students == null)
            {
                data.Students = new List<Student>();
                return;
            }

            // 过滤损坏数据中混入的空项，避免统计时 NullReferenceException。
            data.Students.RemoveAll(s => s == null);

            foreach (var student in data.Students)
            {
                if (student.Order < 0)
                {
                    student.Order = 0;
                }
            }

            int maxOrder = 0;
            foreach (var student in data.Students)
            {
                if (student.IsCalled && student.Order > maxOrder)
                {
                    maxOrder = student.Order;
                }
            }
            if (data.NextOrder < maxOrder + 1)
            {
                data.NextOrder = maxOrder + 1;
            }
        }
    }
}
