using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Resources;

namespace unNetz
{
    class Program
    {
        private static readonly string Name = "Name";
        private static readonly string Culture = "Culture";
        private static readonly string NetzSuffix = "z.dll";
        private static HybridDictionary cache = null;
        private static ResourceManager rm = null;
        private static ArrayList xrRm = null;
        private static bool inResourceResolveFlag = false;
        private static Assembly _assembly;
        private const string guid = "A6C24BF5-3690-4982-887E-11E1B159B249";

        [STAThread]
        static void Main(string[] args)
        {
            var file = args.Length == 0 ? GetFile() : args[0];

            if (string.IsNullOrWhiteSpace(file))
                return;
            if (!File.Exists(file))
                Console.WriteLine($"File \"{file}\" not exists.");

            var outPutDirectoty = Path.Combine(Path.GetDirectoryName(file), @"Cracked");

            var assembly = Assembly.LoadFrom(file);

            rm = new ResourceManager("app", assembly);
            var mainAssembly = rm.GetObject(guid);

            var filedInfo = rm.GetType().GetField("_resourceSets", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            var resourceSets = filedInfo.GetValue(rm) as Dictionary<string, ResourceSet>;
            var resourceSet = resourceSets["zh-CN"];

            foreach (DictionaryEntry item in resourceSet)
            {
                var key = item.Key as string;
                var value = item.Value as byte[];
                if (key == "zip.dll")
                    continue;

                var ms = UnZip(value);
                ms.Seek(0L, SeekOrigin.Begin);
                if (!Directory.Exists(outPutDirectoty))
                    Directory.CreateDirectory(outPutDirectoty);

                var fileName = (key == guid) ? Path.GetFileName(file) : GetName(key);

                FileStream dumpFile = new FileStream(Path.Combine(outPutDirectoty, fileName), FileMode.Create, FileAccess.ReadWrite);
                ms.WriteTo(dumpFile);
            }

            Console.WriteLine("Finished..");
            Console.ReadKey();
        }

        private static string GetFile()
        {
            //using (var dlg = new OpenFileDialog())
            //{
            //    if (dlg.ShowDialog() == DialogResult.OK)
            //        return dlg.FileName;
            //}

            Console.WriteLine("File name to UnNetz：");
            return Console.ReadLine().Trim();
        }

        private static string GetName(string key)
        {
            var i = key.IndexOf("!");
            if (i > 0)
                return key.Substring(0, i) + ".dll";
            return key;
        }

        public static int StartApp(string[] args)
        {
            byte[] resource = GetResource("A6C24BF5-3690-4982-887E-11E1B159B249");
            if (resource == null)
            {
                throw new Exception("application data cannot be found");
            }
            Assembly assembly = GetAssembly(resource);

            return 0;
        }

        private static Assembly GetAssembly(byte[] data)
        {
            MemoryStream memoryStream = null;
            Assembly result = null;
            try
            {
                memoryStream = UnZip(data);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                result = Assembly.Load(memoryStream.ToArray());
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Close();
                }
                memoryStream = null;
            }
            return result;
        }

        private static Assembly LoadZipDll()
        {
            Assembly result = null;
            MemoryStream memoryStream = null;
            try
            {
                byte[] resource = GetResource("zip.dll");
                if (resource == null)
                {
                    return null;
                }
                memoryStream = new MemoryStream(resource);
                result = Assembly.Load(memoryStream.ToArray());
            }
            catch
            {
                result = null;
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Close();
                }
                memoryStream = null;
            }
            return result;
        }
        private static byte[] GetResource(string id)
        {
            byte[] array = null;
            if (rm == null)
            {
                rm = new ResourceManager("app", _assembly);
            }
            try
            {
                inResourceResolveFlag = true;
                string name = MangleDllName(id);
                if (array == null && xrRm != null)
                {
                    for (int i = 0; i < xrRm.Count; i++)
                    {
                        try
                        {
                            ResourceManager resourceManager = (ResourceManager)xrRm[i];
                            if (resourceManager != null)
                            {
                                array = (byte[])resourceManager.GetObject(name);
                            }
                        }
                        catch
                        {
                        }
                        if (array != null)
                        {
                            break;
                        }
                    }
                }
                if (array == null)
                {
                    array = (byte[])rm.GetObject(name);
                }
            }
            finally
            {
                inResourceResolveFlag = false;
            }
            return array;
        }

        private static string MangleDllName(string dll)
        {
            string text = dll.Replace(" ", "!1");
            text = text.Replace(",", "!2");
            text = text.Replace(".Resources", "!3");
            text = text.Replace(".resources", "!3");
            return text.Replace("Culture", "!4");
        }

        private static MemoryStream UnZip(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            MemoryStream memoryStream = null;
            MemoryStream memoryStream2 = null;
            InflaterInputStream inflaterInputStream = null;
            try
            {
                memoryStream = new MemoryStream(data);
                memoryStream2 = new MemoryStream();
                inflaterInputStream = new InflaterInputStream(memoryStream);
                byte[] array = new byte[data.Length];
                while (true)
                {
                    int num = inflaterInputStream.Read(array, 0, array.Length);
                    if (num <= 0)
                    {
                        break;
                    }
                    memoryStream2.Write(array, 0, num);
                }
                memoryStream2.Flush();
                memoryStream2.Seek(0L, SeekOrigin.Begin);
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Close();
                }
                if (inflaterInputStream != null)
                {
                    inflaterInputStream.Close();
                }
                memoryStream = null;
                inflaterInputStream = null;
            }
            return memoryStream2;
        }
        private static void Log(string s)
        {
            Console.WriteLine(s);
        }
        private static Assembly GetAssemblyByName(string name)
        {
            if (name == null)
            {
                return null;
            }
            if (cache == null)
            {
                cache = new HybridDictionary();
            }
            name = name.Trim();
            string key = name.ToLower();
            if (cache[key] != null)
            {
                return (Assembly)cache[key];
            }
            StringDictionary stringDictionary = ParseAssName(name);
            string text = stringDictionary[Name];
            if (text == null)
            {
                return null;
            }
            if (text.ToLower().Equals("zip"))
            {
                Assembly assembly = LoadZipDll();
                cache[key] = assembly;
                return assembly;
            }
            byte[] resource = GetResource(name);
            if (resource == null)
            {
                resource = GetResource(name.ToLower());
            }
            if (resource == null)
            {
                resource = GetResource(text);
            }
            if (resource == null)
            {
                resource = GetResource(text.ToLower());
            }
            if (resource == null)
            {
                resource = GetResource(Path.GetFileNameWithoutExtension(text).ToLower());
            }
            if (resource == null)
            {
                return null;
            }
            Assembly assembly2 = GetAssembly(resource);
            cache[key] = assembly2;
            return assembly2;
        }
        private static StringDictionary ParseAssName(string fullAssName)
        {
            StringDictionary stringDictionary = new StringDictionary();
            string[] array = fullAssName.Split(new char[]
            {
                ','
            });
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = array[i].Trim(new char[]
                {
                    ' ',
                    ','
                }).Split(new char[]
                {
                    '='
                });
                if (array2.Length < 2)
                {
                    stringDictionary.Add(Name, array2[0]);
                }
                else
                {
                    stringDictionary.Add(array2[0].Trim(new char[]
                    {
                        ' ',
                        '='
                    }), array2[1].Trim(new char[]
                    {
                        ' ',
                        '='
                    }));
                }
            }
            return stringDictionary;
        }
    }
}
