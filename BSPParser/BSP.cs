using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPParser
{
    public class BSP {
        private const string FILE_SIGNATURE="VBSP";

        public BSP(Stream stream) {
            Parse(stream);
        }

        public void Parse(Stream stream) {
            using(BinaryReader r=new BinaryReader(stream)) {
                string sig = new string(r.ReadChars(4));
                if(sig != FILE_SIGNATURE) throw new ArgumentException("That's not a BSP file!");
            }
        }
    }
}
