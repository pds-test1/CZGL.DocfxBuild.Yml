﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Text;

namespace CZGL.DocfxBuild.Yml
{
    class Program
    {
        public static bool isSpilt = false;
        public static string Spilt = " ";
        public static bool isSpiltAll = false;
        private static void PrintfInit()
        {
            Console.WriteLine("指定每个目录要生成的默认首页规则：\n1.默认生成方式(输入序号 1) \n" +
                "2. 每个目录以 index.md 为首页(输入序号 2)\n" +
                "3. 其他规则请直接输入文件名(如 home.md)");
        }
        static void Main(string[] args)
        {
            BuildYml build = new BuildYml();
            Console.WriteLine("###############################################");
            Console.WriteLine("#    SetRe.json文件，可配置程序设置           #");
            Console.WriteLine("#    Dir_Exclude：排除生成的文件夹            #");
            Console.WriteLine("#    toc.yml：排除生成的文件                  #");
            Console.WriteLine("###############################################");
            Console.WriteLine("\n输入要处理的目录：");
            string a = Console.ReadLine();

            PrintfInit();

            string b = "";
            while (true)
            {
                b = Console.ReadLine();
                if ((new string[] { "1", "2", "3" }).Contains(b))
                    break;
                Console.WriteLine("输入无效，请重新输入");
                PrintfInit();
            }

            while (true)
            {
                Console.WriteLine("生成名称是否需要去除目录或文件前的序号：\n输入(y/n)");
                string vc = Console.ReadLine();
                if (vc.ToLower() == "y")
                {
                    isSpilt = true;
                    break;
                }
                else if (vc.ToLower() == "n")
                {
                    break;
                }
                Console.WriteLine("输入无效，请重新输入");
            }
            if (isSpilt == true)
                while (true)
                {

                    Console.WriteLine("是否需要修改文件(去除目录或文件的序号)：\n输入(y/n)");
                    string vc = Console.ReadLine();
                    if (vc.ToLower() == "y")
                    {
                        isSpiltAll = true;
                        break;
                    }
                    else if (vc.ToLower() == "n")
                    {
                        break;
                    }
                    Console.WriteLine("输入无效，请重新输入");
                }
            if (isSpilt == true)
                Console.WriteLine("输入序号分隔符：");
            Spilt = Console.ReadLine();



            switch (b)
            {
                case "1": build.homewhat = 1; break;
                case "2": build.homewhat = 2; build.homepagename = "index.md"; break;
                case "3": build.homewhat = 3; build.homepagename = b; break;
                default: return;
            }
            Console.WriteLine("处理结果：" + build.Builder(a));
            Console.WriteLine("输入 d 可以删除本次生成的文件");
            if (Console.ReadLine().ToLower() == "d")
            {
                build.Dedocyml();
            }

            Console.ReadKey();
        }
    }
    public class BuildYml
    {
        string docfx_project_Path;
        public int homewhat;
        public string homepagename;
        string[] redir = { ".git", "image", "images" };
        string[] refile = { "toc.yml", "intro.md" };

