
using System;
using System.IO;

namespace DarkMatter.Simplistic
{
    public class FileTable
    {
        #region Fields

        private Stream _stream;

        #endregion

        #region Constructors

        #endregion

        #region Properties

        public File Root { get; private set; }

        public Stream Stream
        {
            get { return _stream; }
            set
            {
                if (_stream != value)
                {
                    if (_stream != null)
                    {
                        Detach();
                    }
                    _stream = value;
                    if (_stream != null)
                    {
                        Attach();
                    }
                }
            }
        }

        public uint NextId { get; set; }

        #endregion

        #region Methods

        private void Attach()
        {
            NextId = Load();
        }

        private void Detach()
        {
            Save();
            Root = null;
            NextId = 0;
        }

        private uint Load(bool toReid = true)
        {
            File lastFile = null;
            Root = null;
            uint lastId = 1;

            try
            {
                using (var br = new BinaryReader(_stream))
                {
                    while (true)
                    {
                        var id = br.ReadUInt32();
                        var parentId = br.ReadUInt32();
                        var name = br.ReadString();
                        var start = br.ReadInt64();
                        long len = 0;
                        if (start >= 0)
                        {
                            len = br.ReadInt64();
                        }

                        var file = new File {Id = id, Name = name, Start = start, Length = len};
                        if (id > lastId)
                        {
                            lastId = id;
                        }
                        if (parentId > 0)
                        {
                            var parent = FindParent(lastFile, parentId);
                            if (parent == null)
                            {
                                throw new Exception("Directory corrupted: Can't find parent");
                            }

                            file.Parent = parent;
                        }
                        else if (Root != null)
                        {
                            Root = file;
                        }
                        else
                        {
                            throw new Exception("Directory corrupted: Multiple roots");
                        }

                        lastFile = file;
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }

            if (toReid)
            {
                ReId(Root, ref lastId);
                return lastId;
            }
            return lastId + 1;
        }

        private static void ReId(File file, ref uint id)
        {
            file.Id = ++id;
            foreach (var c in file.Children)
            {
                c.Id = ++id;
                ReId(c, ref id);
            }
        }

        private static File FindParent(File refFile, uint parentId)
        {
            if (refFile.Id == parentId)
            {
                return refFile;
            }
            if (refFile.Parent == null)
            {
                return null;
            }
            return FindParent(refFile.Parent, parentId);
        }

        private void Save()
        {
            using (var bw = new BinaryWriter(_stream))
            {
                SaveNode(Root, bw);
            }
        }

        private void SaveNode(File node, BinaryWriter bw)
        {
            var id = node.Id;
            var parentId = node.Parent != null? node.Parent.Id : 0;
            bw.Write(id);
            bw.Write(parentId);
            bw.Write(node.Name);
            bw.Write(node.Start);
            if (!node.IsDirectory)
            {
                bw.Write(node.Length);
            }

            foreach (var c in node.Children)
            {
                SaveNode(c, bw);
            }
        }

        #endregion 
    }
}
