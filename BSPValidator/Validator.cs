using BSPParser;
using KVLib;
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

        private Dictionary<string, List<KeyValue>> entTargetNameMap;

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

        public BSPValidator(Stream stream) {
            Init(stream);
        }

        private void Init(Stream stream) {
            bsp = new BSP(stream);
        }

        public void Validate() {
            if(bsp.version != TF2_BSP_VERSION) error("Wrong BSP Version");

            ValidateEntities();
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
                    ValidateItemEntity(kv);
                    break;

                case "team_control_point":
                    EnsureOneTargetEntity(kv["targetname"].GetString());
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
                case "team_control_point_master":
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
                case "func_regenerate":
                case "light":
                    break;

                default:
                    break;
            }
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

        private void ValidateModel(string v) {
            
        }

        private void ValidateMaterial(string name) {

        }

        private void error(string msg) {
            Console.WriteLine(msg);
        }
    }
}
