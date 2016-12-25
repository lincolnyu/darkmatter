using System.Collections.Generic;

namespace DarkMatter.Simplistic
{
    public class File
    {
        #region Constructors

        public File()
        {
            Children = new Dictionary<string, File>();
        }

        #endregion

        #region Properties

        /// <summary>
        ///  Ids for active files are positive
        /// </summary>
        public uint Id { get; set; }

        public File Parent { get; set; }

        public string Name { get; set; }

        /// <summary>
        ///  Start position of the content in the data pool
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        ///  Length of the content in the data pool 
        /// </summary>
        public long Length { get; set; }

        public IDictionary<string, File> Children { get; private set; }

        public bool IsRoot
        {
            get { return Parent == null; }
        }

        public bool IsDirectory
        {
            get { return Start < -1; }
        }

        public bool IsToDelete { get; set; }

        #endregion
    }
}
