using System.Collections.Generic;
using System.IO;

namespace DarkMatter.Console
{
    public static class WindowsFileHelper
    {
        #region Delegates

        public delegate void Visit(FileSystemInfo file);

        #endregion

        #region Methods

        public static void DepthFirstTraverse(this DirectoryInfo dir, Visit visit)
        {
            visit(dir);

            var dirs = dir.GetDirectories();
            foreach (var d in dirs)
            {
                d.DepthFirstTraverse(visit);
            }

            var files = dir.GetFiles();
            foreach (var f in files)
            {
                visit(f);
            }
        }

        public static void BreadthFirstTraverse(this DirectoryInfo dir, Visit visit)
        {
            var q = new Queue<DirectoryInfo>();

            q.Enqueue(dir);

            while (q.Count > 0)
            {
                var d = q.Dequeue();
                visit(d);

                var files = d.GetFiles();
                foreach (var f in files)
                {
                    visit(f);
                }

                var dirs = d.GetDirectories();
                foreach (var dd in dirs)
                {
                    q.Enqueue(dd);
                }
            }
        }

        #endregion
    }
}
