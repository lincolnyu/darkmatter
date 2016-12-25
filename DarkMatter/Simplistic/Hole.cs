using System;

namespace DarkMatter.Simplistic
{
    public class Hole : IComparable<Hole>
    {
        #region Properties

        public long Start { get; set; }

        public long Length { get; set; }

        #endregion

        #region Methods

        #region IComparable<Hole> members

        public int CompareTo(Hole other)
        {
            return Start.CompareTo(Start);
        }

        #endregion

        #endregion
    }
}
