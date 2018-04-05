using KVLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace BSPParser
{
    public class BSP {
        public uint version;

        public KeyValue[] entData;
        public string[] staticPropModels;
        public StaticProp[] staticProps;

        public ZipFile pakFile;

        public BSP(Stream stream) {
            Parse(stream);
        }

        public void Parse(Stream stream) {
            var parser = new BSPParser(this,stream);
            parser.Parse();
                
        }

        private class BSPParser {
            private Stream masterStream;
            private BSP bsp;

            private const string FILE_SIGNATURE = "VBSP";
            private const int lumpCount = 64;

            private Lump_t[] lumps;

            public BSPParser(BSP bsp,Stream stream) {
                this.bsp = bsp;
                masterStream = stream;
            }

            public void Parse() {
                using(BinaryReader reader = new BinaryReader(masterStream)) {
                    string sig = reader.Read4C(); 
                    if(sig != FILE_SIGNATURE) throw new ArgumentException("That's not a BSP file!");
                    bsp.version = reader.ReadUInt32();

                    lumps = new Lump_t[lumpCount];
                    for(uint lumpIndex = 0; lumpIndex < lumpCount; ++lumpIndex) {
                        lumps[lumpIndex] = new Lump_t(reader);
                    }

                    ParseEntData();
                    ParseGameLump();
                    ParsePakFileLump();
                }
            }

            private void ParsePakFileLump() {
                Stream lumpStream = GetLump(LumpType.LUMP_PAKFILE);
                bsp.pakFile = new ZipFile(lumpStream);
            }

            private void ParseGameLump() {
                Stream lumpStream = GetLump(LumpType.LUMP_GAME_LUMP);
                var r = new BinaryReader(lumpStream);
                uint gameSubLumpCount = r.ReadUInt32();

                for(uint subLumpIndex=0;subLumpIndex<gameSubLumpCount;++subLumpIndex) {
                    GameLump lump = new GameLump(r);

                    switch(lump.tag) {
                        case "prps":
                            ParseStaticPropLump(lump.version,new SubStream(masterStream, lump.offset,lump.length));
                            break;
                    }
                }
            }

            private void ParseStaticPropLump(ushort version, Stream stream) {
                using(BinaryReader r=new BinaryReader(stream)) {
                    ParseStaticPropLump(version, r);
                }
            }

            private void ParseStaticPropLump(ushort version, BinaryReader r) {
                uint modelCount = r.ReadUInt32();
                bsp.staticPropModels = new string[modelCount];
                for(uint modelIndex=0;modelIndex<modelCount;++modelIndex) {
                    bsp.staticPropModels[modelIndex] = r.ReadUTFString(128).TrimEnd('\0');
                }

                int leafCount = r.ReadInt32();
                r.Skip(leafCount * 2);//don't care about the leaf data

                uint propCount = r.ReadUInt32();
                bsp.staticProps = new StaticProp[propCount];
                for(uint propIndex=0;propIndex<propCount;++propIndex) {
                    bsp.staticProps[propIndex] = new StaticProp(version, r);
                }
            }

            private void ParseEntData() {
                Stream lumpStream = GetLump(LumpType.LUMP_ENTITIES);
                var r = new BinaryReader(lumpStream);
                bsp.entData = KVParser.ParseAllKVRootNodes(
                    r.ReadUTFString((int)lumpStream.Length)
                );
            }

            private Stream GetLump(LumpType lumpId) {
                Lump_t lump = lumps[(int)lumpId];

                return new SubStream(masterStream, lump.offset, lump.length);
            }

            private struct Lump_t {

                public uint offset;
                public uint length;
                public uint version;
                public string tag;

                public Lump_t(BinaryReader r) {
                    offset = r.ReadUInt32();
                    length = r.ReadUInt32();
                    version = r.ReadUInt32();
                    tag = r.Read4C();
                }
            }

            enum LumpType {
                LUMP_ENTITIES = 0,  // *
                LUMP_PLANES = 1,    // *
                LUMP_TEXDATA = 2,   // *
                LUMP_VERTEXES = 3,  // *
                LUMP_VISIBILITY = 4,    // *
                LUMP_NODES = 5, // *
                LUMP_TEXINFO = 6,   // *
                LUMP_FACES = 7, // *
                LUMP_LIGHTING = 8,  // *
                LUMP_OCCLUSION = 9,
                LUMP_LEAFS = 10,    // *
                LUMP_FACEIDS = 11,
                LUMP_EDGES = 12,    // *
                LUMP_SURFEDGES = 13,    // *
                LUMP_MODELS = 14,   // *
                LUMP_WORLDLIGHTS = 15,  // 
                LUMP_LEAFFACES = 16,    // *
                LUMP_LEAFBRUSHES = 17,  // *
                LUMP_BRUSHES = 18,  // *
                LUMP_BRUSHSIDES = 19,   // *
                LUMP_AREAS = 20,    // *
                LUMP_AREAPORTALS = 21,  // *
                LUMP_UNUSED0 = 22,
                LUMP_UNUSED1 = 23,
                LUMP_UNUSED2 = 24,
                LUMP_UNUSED3 = 25,
                LUMP_DISPINFO = 26,
                LUMP_ORIGINALFACES = 27,
                LUMP_PHYSDISP = 28,
                LUMP_PHYSCOLLIDE = 29,
                LUMP_VERTNORMALS = 30,
                LUMP_VERTNORMALINDICES = 31,
                LUMP_DISP_LIGHTMAP_ALPHAS = 32,
                LUMP_DISP_VERTS = 33,       // CDispVerts
                LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS = 34,   // For each displacement
                                                            //     For each lightmap sample
                                                            //         byte for index
                                                            //         if 255, then index = next byte + 255
                                                            //         3 bytes for barycentric coordinates
                                                            // The game lump is a method of adding game-specific lumps
                                                            // FIXME: Eventually, all lumps could use the game lump system
                LUMP_GAME_LUMP = 35,
                LUMP_LEAFWATERDATA = 36,
                LUMP_PRIMITIVES = 37,
                LUMP_PRIMVERTS = 38,
                LUMP_PRIMINDICES = 39,
                // A pak file can be embedded in a .bsp now, and the file system will search the pak
                //  file first for any referenced names, before deferring to the game directory 
                //  file system/pak files and finally the base directory file system/pak files.
                LUMP_PAKFILE = 40,
                LUMP_CLIPPORTALVERTS = 41,
                // A map can have a number of cubemap entities in it which cause cubemap renders
                // to be taken after running vrad.
                LUMP_CUBEMAPS = 42,
                LUMP_TEXDATA_STRING_DATA = 43,
                LUMP_TEXDATA_STRING_TABLE = 44,
                LUMP_OVERLAYS = 45,
                LUMP_LEAFMINDISTTOWATER = 46,
                LUMP_FACE_MACRO_TEXTURE_INFO = 47,
                LUMP_DISP_TRIS = 48,
                LUMP_PHYSCOLLIDESURFACE = 49,   // deprecated.  We no longer use win32-specific havok compression on terrain
                LUMP_WATEROVERLAYS = 50,
                LUMP_LEAF_AMBIENT_INDEX_HDR = 51,   // index of LUMP_LEAF_AMBIENT_LIGHTING_HDR
                LUMP_LEAF_AMBIENT_INDEX = 52,   // index of LUMP_LEAF_AMBIENT_LIGHTING

                // optional lumps for HDR
                LUMP_LIGHTING_HDR = 53,
                LUMP_WORLDLIGHTS_HDR = 54,
                LUMP_LEAF_AMBIENT_LIGHTING_HDR = 55,    // NOTE: this data overrides part of the data stored in LUMP_LEAFS.
                LUMP_LEAF_AMBIENT_LIGHTING = 56,    // NOTE: this data overrides part of the data stored in LUMP_LEAFS.

                LUMP_XZIPPAKFILE = 57,   // deprecated. xbox 1: xzip version of pak file
                LUMP_FACES_HDR = 58,    // HDR maps may have different face data.
                LUMP_MAP_FLAGS = 59,   // extended level-wide flags. not present in all levels
                LUMP_OVERLAY_FADES = 60, // Fade distances for overlays
            }

            private struct GameLump {
                public string tag;
                public ushort flags;
                public ushort version;
                public uint offset;
                public uint length;

                public GameLump(BinaryReader r) : this() {
                    tag = r.Read4C();
                    flags = r.ReadUInt16();
                    version = r.ReadUInt16();
                    offset = r.ReadUInt32();
                    length = r.ReadUInt32();
                }
            }

            
        }

        public class StaticProp {
            public Vector Origin;

            public StaticProp(ushort version, BinaryReader r) {
                if(version < 4) throw new NotImplementedException();

                if(version < 5) return;

                if(version>=6 && version<=7) {

                } else {

                }
                if(version < 7) return;
                if(version >= 10) {

                }
                if(version >= 9) {

                }
            }

            public struct Vector {
                public float x, y, z;
            }

            public struct QAngle {
                public float x, y, z;
            }
        }
    }
}
