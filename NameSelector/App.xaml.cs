using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace NameSelector
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // 支持单文件发布：Newtonsoft.Json.dll 作为嵌入资源打进 exe，
            // 首次使用前从这里解析加载。
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string simpleName = new AssemblyName(args.Name).Name;
            if (string.Equals(simpleName, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream stream = typeof(App).Assembly.GetManifestResourceStream("Newtonsoft.Json.dll"))
                {
                    if (stream != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return Assembly.Load(ms.ToArray());
                        }
                    }
                }
            }
            return null;
        }
    }
}
