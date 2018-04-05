using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPParser {
    public static class ExtensionFunctions {
        public static string Read4C(this BinaryReader r) {
            return new string(r.ReadChars(4));
        }

        public static string ReadUTFString(this BinaryReader r, int bytesToRead) {
            var ba = r.ReadBytes(bytesToRead);
            return new String(Encoding.UTF8.GetChars(ba, 0, bytesToRead - 1));
        }

        public static string ReadAsUTF8(this Stream s) {
            var ba = new byte[s.Length];
            s.Read(ba, 0, (int)s.Length);
            return new string(Encoding.UTF8.GetChars(ba));
        }

        public static void Skip(this BinaryReader r, int bytesToSkip) {
            r.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
        }

        public static void Seek(this BinaryReader r, int bytesToSkip) {
            r.BaseStream.Seek(bytesToSkip, SeekOrigin.Begin);
        }

        public static void Seek(this BinaryReader r, int bytesToSkip, SeekOrigin seekOrigin) {
            r.BaseStream.Seek(bytesToSkip, seekOrigin);
        }

        public static void WriteLenPrefixedUTFString(this BinaryWriter w, string str) {
            var ba = Encoding.UTF8.GetBytes(str + "\0");
            w.Write((int)ba.Length);
            w.Write(ba);
        }

        public static long BytesLeft(this BinaryReader r) {
            return r.BaseStream.Length - r.BaseStream.Position;
        }

        public static string ReadNullTerminatedUTF8String(this BinaryReader r) {
            var l = new List<Byte>();
            for(; ; ) {
                byte b = r.ReadByte();
                if(b == 0) break;
                l.Add(b);
            }
            return new string(Encoding.UTF8.GetChars(l.ToArray()));
        }
    }
}
