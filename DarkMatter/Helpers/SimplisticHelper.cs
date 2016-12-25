using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkMatter.Simplistic;

namespace DarkMatter.Helpers
{
    public static class SimplisticHelper
    {
        #region Delegates

        public delegate void Visit(File file);

        #endregion

        #region Methods

        public static void DepthFirstTraverse(this File dir, Visit visit)
        {
            visit(dir);

            var children = dir.Children;
            foreach (var d in children.Values.Where(x=>x.IsDirectory))
            {
                d.DepthFirstTraverse(visit);
            }

            foreach (var f in children.Values.Where(x=>!x.IsDirectory))
            {
                visit(f);
            }
        }

        public static void BreadthFirstTraverse(this File dir, Visit visit)
        {
            var q = new Queue<File>();

            q.Enqueue(dir);

            while (q.Count > 0)
            {
                var d = q.Dequeue();
                visit(d);

                var children = d.Children;
                foreach (var f in children.Values.Where(x=>!x.IsDirectory))
                {
                    visit(f);
                }

                foreach (var dd in children.Values.Where(x=>x.IsDirectory))
                {
                    q.Enqueue(dd);
                }
            }
        }

        public static bool IsAbsoluteDirectory(this string path)
        {
            return path.StartsWith("/");
        }

        public static File GetRoot(this File start)
        {
            var p = start;
            for (; p.Parent != null; p = p.Parent)
            {
            }
            return p;
        }

        public static File AddOrOverwriteFile(this Manager manager, File dir, string name)
        {
            File c;
            if (dir.Children.TryGetValue(name, out c))
            {
                manager.DeleteFile(c);
            }
            return manager.AddFile(dir, name);
        }

        /// <summary>
        ///  Creates a directorhy
        /// </summary>
        /// <param name="manager">The simplistic file system manager</param>
        /// <param name="path">Path is like /folder1/folder2/...</param>
        public static void CreateDirectoryIfNeeded(this Manager manager, string path)
        {
            manager.CreateDirectoryIfNeeded(manager.FileTable.Root, path);
        }

        public static File CreateDirectoryIfNeeded(this Manager manager, File baseDir, string path)
        {
            var d = baseDir;
            var segs = path.SplitPath();
            foreach (var seg in segs)
            {
                d = manager.CreateFolderIfNeeded(d, seg);
            }
            return d;
        }

        public static File CreateFolderIfNeeded(this Manager manager, File dir, string name)
        {
            File f;
            if (dir.Children.TryGetValue(name, out f))
            {
                return f;
            }
            return manager.CreateFolder(dir, name);
        }

        /// <summary>
        ///  moves a file to under a specified folder
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="srcBaseDir"></param>
        /// <param name="src">Must NOT be empty</param>
        /// <param name="dstBaseDir"></param>
        /// <param name="dstDir"></param>
        public static File MoveFileUnder(this Manager manager, File srcBaseDir, string src, File dstBaseDir, string dstDir)
        {
            var fslist = src.SplitPath().ToList();
            var fdlist = dstDir.SplitPath().ToList();
            return manager.MoveFileUnder(srcBaseDir, fslist, dstBaseDir, fdlist);
        }

        public static File MoveFileUnder(this Manager manager, File srcBaseDir, IList<string> src, File dstBaseDir,
            IList<string> dstDir)
        {
            var f1 = srcBaseDir.GetFile(src);
            var fn1 = src[src.Count - 1];

            var d2 = dstBaseDir.GetFile(dstDir);

            if (d2.Children.ContainsKey(fn1))
            {
                throw new Exception("A file with the same name already exists in the target folder");
            }
            
            manager.MoveFile(f1, d2);
            return f1;
        }

        /// <summary>
        ///  Move file such that it's path changes from srcBaseDir/src to dstBaseDir/dst
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="srcBaseDir"></param>
        /// <param name="src">Must NOT be empty</param>
        /// <param name="dstBaseDir"></param>
        /// <param name="dst">Must NOT be empty</param>
        public static File MoveFile(this Manager manager, File srcBaseDir, string src, File dstBaseDir, string dst)
        {
            var fslist = src.SplitPath().ToList();
            
            var fdlist = dst.SplitPath().ToList();
            var d2 = fdlist.GetRange(0, fdlist.Count - 1);
            var dir2 = manager.GetFile(d2);
            var fn2 = fdlist[fdlist.Count - 1];

            if (dir2.Children.ContainsKey(fn2))
            {
                throw new Exception("A file with the same name already exists in the target folder");
            }

            var f =  manager.MoveFileUnder(srcBaseDir, fslist, dstBaseDir, d2);
            f.Name = fn2;
            return f;
        }

        public static File MoveFile(this Manager manager, string src, string dst)
        {
            return manager.MoveFile(manager.FileTable.Root, src, manager.FileTable.Root, dst);
        }

        public static File GetFile(this File start, string path)
        {
            if (path.IsAbsoluteDirectory())
            {
                return start.GetRoot().GetFile(path);
            }
            var segs = path.SplitPath();
            return start.GetFile(segs);
        }

        public static File GetFile(this Manager manager, string path)
        {
            var segs = path.SplitPath();            
            return manager.GetFile(segs);
        }

        public static File GetFile(this Manager manager, IEnumerable<string> segmentedPath)
        {
            var d = manager.FileTable.Root;
            return GetFile(d, segmentedPath);
        }

        private static File GetFile(this File start, IEnumerable<string> segmentedPath)
        {
            var d = start;
            foreach (var seg in segmentedPath)
            {
                if (seg == ".")
                {
                    continue;
                }
                File sd;
                if (seg == "..")
                {
                    sd = d.Parent;
                }
                else if (!d.Children.TryGetValue(seg, out sd))
                {
                    throw new Exception("Directory not found");
                }
                d = sd;
            }
            return d;
        }

        public static StringBuilder GetPathString(this File file)
        {
            var sb = file.Parent != null ? file.Parent.GetPathString() : new StringBuilder();
            sb.Append('/');
            sb.Append(file.Name);
            return sb;
        }

        public static IEnumerable<string> SplitPath(this string path)
        {
            var sb = new StringBuilder();
            var escaping = false;
            foreach (char c in path)
            {
                if (escaping)
                {
                    sb.Append(c);
                    escaping = false;
                }
                else switch (c)
                {
                    case '/':
                        if (sb.Length > 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }
                        break;
                    case '\\':
                        escaping = true;
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            if (sb.Length > 0)
            {
                yield return sb.ToString();
            }
        }

        #endregion
    }
}
