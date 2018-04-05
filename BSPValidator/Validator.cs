using BSPParser;
using KVLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BSPValidator {
    class BSPValidator {
        private const uint TF2_BSP_VERSION = 20;
        private const int TF2_STEAM_APPID = 440;

        private BSP bsp;

        private Dictionary<string, List<KeyValue>> entTargetNameMap;

        static void Main(string[] args) {
            if(args.Length != 1) {
                Console.WriteLine("One argument, the file to validate.");
                return;
            }

            var v = new BSPValidator(args[0]);
            v.MountGame(TF2_STEAM_APPID);
            v.Validate();
        }

        private void MountGame(int appId) {
            string installPath=SteamHelper.GetInstallPathForApp(appId);

        }

        public BSPValidator(string filename) {
            Init(File.OpenRead(filename));
        }

        public BSPValidator(Stream stream) {
            Init(stream);
        }

        private void Init(Stream stream) {
            bsp = new BSP(stream);
        }

        public void Validate() {
            if(bsp.version != TF2_BSP_VERSION) error("Wrong BSP Version");

            ValidateStaticModels();
            ValidateEntities();
        }

        private void ValidateStaticModels() {
            foreach(string modelName in bsp.staticPropModels) {
                ValidateModel(modelName);
            }
        }

        private void ValidateEntities() {
            BuildEntTargetNameLUT();
            foreach(var kv in bsp.entData) {
                try {
                    ValidateEntity(kv);
                } catch(Exception ex) {
                    error(ex.Message);
                }
            }
        }

        private void ValidateEntity(KeyValue kv) {
            string classname = kv["classname"].GetString();

            switch(classname) {
                case "prop_dynamic":
                case "prop_physics":
                    ValidateModel(kv["model"].GetString());
                    break;

                case "item_healthkit_small":
                case "item_healthkit_medium":
                case "item_healthkit_large":
                case "item_ammopack_small":
                case "item_ammopack_medium":
                case "item_ammopack_large":
                case "tf_spell_pickup":
                    ValidateItemEntity(kv);
                    break;

                case "team_control_point":
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_0"].GetString());
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_1"].GetString());
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_2"].GetString());
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_0"].GetString());
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_1"].GetString());
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_2"].GetString());
                    break;


                case "func_regenerate":
                    EnsureOneTargetEntity(kv["associatedmodel"].GetString());
                    break;


                case "team_control_point_master":
                    ValidateControlPointLayout(kv["caplayout"].GetString());
                    break;

                //entclasess with no current validation
                case "worldspawn":
                case "func_door":
                case "func_areaportal":
                case "info_player_teamspawn":
                case "trigger_multiple":
                case "trigger_hurt":
                case "filter_activator_tfteam":
                case "tf_gamerules":
                case "shadow_control":
                case "light_environment":
                case "env_tonemap_controller":
                case "env_fog_controller":
                case "water_lod_control":
                case "team_round_timer":
                case "func_respawnroom":
                case "trigger_capture_area":
                case "game_round_win":
                case "logic_auto":
                case "logic_relay":
                case "func_respawnroomvisualizer":
                case "func_nobuild":
                case "info_observer_point":
                case "light":
                    break;

                default:
                    break;
            }
        }

        private void ValidateControlPointLayout(string v) {
            if(!Regex.IsMatch(v,@"^[0-7 ,]*[0-7]$")) error("Bad capture point layout");
        }

        private void ValidateItemEntity(KeyValue kv) {
            KeyValue modelOverrideKv = kv["powerup_model"];
            if(modelOverrideKv == null) return;
            ValidateModel(modelOverrideKv.GetString());
        }

        private void EnsureOneTargetEntity(string targetName) {
            if(!entTargetNameMap.TryGetValue(targetName, out List<KeyValue> nameList)) {
                error($"Missing entity with targetname {targetName}");
            }
            if(nameList.Count != 1) error($"Duplicate entities with targetname {targetName}");
        }
        private void EnsureZeroOrOneTargetEntity(string targetName) {
            if(!entTargetNameMap.TryGetValue(targetName, out List<KeyValue> nameList)) {
                return;
            }
            if(nameList.Count != 1) error($"Duplicate entities with targetname {targetName}");
        }

        private void BuildEntTargetNameLUT() {
            entTargetNameMap = new Dictionary<string, List<KeyValue>>();

            foreach(var kv in bsp.entData) {
                KeyValue nameKv = kv["targetname"];
                if(nameKv == null) continue;
                string targetName = nameKv.GetString();
                List<KeyValue> nameList;
                if(!entTargetNameMap.TryGetValue(targetName,out nameList)) {
                    nameList = new List<KeyValue>();
                    entTargetNameMap.Add(targetName, nameList);
                }
                nameList.Add(kv);
            }
        }

        private void ValidateFile(string name) {
            //Try the pakfile
            var entry=bsp.pakFile.GetEntry(name);
            if(entry!=null) return;

        }

        private void ValidateModel(string name) {
            ValidateFile(name);
        }

        private void ValidateMaterial(string name) {
            ValidateFile(name);
        }

        private void ValidateTexture(string name) {
            ValidateFile(name);
        }

        private void error(string msg) {
            Console.WriteLine(msg);
        }
    }
}
