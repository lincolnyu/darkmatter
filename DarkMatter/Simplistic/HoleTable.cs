using System.Collections.Generic;
using System.IO;

namespace DarkMatter.Simplistic
{
    public class HoleTable
    {
        #region Fields

        private Stream _stream;

        #endregion

        #region Constructors

        public HoleTable()
        {
            Holes = new List<Hole>();
        }

        #endregion

        #region Properties

        public List<Hole> Holes { get; private set; }

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

        #endregion

        #region Methods

        private void Attach()
        {
            Load();
        }

        private void Detach()
        {
            Save();
            Holes.Clear();
        }

        private void Load()
        {
            try
            {
                using (var br = new BinaryReader(_stream))
                {
                    while (true)
                    {
                        var start = br.ReadInt64();
                        var len = br.ReadInt64();
                        var hole = new Hole
                        {
                            Start = start,
                            Length = len
                        };
                        Holes.Add(hole);
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }
        }

        private void Save()
        {
            using (var bw = new BinaryWriter(_stream))
            {
                foreach (var hole in Holes)
                {
                    bw.Write(hole.Start);
                    bw.Write(hole.Length);
                }
            }
        }

        public void Add(Hole hole)
        {
            var index =  Holes.BinarySearch(hole);
            if (index < 0)
            {
                index = -index - 1;
            }

            var adjacentToNext = index < Holes.Count && hole.Start + hole.Length == Holes[index].Start;
            var adjacentToPrev = index > 0 && hole.Start == Holes[index-1].Start + Holes[index-1].Length;
            if (adjacentToNext && adjacentToPrev)
            {
                var next = Holes[index];
                Holes[index - 1].Length = next.Start + next.Length - Holes[index - 1].Start;
                Holes.RemoveAt(index);
            }
            else if (adjacentToNext)
            {
                Holes[index].Start = hole.Start;
                Holes[index].Length += hole.Length;
            }
            else if (adjacentToPrev)
            {
                Holes[index - 1].Length += hole.Length;
            }
            else
            {
                Holes.Insert(index, hole);
            }
        }

        #endregion

    }
}
