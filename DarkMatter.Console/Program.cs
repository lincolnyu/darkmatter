using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DarkMatter.Helpers;
using DarkMatter.Simplistic;
using Sc = System.Console;
using Dmf = DarkMatter.Simplistic.File;

namespace DarkMatter.Console
{
    class Program
    {


        #region Fields

        private static Dmf _currentDir;
        private static Manager _manager;

        private static readonly List<List<Dmf>> DeletedFiles = new List<List<Dmf>>(); 

        #endregion

        #region Methods

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Sc.WriteLine("Usage: dmc <datafile> <treefile> <holefile>");
                return;
            }

            var fndata = args[0];
            var fntree = args[1];
            var fnholes = args[2];

            using(var sdata = new FileStream(fndata, FileMode.OpenOrCreate))
            using (var stree = new FileStream(fntree, FileMode.OpenOrCreate))
            using (var sholes = new FileStream(fnholes, FileMode.OpenOrCreate))
            {
                _manager = new Manager();
                _manager.Attach(sdata, stree, sholes);

                _currentDir = _manager.FileTable.Root;

                var running = true;
                while (running)
                {
                    Sc.Write("> ");
                    var cmdline = Sc.ReadLine();
                    if (cmdline == null)
                    {
                        continue;
                    }
                    var tlccmdline = cmdline.Trim().ToLower();
                    string cmd = null;
                    try
                    {
                        switch (tlccmdline)
                        {
                            case "quit":
                            case "exit":
                                running = false;
                                break;
                        }
                        var segs = SplitCommand(tlccmdline).ToArray();
                        switch (cmd = segs[0])
                        {
                            case "receive":
                            case "recv":
                                Receive(segs);
                                break;
                            case "send":
                                Send(segs);
                                break;
                            case "move":
                                Move(segs);
                                break;
                            case "del":
                            case "rm":
                                Delete(segs);
                                break;
                            case "dir":
                            case "ls":
                                List(segs);
                                break;
                            case "cd":
                            case "chdir":
                                ChangeDir(segs);
                                break;
                            case "help":
                                PrintHelp(segs);
                                break;
                        }
                    }
                    catch (CommandException)
                    {
                        // TODO display how to use the command   
                        PrintHint(cmd);
                    }
                    catch (Exception e)
                    {
                        Sc.WriteLine("Exception: {0}", e);
                    }
                }

                _manager.Detach();
            }
        }

        private static void PrintHint(string s)
        {
            switch (s)
            {
                case "receive":
                case "recv":
                    Sc.WriteLine("rec[ei]v[e] <local dir> <dm dir>");
                    break;
                case "send":
                    Sc.WriteLine("send <local dir> <dm dir>");
                    break;
                case "move":
                    Sc.WriteLine("send <source> <destination>");
                    break;
                case "del":
                case "rm":
                    Sc.WriteLine("del/rm <path>");
                    break;
                case "dir":
                case "ls":
                    Sc.WriteLine("dir/ls [-r]");
                    break;
                case "cd":
                case "chdir":
                    Sc.WriteLine("c[h]d[ir] <path>");
                    break;
                case "help":
                    Sc.WriteLine("help [<command>]");
                    break;
            }
        }

        private static void PrintHelp(string[] args)
        {
            // TODO implement it
        }

        private static void ChangeDir(string[] args)
        {
            if (args.Length == 1)
            {
                return;
            }

            if (args.Length != 2)
            {
                throw new CommandException();
            }

            var tgt = args[1];
            switch (tgt)
            {
                case ".":
                    return;
                case "..":
                    if (_currentDir.Parent != null)
                    {
                        _currentDir = _currentDir.Parent;
                    }
                    break;
                case "/":
                    _currentDir = _manager.FileTable.Root;
                    break;
                default:
                    _currentDir = _currentDir.GetFile(tgt);
                    break;
            }
            throw new CommandException();
        }

        private static void List(string[] args)
        {
            var isRecursive = false;

            // TODO implement: ls <path>
            if (args.Length == 2)
            {
                var a = args[1];
                if (a.Trim().ToLower() == "-r")
                {
                    isRecursive = true;
                }
            }

            if (isRecursive)
            {
                ListRecursive(_currentDir);
            }
            else
            {
                ListCurrent(_currentDir);
            }
        }

        private static void ListCurrent(Dmf file)
        {
            Sc.WriteLine(" Directory of {0}", file.GetPathString());
            foreach (var sf in file.Children.Values)
            {
                var size = sf.Length;
                var name = sf.Name;
                var isDir = sf.IsDirectory;
                Sc.WriteLine("{0:0,000,000,000} {1} {2}", size, isDir? "<DIR>" : "     ", name);
            }
            Sc.WriteLine();
        }

        private static void ListRecursive(Dmf file)
        {
            ListCurrent(file);
            foreach (var sf in file.Children.Values)
            {
                ListRecursive(sf);
            }
        }

        private static void Delete(string[] args)
        {
            if (args.Length != 2)
            {
                throw new CommandException();
            }

            var path = args[1];

            var f = _currentDir.GetFile(path);
            var files = _manager.DeleteFile(f).ToList();
            DeletedFiles.Add(files);
        }

        private static void Move(string[] args)
        {
            if (args.Length != 3)
            {
                throw new CommandException();
            }

            var src = args[1];
            var dst = args[2];
            _manager.MoveFile(_currentDir, src, _currentDir, dst);
        }

        private static void Send(string[] args)
        {
            if (args.Length != 3)
            {
                throw new CommandException();
            }
            var local = args[1];
            var dmdir = args[2];

            Send(local, dmdir);
        }

        private static void Receive(string[] args)
        {
            if (args.Length != 3)
            {
                throw new CommandException();
            }
            var local = args[1];
            var dmdir = args[2];

            Receive(local, dmdir);
        }

        /// <summary>
        ///  Sends a file or directory to the remote directory such that
        ///  1. if it's a file, the file will sit in that directory
        ///  2. if it's a directory, it will sit under that directory with
        ///     all its contents
        /// </summary>
        /// <param name="local"></param>
        /// <param name="dmdir">The path to the remote folder</param>
        private static async void Send(string local, string dmdir)
        {
            var isFile = System.IO.File.Exists(local);
            if (isFile)
            {
                var f = new FileInfo(local);
                var d = _manager.GetFile(dmdir);
                var dmf = _manager.AddFile(d, f.Name);
                await Sender.CopyFile(_manager, f);
                _manager.FinalizeFile(dmf);
            }
            var isDir = Directory.Exists(local);
            if (isDir)
            {
                // All files in the directory
                var dir = new DirectoryInfo(local);
                var sender = new Sender(_manager, dir, dmdir);
                dir.DepthFirstTraverse(sender.VisitFile);
            }
            throw new Exception("Invalid path");
        }

        /// <summary>
        ///  Receives a file or a directory from the remote site and
        ///  put it under the given local directory
        /// </summary>
        /// <param name="localDir"></param>
        /// <param name="dmdir"></param>
        private static void Receive(string localDir, string dmdir)
        {
            var ldir = new DirectoryInfo(localDir);
            var dmf = _manager.GetFile(dmdir);
            if (dmf.IsDirectory)
            {
                // is a directory
                var receiver = new Receiver(_manager, ldir);
                dmf.DepthFirstTraverse(receiver.VisitFile);
            }
            else
            {
                // is a file
            }
        }

        private static IEnumerable<string> SplitCommand(string cmdline)
        {
            var sb = new StringBuilder();
            var escaping = false;
            var inquote = false;
            foreach (var c in cmdline)
            {
                if (escaping)
                {
                    sb.Append(c);
                    escaping = false;
                    continue;
                }

                switch (c)
                {
                    case ' ':
                        if (inquote)
                        {
                            sb.Append(c);
                        }
                        else if (sb.Length > 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }
                        break;
                    case '\\':
                        escaping = true;
                        break;
                    case '\"':
                        inquote = !inquote;
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