        public BuildYml()
        {
            Init();
        }
        async void Init()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "SetRe.json")))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "SetRe.json"));
                    SetRe model = JsonConvert.DeserializeObject<SetRe>(json);
                    redir = model.Dir_Exclude;
                    refile = model.File_Exclude;
                }
                catch
                {
                    Console.WriteLine("配置文件不存在或出现配置出错");
                    Console.ReadKey();
                }
            }
        }
        public class SetRe
        {
            public string[] Dir_Exclude { get; set; }
            public string[] File_Exclude { get; set; }
        }
        public bool Builder(string articles)
        {
            docfx_project_Path = articles;
            if (!Directory.Exists(docfx_project_Path))
            {
                Console.WriteLine("目录不存在");
                return false;
            }

            try
            {
                Traverse(docfx_project_Path);
                if (Program.isSpiltAll == true)
                {
                    Console.WriteLine("迁移文件中...");
                    MoveF(articles);
                    Console.WriteLine("迁移成功...");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }
        // 遍历
        public void Traverse(string dirpath)
        {
            string[] dirs = Directory.GetDirectories(dirpath);
            foreach (var item in dirs)
            {
                if (redir.Contains((new DirectoryInfo(item).Name).ToLower()))
                    dirs = dirs.Where(x => x != item).ToArray();
            }
            if (dirs.Length != 0)
            {
                foreach (var item in dirs)
                {
                    Traverse(item);
                }
            }
            BuilderFileYml(dirpath);
        }
        public void Dedocyml()
        {
            DeTocYml(docfx_project_Path);
            Console.WriteLine("删除完成");
        }
        private void DeTocYml(string dirpath)
        {
            string[] dirs = Directory.GetDirectories(dirpath);
            foreach (var item in dirs)
            {
                if (redir.Contains((new DirectoryInfo(item).Name).ToLower()))
                    dirs = dirs.Where(x => x != item).ToArray();
            }

            if (dirs.Length != 0)
            {
                foreach (var item in dirs)
                {
                    DeTocYml(item);
                }
            }
            DeleteYml(dirpath);
        }
        void DeleteYml(string dirpath)
        {
            try
            {
                File.Delete(Path.Combine(dirpath, "toc.yml"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        // 对一个目录生成操作
        public async void BuilderFileYml(string dirpath)
        {
            Console.WriteLine("生成：" + dirpath);
            await Task.Run(() =>
            {
                string[] dirs = Directory.GetDirectories(dirpath);
                string[] files = Directory.GetFiles(dirpath);
                if (dirs.Length == 0 && files.Length == 0)
                {
                    FileStream x = new FileStream(Path.Combine(dirpath, "Empty.md"), FileMode.OpenOrCreate);
                    var markdown = Encoding.Default.GetBytes("This an empty directory");
                    x.WriteAsync(markdown, 0, markdown.Length);
                    files = Directory.GetFiles(dirpath);
                }

                foreach (var item in dirs)
                {
                    if (redir.Contains((new DirectoryInfo(item).Name).ToLower()))
                        dirs = dirs.Where(x => x != item).ToArray();
                }
                foreach (var item in files)
                {
                    if (refile.Contains((new DirectoryInfo(item).Name).ToLower()))
                        files = files.Where(x => x != item).ToArray();
                }

                using (FileStream fx = new FileStream(Path.Combine(dirpath, "toc.yml"), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    StreamWriter writer = new StreamWriter(fx);
                    writer.WriteLine("### " + dirpath);
                    foreach (var item in dirs)
                    {
                        string dirName = new DirectoryInfo(item).Name;
                        if (Program.isSpilt)
                        {
                            string[] dirsp = dirName.Split(Program.Spilt, 2);
                            if (dirsp.Length > 1)
                                dirName = dirsp[1];
                            else dirName = dirsp[0];
                        }
                        writer.WriteLine("- name: " + dirName);
                        if (Program.isSpiltAll == true)
                        {
                            writer.WriteLine("  href: " + dirName + "/" + "toc.yml");
                        }
                        else writer.WriteLine("  href: " + new DirectoryInfo(item).Name + "/" + "toc.yml");
                    }
                    foreach (var item in files)
                    {
                        string fileName = Path.GetFileName(item).Replace(".md", "").Replace(".MD", "");
                        if (Program.isSpilt)
                        {
                            string[] filesp = fileName.Split(Program.Spilt, 2);
                            if (filesp.Length > 1)
                                fileName = filesp[1];
                            else fileName = filesp[0];
                        }
                        writer.WriteLine("- name: " + fileName);
                        if (Program.isSpiltAll == true)
                        {
                            writer.WriteLine("  href: " + fileName + ".md");
                        }

                        else writer.WriteLine("  href: " + Path.GetFileName(item) + ".md");
                    }
                    if (files.Length != 0)
                    {
                        if (homewhat == 1)
                        {
                            string homepage = Path.GetFileName(files[files.Length - 1]);
                            if (Program.isSpilt)
                            {
                                string[] filesp = homepage.Split(Program.Spilt, 2);
                                if (filesp.Length > 1)
                                    homepage = filesp[1];
                                else homepage = filesp[0];
                            }
                            writer.WriteLine("  homepage: " + homepage);
                        }
                        else if (homewhat == 2)
                        {
                            writer.WriteLine("  homepage: index.md");
                        }
                        else
                            writer.WriteLine("  homepage: " + homepagename);
                    }
                    writer.Close();
                    fx.Close();
                }
            });
        }

        // 对所有目录和文件去除序号
        public void MoveF(string dirpath)
        {
            string[] dirs = Directory.GetDirectories(dirpath);
            foreach (var item in dirs)
            {
                if (redir.Contains((new DirectoryInfo(item).Name).ToLower()))
                    dirs = dirs.Where(x => x != item).ToArray();
            }

            if (dirs.Length != 0)
            {
                foreach (var item in dirs)
                {
                    MoveF(item);
                }
            }

            string[] files = Directory.GetFiles(dirpath);
            foreach (var itemi in files)
            {
                if (refile.Contains((new DirectoryInfo(itemi).Name).ToLower()))
                    files = files.Where(x => x != itemi).ToArray();
            }
            foreach (var itemi in files)
            {
                string fileName = Path.GetFileName(itemi);
                string[] filesp = fileName.Split(Program.Spilt, 2);
                if (filesp.Length > 1)
                    fileName = filesp[1];
                else fileName = filesp[0];
                try
                {
                    File.Move(itemi, Path.Combine(Path.GetDirectoryName(itemi), fileName));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(itemi + "|" + Path.Combine(Directory.GetParent(itemi).FullName, fileName));
                    Console.WriteLine(ex.ToString());
                }
            }
            string[] dirss = Directory.GetDirectories(dirpath);
            foreach (var item in dirss)
            {
                if (redir.Contains((new DirectoryInfo(item).Name).ToLower()))
                    dirss = dirss.Where(x => x != item).ToArray();
            }
            foreach (var item in dirss)
            {
                string dirName = new DirectoryInfo(item).Name;
                string[] dirsp = dirName.Split(Program.Spilt, 2);
                if (dirsp.Length > 1)
                    dirName = dirsp[1];
                else dirName = dirsp[0];
                try
                {
                    Directory.Move(item, Path.Combine(Directory.GetParent(item).FullName, dirName));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(item + "|" + Path.Combine(Directory.GetParent(item).FullName, dirName));
                }
            }
        }
    }
}
