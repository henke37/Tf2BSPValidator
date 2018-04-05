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
            string installPath = SteamHelper.GetInstallPathForApp(appId);

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
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_0"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_1"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_2_2"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_0"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_1"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["team_previouspoint_3_2"], "team_control_point");
                    break;


                case "func_regenerate":
                    EnsureOneTargetEntity(kv["associatedmodel"].GetString());
                    break;


                case "team_control_point_master":
                    ValidateControlPointLayout(kv["caplayout"].GetString());
                    break;

                case "trigger_capture_area":
                    EnsureOneTargetEntity(kv["area_cap_point"], "team_control_point");
                    break;
                case "trigger_timer_door":
                    EnsureOneTargetEntity(kv["area_cap_point"], "team_control_point");
                    EnsureOneTargetEntity(kv["door_name"]);
                    break;

                case "team_train_watcher":
                    EnsureZeroOrOneTargetEntity(kv["env_spark_name"], "env_spark");
                    EnsureOneTargetEntity(kv["train"], "func_tracktrain");
                    EnsureOneTargetEntity(kv["start_node"], "path_track");
                    EnsureOneTargetEntity(kv["goal_node"], "path_track");

                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_1"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_1"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_2"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_2"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_3"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_3"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_4"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_4"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_5"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_5"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_6"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_6"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_7"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_7"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["linked_pathtrack_8"], "path_track");
                    EnsureZeroOrOneTargetEntity(kv["linked_cp_8"], "team_control_point");
                    break;

                case "obj_teleporter":
                    EnsureZeroOrOneTargetEntity(kv["matchingTeleporter"], "obj_teleporter");
                    break;

                case "tf_robot_destruction_robot_spawn":
                    EnsureOneTargetEntity(kv["startpath"], "path_track");
                    break;

                case "tf_logic_player_destruction":
                    ValidateModel(kv["prop_model_name"].GetString());
                    break;

                case "tf_logic_cp_timer":
                    EnsureOneTargetEntity(kv["controlpoint"], "team_control_point");
                    break;

                case "entity_spawn_point":
                    EnsureOneTargetEntity(kv["spawn_manager_name"], "entity_spawn_manager");
                    break;

                case "tf_generic_bomb":
                    ValidateModel(kv["model"], kv["skin"]);
                    break;

                case "training_prop_dynamic":
                    ValidateModel(kv["model"], kv["skin"]);
                    break;

                case "tf_point_weapon_mimic":
                    ValidateParticleEffect(kv["ParticleEffect"].GetString());
                    break;

                case "trigger_catapult":
                    EnsureOneTargetEntity(kv["launchTarget"]);
                    break;

                case "tf_glow":
                    EnsureOneTargetEntity(kv["target"]);
                    break;


                case "info_player_teamspawn":
                    EnsureZeroOrOneTargetEntity(kv["controlpoint"], "team_control_point");
                    EnsureZeroOrOneTargetEntity(kv["round_bluespawn"], "team_control_point_round");
                    EnsureZeroOrOneTargetEntity(kv["round_redspawn"], "team_control_point_round");
                    break;

                case "item_teamflag":
                    if(kv["flag_model"]!=null) ValidateModel(kv["flag_model"]);
                    break;

                case "info_observer_point":
                    EnsureZeroOrOneTargetEntity(kv["associated_team_entity"]);
                    break;

                //entclasess with no current validation
                case "worldspawn":
                case "func_door":
                case "func_areaportal":
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
                case "game_round_win":
                case "logic_auto":
                case "logic_relay":
                case "func_respawnroomvisualizer":
                case "func_nobuild":
                case "light":
                    break;

                default:
                    break;
            }
        }

        private void ValidateControlPointLayout(string v) {
            if(!Regex.IsMatch(v, @"^[0-7 ,]*[0-7]$")) error("Bad capture point layout");
        }

        private void ValidateItemEntity(KeyValue kv) {
            KeyValue modelOverrideKv = kv["powerup_model"];
            if(modelOverrideKv == null) return;
            ValidateModel(modelOverrideKv.GetString());
        }


        private void EnsureOneTargetEntity(KeyValue targetName, string targetClass = null) {
            if(targetName == null) {
                error("No entity specified");
                return;
            }
            EnsureOneTargetEntity(targetName.GetString(), targetClass);
        }

        private void EnsureOneTargetEntity(string targetName, string targetClass = null) {
            if(!entTargetNameMap.TryGetValue(targetName, out List<KeyValue> nameList)) {
                error($"Missing entity with targetname {targetName}");
            }
            if(nameList.Count != 1) error($"Duplicate entities with targetname {targetName}");
            var target = nameList[0];
            if(targetClass != null && target["classname"].GetString() != targetClass) {
                error("Target entity class mismatch");
            }
        }

        private void EnsureZeroOrOneTargetEntity(KeyValue targetName, string targetClass = null) {
            if(targetName == null) return;
            EnsureZeroOrOneTargetEntity(targetName.GetString(), targetClass);
        }
        private void EnsureZeroOrOneTargetEntity(string targetName, string targetClass = null) {
            if(!entTargetNameMap.TryGetValue(targetName, out List<KeyValue> nameList)) {
                return;
            }
            if(nameList.Count != 1) error($"Duplicate entities with targetname {targetName}");
            var target = nameList[0];
            if(targetClass != null && target["classname"].GetString() != targetClass) {
                error("Target entity class mismatch");
            }
        }

        private void BuildEntTargetNameLUT() {
            entTargetNameMap = new Dictionary<string, List<KeyValue>>();

            foreach(var kv in bsp.entData) {
                KeyValue nameKv = kv["targetname"];
                if(nameKv == null) continue;
                string targetName = nameKv.GetString();
                List<KeyValue> nameList;
                if(!entTargetNameMap.TryGetValue(targetName, out nameList)) {
                    nameList = new List<KeyValue>();
                    entTargetNameMap.Add(targetName, nameList);
                }
                nameList.Add(kv);
            }
        }

        private void ValidateFile(string name) {
            //Try the pakfile
            var entry = bsp.pakFile.GetEntry(name);
            if(entry != null) return;

        }


        private void ValidateModel(KeyValue kvname, KeyValue kvskin = null) {
            int skin = (kvskin == null) ? 0 : kvskin.GetInt();
            ValidateModel(kvname.GetString(), skin);
        }

        private void ValidateModel(string name, int skin = 0) {
            ValidateFile(name);
        }

        private void ValidateMaterial(string name) {
            ValidateFile(name);
        }

        private void ValidateTexture(string name) {
            ValidateFile(name);
        }

        private void ValidateParticleEffect(string name) {

        }

        private void error(string msg) {
            Console.WriteLine(msg);
        }
    }
}
