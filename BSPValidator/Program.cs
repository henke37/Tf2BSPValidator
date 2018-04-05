using BSPParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPValidator {
    class BSPValidator {
        private const uint TF2_BSP_VERSION=20;

        private BSP bsp;

        static void Main(string[] args) {
            if(args.Length != 1) {
                Console.WriteLine("One argument, the file to validate.");
                return;
            }

            var v = new BSPValidator(args[0]);
            v.Validate();
        }

        public BSPValidator(string filename) {
            Init(File.OpenRead(filename));
        }

        private BSPValidator(Stream stream) {
            Init(stream);
        }

        private void Init(Stream stream) {
            bsp = new BSP(stream);
        }

        private void Validate() {
            if(bsp.version != TF2_BSP_VERSION) error("Wrong BSP Version");

        }

        private void error(string msg) {
            Console.WriteLine(msg);
        }
    }
}
