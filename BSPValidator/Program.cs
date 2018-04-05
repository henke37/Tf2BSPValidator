using BSPParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPValidator {
    class Program {
        static void Main(string[] args) {
            if(args.Length!=1) {
                Console.WriteLine("One argument, the file to validate.");
                return;
            }

            Stream s = File.OpenRead(args[0]);
            BSP bsp = new BSP(s);
        }
    }
}
