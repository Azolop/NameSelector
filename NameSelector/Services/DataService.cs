using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using NameSelector.Models;

namespace NameSelector.Services
{
    /// <summary>
    /// namelist.json 的读写。文件始终位于 exe 所在目录。
    /// </summary>
    public static class DataService
    {
        public static string DataFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "namelist.json"); }
        }

        public static AppData Load()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    string json = File.ReadAllText(DataFilePath, Encoding.UTF8);
                    AppData data = new JavaScriptSerializer().Deserialize<AppData>(json);
                    if (data != null)
                    {
                        if (data.Students == null)
                        {
                            data.Students = new List<Student>();
                        }
                        return data;
                    }
                }

                // 未发现 namelist.json 或文件内容为空：初始化默认名单并保存。
                AppData defaults = CreateDefaultData();
                Save(defaults);
                return defaults;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "读取 namelist.json 失败，程序将使用默认名单重新开始。\n\n" + ex.Message,
                    "点名分组工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return CreateDefaultData();
        }

        /// <summary>
        /// 默认名单：经典的“中国最忙五人组”。
        /// </summary>
        private static AppData CreateDefaultData()
        {
            var data = new AppData();
            string[] defaultNames = { "张吉惟", "林国瑞", "林玟书", "林雅南", "江奕云" };
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
            try
            {
                string json = new JavaScriptSerializer().Serialize(data);
                File.WriteAllText(DataFilePath, json, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "保存 namelist.json 失败。\n\n" + ex.Message,
                    "点名分组工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
