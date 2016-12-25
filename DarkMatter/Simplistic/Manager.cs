using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DarkMatter.Simplistic
{
    public class Manager
    {
        #region Fields

        private Stream _dataStream;

        #endregion

        #region Constructors

        public Manager()
        {
            FileTable = new FileTable();
            DeletedFiles = new HashSet<File>();
            HoleTable = new HoleTable();
        }

        #endregion

        #region Properties

        public FileTable FileTable { get; private set; }

        public HoleTable HoleTable { get; private set; }

        public HashSet<File> DeletedFiles { get; private set; } 

        #endregion

        #region Methods

        public void Attach(Stream dataStream, Stream fileTableStream, Stream holeStream)
        {
            FileTable.Stream = fileTableStream;
            HoleTable.Stream = holeStream;

            _dataStream = dataStream;
        }

        public void Detach()
        {
            FlushDeletions();
            FileTable.Stream = null;
            HoleTable.Stream = null;
        }

        #region Read file

        public void AccessFile(File file)
        {
            var start = file.Start;
            _dataStream.Seek(start, SeekOrigin.Begin);
        }

        public async Task ReadAsync(byte[] buffer, int offset, int count)
        {
            await _dataStream.ReadAsync(buffer, offset, count);
        }

        #endregion

        #region Add file

        /// <summary>
        ///  Adds a file under the specified directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public File AddFile(File dir, string name)
        {
            // appends the file at the end
            _dataStream.Seek(0, SeekOrigin.End);
            var start = _dataStream.Position;
            var newf = new File
            {
                Id = FileTable.NextId++,
                Name = name,
                Parent = dir,
                Start = start
                // leave the length to the finalization process
            };
            dir.Children.Add(name, newf);
            return newf;
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count)
        {
            await _dataStream.WriteAsync(buffer, offset, count);
        }

        public void FinalizeFile(File file)
        {
            var length = _dataStream.Position - file.Start;
            file.Length = length;
        }

        #endregion

        /// <summary>
        ///  Creates a folder under the specified directory with the specified name
        ///  and throw an exception if the folder already exists
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public File CreateFolder(File dir, string name)
        {
            var folder = new File
            {
                Id = FileTable.NextId++,
                Name = name,
                Parent = dir,
                Start = -1
                // leave the length to the finalization process
            };
            dir.Children.Add(name, folder);
            return folder;
        }

        /// <summary>
        ///  Deletes file recursively
        /// </summary>
        /// <param name="file"></param>
        public IEnumerable<File> DeleteFile(File file)
        {
            foreach (var sf in file.Children.Values)
            {
                var fs = DeleteFile(sf);
                foreach (var f in fs)
                {
                    yield return f;
                }
            }
            file.IsToDelete = true;
            DeletedFiles.Add(file);
            yield return file;
        }

        public void UndeleteFiles(IEnumerable<File> files)
        {
            foreach (var file in files)
            {
                file.IsToDelete = false;
                DeletedFiles.Remove(file);
            }
        }

        public void UndeleteAll()
        {
            foreach (var file in DeletedFiles)
            {
                file.IsToDelete = false;
            }
            DeletedFiles.Clear();
        }

        /// <summary>
        ///  Move the specified file (or directory) to the target folder
        /// </summary>
        /// <param name="file">The file to move</param>
        /// <param name="targetFolder">The folder to move the file under</param>
        public void MoveFile(File file, File targetFolder)
        {
            if (file.Parent == null)
            {
                throw new Exception("Can't move root dir");
            }
            if (file.Parent != targetFolder)
            {
                if (targetFolder.Children.ContainsKey(file.Name))
                {
                    throw new Exception("Target folder contains a file with the same name");
                }

                file.Parent.Children.Remove(file.Name);
                targetFolder.Children.Add(file.Name, file);
                file.Parent = targetFolder;
            }
        }

        private void FlushDeletions()
        {
            foreach (var df in DeletedFiles)
            {
                df.Parent.Children.Remove(df.Name);
                var hole = new Hole
                {
                    Start = df.Start,
                    Length = df.Length
                };
                HoleTable.Add(hole);
            }
            DeletedFiles.Clear();
        }

        public void Defrag()
        {
            long nextStart = -1;
            for (var i = 0; i < HoleTable.Holes.Count; i++)
            {
                var hole = HoleTable.Holes[i];
                // the current hole start
                var holeStart = nextStart < 0 ? hole.Start : nextStart;
                var holeLen = hole.Start + hole.Length - holeStart;

                var dataOffset = holeStart + holeLen;

                var nextHole = i < HoleTable.Holes.Count - 1 ? HoleTable.Holes[i + 1].Start : _dataStream.Position;
                var chunkLen = nextHole - dataOffset;
                if (chunkLen > 0)
                {
                    MoveChunkLeft(dataOffset, chunkLen, holeStart);
                }
                nextStart = holeStart + chunkLen;
            }

            if (nextStart >= 0)
            {
                _dataStream.SetLength(nextStart);
            }

            HoleTable.Holes.Clear();
        }

        private void MoveChunkLeft(long oldOffset, long length, long newOffset)
        {
            const int bufSize = 4096;
            var buf = new byte[bufSize];
            var end = oldOffset + length;
            for (long o = oldOffset, n = newOffset;  o < end; o += bufSize, n += bufSize)
            {
                var len = (int)Math.Min(bufSize, end - o);
                _dataStream.Position = o;
                _dataStream.Read(buf, 0, len);
                _dataStream.Position = n;
                _dataStream.Write(buf, 0, len);
            }
        }

        #endregion
    }
}
