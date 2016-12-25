using System.IO;
using System.Text;
using System.Threading.Tasks;
using DarkMatter.Helpers;
using DarkMatter.Simplistic;
using File = DarkMatter.Simplistic.File;

namespace DarkMatter.Console
{
    class Sender
    {
        #region Constructors

        public Sender(Manager manager, DirectoryInfo local, string dmdir)
        {
            Local = local;
            DmDir = dmdir;
            Manager = manager;
            DmBase = manager.GetFile(dmdir);
        }

        #endregion

        #region Properties

        public DirectoryInfo Local { get; private set; }
        public string DmDir { get; private set; }
        public Manager Manager { get; private set; }
        public File DmBase { get; private set; }

        #endregion

        #region Methods

        public async void VisitFile(FileSystemInfo file)
        {
            var dir = file as DirectoryInfo;
            if (dir != null)
            {
                var rel = GetRelativeDir(Local, dir);
                Manager.CreateDirectoryIfNeeded(DmBase, rel);
                return;
            }

            var f = file as FileInfo;
            if (f != null)
            {
                var rel = GetRelativeDir(Local, f);
                // We always assume the directory has been created
                var d = Manager.GetFile(rel);

                // TODO file extension?
                var dmf = Manager.AddFile(d, f.Name);
                await CopyFile(f);
                Manager.FinalizeFile(dmf);
            }
        }

        private async Task CopyFile(FileInfo fileInfo)
        {
            await CopyFile(Manager, fileInfo);
        }

        public static async Task CopyFile(Manager manager, FileInfo fileInfo)
        {
            const int bufSize = 4 * 1024 * 1024;
            var buf = new byte[bufSize];
            using (var fs = fileInfo.OpenRead())
            {
                while (true)
                {
                    var c = await fs.ReadAsync(buf, 0, bufSize);
                    if (c <= 0)
                    {
                        break;
                    }
                    await manager.WriteAsync(buf, 0, c);
                }
            }
        }

        public static string GetRelativeDir(DirectoryInfo basedir, DirectoryInfo dir)
        {
            var sb = new StringBuilder();
            var p = dir;
            for (; p != basedir; p = p.Parent)
            {
                sb.Insert(0, '/');
                sb.Insert(0, p.Name);
            }
            return sb.ToString();
        }

        /// <summary>
        ///  Returns the directory of the containing folder of the file relative to the base dir
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetRelativeDir(DirectoryInfo baseDir, FileInfo file)
        {
            var sb = new StringBuilder();
            var p = file.Directory;
            return GetRelativeDir(baseDir, p);
        }

        #endregion
    }
}
