using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieTorrentSharp
{
    public class Movie
    {
        public string DisplayName { get; set; }
        public string MagnetLink { get; set; }
        public string ContentLink { get; set; }
        public bool EnglishAudio { get; set; }
        public string Size { get; set; }
        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
