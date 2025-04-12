using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Barotrauma.Networking;
using System.Threading.Tasks;
using Barotrauma.Items.Components;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using HarmonyLib;

namespace Barotrauma
{
    public static partial class LuaForSumneko
    {
        public readonly static string DocumentationRelativePath = "LuaForSumenko";

        public static ImmutableHashSet<string> LuaKeyWords = ImmutableHashSet.Create(
            "and", "break", "do", "else", "elseif", "end", "false",
            "for", "function", "goto", "if", "in", "local", "nil",
            "not", "or", "repeat", "return", "then", "true", "until", "while"
        );

        public static Dictionary<Int64, StringBuilder> LuaClrDefinitions = new Dictionary<Int64, StringBuilder>();

        public static List<(ClassMetadata derivedMetadata, ClassMetadata baseMetadata)> LuaClrBasePairs = new List<(ClassMetadata, ClassMetadata)>();

        public static List<ClassMetadata> LualyRecorder = new List<ClassMetadata>() { };

        public static Dictionary<Int64, StringBuilder> OverloadedOperatorAnnotations = new Dictionary<Int64, StringBuilder>();
        private static string[] _overloadedOperators = { "op_UnaryNegation", "op_Addition", "op_Subtraction", "op_Multiply", "op_Division" };
        private static bool IsOverloadedOperatorMethod(MethodInfo method)
            => method.IsSpecialName && _overloadedOperators.Contains(method.Name);

        public static void Execute()
        {
            if (!Directory.Exists(DocumentationRelativePath))
            {
                Directory.CreateDirectory(DocumentationRelativePath);
            }
            else
            {
                foreach (var subfile in Directory.GetFiles(DocumentationRelativePath))
                {
                    File.Delete(subfile);
                }
            }

            LualyAll();

            var _0Global = new StringBuilder();
            ExplanNewLine(_0Global);

            int prevCount = -1;
            while (prevCount < LuaClrBasePairs.Count)
            {
                prevCount = LuaClrBasePairs.Count;
                foreach (var pair in LuaClrBasePairs.ToArray())
                {
                    if (pair.baseMetadata == ClassMetadata.Empty) { GetLuaInheritPartial(pair.derivedMetadata, null); }
                    else { GetLuaInheritPartial(pair.derivedMetadata, pair.baseMetadata.OriginalType); }
                }
            }

            LuaClrBasePairs.RemoveAll(pair => LualyRecorder.Contains(pair.derivedMetadata));

            foreach (var pair in LuaClrBasePairs.ToArray())
            {
                var inheritPartial = (pair.baseMetadata == ClassMetadata.Empty)
                    ? GetLuaInheritPartial(pair.derivedMetadata, null)
                    : GetLuaInheritPartial(pair.derivedMetadata, pair.baseMetadata.OriginalType);
                if (inheritPartial.IsNullOrEmpty())
                {
                    _0Global.AppendLine($@"---@class {pair.derivedMetadata.LuaClrName}");
                }
                else
                {
                    _0Global.AppendLine($@"---@class {pair.derivedMetadata.LuaClrName} : {inheritPartial}");
                }
            }

            _0Global.Insert(0, "---@meta\n");
            File.WriteAllText(Path.Combine(DocumentationRelativePath, $"{nameof(_0Global)}.lua"), _0Global.ToString());

            foreach (var (token, builder) in LuaClrDefinitions)
            {
                int startIndex = builder.ToString().IndexOf("[placeHolder:operators]");
                builder.Remove(startIndex, "[placeHolder:operators]".Length + 2); // remove the last newline, @field must be defined after @class
                if (OverloadedOperatorAnnotations.TryGetValue((Int64)token, out StringBuilder ops))
                {
                    builder.Insert(startIndex, ops.ToString());
                }

                File.WriteAllText(Path.Combine(DocumentationRelativePath, $"_{Convert.ToString(token, 16).PadLeft(16, '0').ToUpperInvariant()}.lua"), builder.ToString());
            }
        }

        public static void LualyAll()
        {
            Lualy<System.Object>();
            Lualy<System.String>();
            Lualy<System.Boolean>();
            Lualy<System.SByte>();
            Lualy<System.Byte>();
            Lualy<System.Int16>();
            Lualy<System.UInt16>();
            Lualy<System.Int32>();
            Lualy<System.UInt32>();
            Lualy<System.Int64>();
            Lualy<System.UInt64>();
            Lualy<System.Single>();
            Lualy<System.Double>();

            Lualy<System.TimeSpan>(new string[] { "TimeSpan" });
            LualyBase(typeof(System.Console));
            Lualy<System.Exception>();

#if CLIENT
            Lualy<Media.Video>();
            Lualy<Microsoft.Xna.Framework.Graphics.SpriteBatch>();
            Lualy<Microsoft.Xna.Framework.Graphics.Texture2D>();
            Lualy<Microsoft.Xna.Framework.Graphics.SpriteEffects>(new string[] { "SpriteEffects" });
#endif
            Lualy<Microsoft.Xna.Framework.Matrix>(new string[] { "Matrix" });
            Lualy<Microsoft.Xna.Framework.Vector2>(new string[] { "Vector2" });
            Lualy<Microsoft.Xna.Framework.Vector3>(new string[] { "Vector3" });
            Lualy<Microsoft.Xna.Framework.Vector4>(new string[] { "Vector4" });
            Lualy<Microsoft.Xna.Framework.Color>(new string[] { "Color" });
            Lualy<Microsoft.Xna.Framework.Point>(new string[] { "Point" });
            Lualy<Microsoft.Xna.Framework.Rectangle>(new string[] { "Rectangle" });

#if CLIENT
            Lualy<Barotrauma.WaterRenderer>();
            Lualy<Barotrauma.WaterVertexData>();
#endif

            Lualy<Barotrauma.LuaSByte>(new string[] { "SByte" });
            Lualy<Barotrauma.LuaByte>(new string[] { "Byte" });
            Lualy<Barotrauma.LuaInt16>(new string[] { "Int16" }, new string[] { "Short" });
            Lualy<Barotrauma.LuaUInt16>(new string[] { "UInt16" }, new string[] { "UShort" });
            Lualy<Barotrauma.LuaInt32>(new string[] { "Int32" });
            Lualy<Barotrauma.LuaUInt32>(new string[] { "UInt32" });
            Lualy<Barotrauma.LuaInt64>(new string[] { "Int64" });
            Lualy<Barotrauma.LuaUInt64>(new string[] { "UInt64" });
            Lualy<Barotrauma.LuaSingle>(new string[] { "Single" }, new string[] { "Float" });
            Lualy<Barotrauma.LuaDouble>(new string[] { "Double" });

            LualyBase(typeof(LuaCsLogger), new string[] { "Logger" });
            LualyBase(typeof(LuaUserData), new string[] { "LuaUserData" });
            LualyBase(typeof(LuaGame), new string[] { "Game" });
            Lualy<Barotrauma.LuaCsPatch>();
            Lualy<Barotrauma.LuaCsAction>();
            Lualy<Barotrauma.LuaCsFunc>();
            Lualy<Barotrauma.LuaCsPatchFunc>();
            LualyBase(typeof(LuaCsHook), new string[] { "Hook" });
            LualyBase(typeof(LuaCsHook.HookMethodType), new string[] { "Hook", "HookMethodType" });
            LualyBase(typeof(LuaCsHook.ParameterTable), new string[] { "Hook", "ParameterTable" });
            LualyBase(typeof(LuaCsTimer), new string[] { "Timer" });
            LualyBase(typeof(LuaCsFile), new string[] { "File" });
            LualyBase(typeof(LuaCsNetworking), new string[] { "Networking" });
            LualyBase(typeof(LuaCsSteam), new string[] { "Steam" });
            LualyBase(typeof(LuaCsPerformanceCounter), new string[] { "PerformanceCounter" });
            LualyBase(typeof(LuaCsConfig), new string[] { "LuaCsConfig" });
            LualyBase(typeof(LuaCsSetup.LuaCsModStore), new string[] { "ModStore" });
            LualyBase(typeof(LuaCsSetup.LuaCsModStore.CsModStore), new string[] { "ModStore", "CsModStore" });
            LualyBase(typeof(LuaCsSetup.LuaCsModStore.LuaModStore), new string[] { "ModStore", "LuaModStore" });
            LualyBase(typeof(Barotrauma.AssemblyManager), new string[] { "AssemblyManager" });
            LualyBase(typeof(Barotrauma.CsPackageManager), new string[] { "PluginPackageManager" });
            Lualy<MoonSharp.Interpreter.Interop.IUserDataDescriptor>();

            LualyBase(typeof(Barotrauma.ToolBox));
            LualyBase(typeof(FarseerPhysics.ConvertUnits), new string[] { "ConvertUnits" });

            LualyBase(typeof(Barotrauma.Rand));
            Lualy<Barotrauma.Rand.RandSync>(null, new string[] { "RandSync" });

            Lualy<Steamworks.SteamServer>();
            Lualy<Steamworks.Friend>();
            Lualy<Steamworks.Ugc.Item>();
            Lualy<Barotrauma.SteamWorkshopId>();

            Lualy<Barotrauma.Option<Barotrauma.Character>>();
            Lualy<Barotrauma.Option<Barotrauma.Networking.AccountId>>();
            Lualy<Barotrauma.Option<Barotrauma.Networking.SteamId>>();
            Lualy<Barotrauma.Option<Barotrauma.Networking.EpicAccountId>>();
            Lualy<Barotrauma.Option<Barotrauma.ContentPackageId>>();
            Lualy<Barotrauma.Option<System.DateTime>>();
            Lualy<Barotrauma.Option<Barotrauma.Networking.BannedPlayer>>();

            LualyBase(typeof(Barotrauma.MathUtils));
            LualyBase(typeof(System.Math));
            LualyBase(typeof(System.MathF));
            LualyBase(typeof(Microsoft.Xna.Framework.MathHelper));

            Lualy<Barotrauma.PerformanceCounter>();

            Lualy<Barotrauma.GameMain>();

#if CLIENT
            Lualy<Barotrauma.SerializableEntityEditor>();
#endif
            Lualy<Barotrauma.SerializableProperty>();

            Lualy<Barotrauma.Range<System.Single>>();
            Lualy<Barotrauma.Range<System.Int32>>();

            #region String
            Lualy<AddedPunctuationLString>();
            Lualy<CapitalizeLString>();
            Lualy<ConcatLString>();
            Lualy<FallbackLString>();
            Lualy<FormattedLString>();
            Lualy<InputTypeLString>();
            Lualy<JoinLString>();
            Lualy<LocalizedString>();
            Lualy<LowerLString>();
            Lualy<RawLString>();
            Lualy<ReplaceLString>();
            Lualy<ServerMsgLString>();
            Lualy<SplitLString>();
            Lualy<TagLString>();
            Lualy<TrimLString>();
            Lualy<UpperLString>();

            Lualy<RichString>();
            Lualy<StripRichTagsLString>();
            Lualy<RichTextData>();

            LualyBase(typeof(TextManager));
            Lualy<TextPack>();

            Lualy<Identifier>();
            Lualy<LanguageIdentifier>();
            #endregion

            Lualy<Barotrauma.Networking.AccountInfo>();
            Lualy<Barotrauma.Networking.AccountId>();
            Lualy<Barotrauma.Networking.SteamId>();
            Lualy<Barotrauma.Networking.EpicAccountId>();
            Lualy<System.Net.IPAddress>(new string[] { "IPAddress" });
            Lualy<Barotrauma.Networking.Address>();
            Lualy<Barotrauma.Networking.LidgrenAddress>();
            Lualy<Barotrauma.Networking.SteamP2PAddress>();
            Lualy<Barotrauma.Networking.PipeAddress>();
            Lualy<Barotrauma.Networking.UnknownAddress>();
            Lualy<Barotrauma.Networking.P2PAddress>();
            Lualy<Barotrauma.Networking.EosP2PAddress>();
            Lualy<Barotrauma.Networking.Endpoint>();
            Lualy<Barotrauma.Networking.LidgrenEndpoint>();
            Lualy<Barotrauma.Networking.SteamP2PEndpoint>();
            Lualy<Barotrauma.Networking.PipeEndpoint>();

            Lualy<Barotrauma.Md5Hash>();
            Lualy<Barotrauma.ContentFile>();
            Lualy<Barotrauma.ContentPackage>();
            Lualy<Barotrauma.ContentPackageId>();
            LualyBase(typeof(Barotrauma.ContentPackageManager));
            Lualy<Barotrauma.ContentPackageManager.PackageSource>();
            LualyBase(typeof(Barotrauma.ContentPackageManager.EnabledPackages));
            Lualy<Barotrauma.RegularPackage>();
            Lualy<Barotrauma.CorePackage>();
            Lualy<Barotrauma.ContentXElement>();
            Lualy<Barotrauma.ContentPath>();
            Lualy<Barotrauma.Success<Barotrauma.ContentPackage, System.Exception>>();

            #region Files
            Lualy<Barotrauma.AfflictionsFile>();
            Lualy<Barotrauma.BackgroundCreaturePrefabsFile>();
            Lualy<Barotrauma.BallastFloraFile>();
            Lualy<Barotrauma.BeaconStationFile>();
            Lualy<Barotrauma.CaveGenerationParametersFile>();
            Lualy<Barotrauma.CharacterFile>();
            Lualy<Barotrauma.CorpsesFile>();
            Lualy<Barotrauma.DecalsFile>();
            Lualy<Barotrauma.EnemySubmarineFile>();
            Lualy<Barotrauma.EventManagerSettingsFile>();
            Lualy<Barotrauma.FactionsFile>();
            Lualy<Barotrauma.ItemAssemblyFile>();
            Lualy<Barotrauma.ItemFile>();
            Lualy<Barotrauma.JobsFile>();
            Lualy<Barotrauma.LevelGenerationParametersFile>();
            Lualy<Barotrauma.LevelObjectPrefabsFile>();
            Lualy<Barotrauma.LocationTypesFile>();
            Lualy<Barotrauma.MapGenerationParametersFile>();
            Lualy<Barotrauma.MissionsFile>();
            Lualy<Barotrauma.NPCConversationsFile>();
            Lualy<Barotrauma.NPCPersonalityTraitsFile>();
            Lualy<Barotrauma.NPCSetsFile>();
            Lualy<Barotrauma.OrdersFile>();
            Lualy<Barotrauma.OtherFile>();
            Lualy<Barotrauma.OutpostConfigFile>();
            Lualy<Barotrauma.OutpostFile>();
            Lualy<Barotrauma.OutpostModuleFile>();
            Lualy<Barotrauma.ParticlesFile>();
            Lualy<Barotrauma.RandomEventsFile>();
            Lualy<Barotrauma.RuinConfigFile>();
            Lualy<Barotrauma.ServerExecutableFile>();
            Lualy<Barotrauma.SkillSettingsFile>();
            Lualy<Barotrauma.SoundsFile>();
            Lualy<Barotrauma.StartItemsFile>();
            Lualy<Barotrauma.StructureFile>();
            Lualy<Barotrauma.SubmarineFile>();
            Lualy<Barotrauma.TalentsFile>();
            Lualy<Barotrauma.TalentTreesFile>();
            Lualy<Barotrauma.TextFile>();
            Lualy<Barotrauma.TutorialsFile>();
            Lualy<Barotrauma.UIStyleFile>();
            Lualy<Barotrauma.UpgradeModulesFile>();
            Lualy<Barotrauma.WreckAIConfigFile>();
            Lualy<Barotrauma.WreckFile>();
            #endregion

            LualyBase(typeof(Barotrauma.XMLExtensions));
            Lualy<System.Xml.Linq.XElement>(new string[] { nameof(XElement) });
            Lualy<System.Xml.Linq.XName>(new string[] { nameof(XName) });
            Lualy<System.Xml.Linq.XAttribute>(new string[] { nameof(XAttribute) });
            Lualy<System.Xml.Linq.XContainer>(new string[] { nameof(XContainer) });
            Lualy<System.Xml.Linq.XDocument>(new string[] { nameof(XDocument) });
            Lualy<System.Xml.Linq.XNode>(new string[] { nameof(XNode) });

            Lualy<Barotrauma.Camera>();

            Lualy<Barotrauma.CauseOfDeathType>();
            Lualy<Barotrauma.CauseOfDeath>();

            Lualy<Barotrauma.SpawnType>();
            Lualy<Barotrauma.WayPoint>();

            Lualy<Barotrauma.Networking.ServerLog>();
            Lualy<Networking.ServerLog.MessageType>(null, new string[] { "ServerLog_MessageType" }, new string[] { "ServerLogMessageType" });

            Lualy<Barotrauma.PropertyConditional>();
            Lualy<Barotrauma.PropertyConditional.ComparisonOperatorType>();
            Lualy<Barotrauma.PropertyConditional.ConditionType>();
            Lualy<Barotrauma.StatusEffect>();
            Lualy<Barotrauma.StatusEffect.TargetType>();
            Lualy<Barotrauma.StatusEffect.AbilityStatusEffectIdentifier>();
            Lualy<Barotrauma.StatusEffect.CharacterSpawnInfo>();
            Lualy<Barotrauma.StatusEffect.GiveSkill>();
            Lualy<Barotrauma.StatusEffect.GiveTalentInfo>();
            Lualy<Barotrauma.DelayedEffect>();
            Lualy<Barotrauma.DelayedListElement>();
            Lualy<Barotrauma.DurationListElement>();
            LualyBase(typeof(Barotrauma.OutpostGenerator));


            Lualy<Barotrauma.FireSource>();
            Lualy<Barotrauma.DummyFireSource>();

            Lualy<Barotrauma.Explosion>();

            #region Enum
            Lualy<Barotrauma.TransitionMode>();
            Lualy<Barotrauma.ActionType>();
            Lualy<Barotrauma.AbilityEffectType>();
            Lualy<Barotrauma.StatTypes>();
            Lualy<Barotrauma.AbilityFlags>();
            #endregion

            #region Game
            Lualy<Barotrauma.Screen>(new string[] { "GUI", "Screen" });
            Lualy<Barotrauma.GameScreen>();
            Lualy<Barotrauma.NetLobbyScreen>();

            LualyBase(typeof(Barotrauma.GameSettings));
#if CLIENT
            Lualy<Barotrauma.SettingsMenu>();
            Lualy<Barotrauma.TabMenu>();
#endif

            Lualy<GameSession>();

            //Data
            Lualy<Barotrauma.CampaignMetadata>();
            Lualy<Barotrauma.CharacterCampaignData>();
            Lualy<Barotrauma.Faction>();
            Lualy<Barotrauma.FactionPrefab>();
            Lualy<Barotrauma.Reputation>();

            //Game Mode
            Lualy<Barotrauma.GameModePreset>();
            Lualy<Barotrauma.GameMode>();
#if CLIENT
            Lualy<Barotrauma.TestGameMode>();
            Lualy<Barotrauma.TutorialMode>();
#endif
            Lualy<Barotrauma.CampaignMode>();
#if CLIENT
            Lualy<Barotrauma.SinglePlayerCampaign>();
#endif
            Lualy<Barotrauma.MultiPlayerCampaign>();
            Lualy<Barotrauma.CoOpMode>();
            Lualy<Barotrauma.MissionMode>();
            Lualy<Barotrauma.PvPMode>();


            LualyBase(typeof(Barotrauma.AutoItemPlacer));
            Lualy<Barotrauma.CargoManager>();
            Lualy<Barotrauma.CrewManager>();
            Lualy<Barotrauma.SteeringManager>();
            Lualy<Barotrauma.IndoorsSteeringManager>();
            Lualy<Barotrauma.SteeringPath>();
            Lualy<Barotrauma.HireManager>();
            Lualy<Barotrauma.MedicalClinic>();
            Lualy<Barotrauma.ReadyCheck>();

            //Mission
            Lualy<Barotrauma.Mission>();
            Lualy<Barotrauma.MissionPrefab>();
            Lualy<Barotrauma.AbandonedOutpostMission>();
            Lualy<Barotrauma.EliminateTargetsMission>();
            Lualy<Barotrauma.EndMission>();
            Lualy<Barotrauma.BeaconMission>();
            Lualy<Barotrauma.CargoMission>();
            Lualy<Barotrauma.CombatMission>();
            Lualy<Barotrauma.EscortMission>();
            Lualy<Barotrauma.GoToMission>();
            Lualy<Barotrauma.MineralMission>();
            Lualy<Barotrauma.MonsterMission>();
            Lualy<Barotrauma.NestMission>();
            Lualy<Barotrauma.PirateMission>();
            Lualy<Barotrauma.SalvageMission>();
            Lualy<Barotrauma.ScanMission>();
            #endregion


            #region Level
            Lualy<Barotrauma.Level.InterestingPosition>();
            Lualy<Barotrauma.Level.PositionType>(null, new string[] { "PositionType" });
            Lualy<Barotrauma.Level>();
            Lualy<Barotrauma.LevelData>();
            Lualy<Barotrauma.LevelGenerationParams>();
            Lualy<Barotrauma.LevelObjectManager>();
            Lualy<Barotrauma.LevelObjectPrefab>();
            Lualy<Barotrauma.LevelObject>();
            Lualy<Barotrauma.LevelTrigger>();
            Lualy<Barotrauma.LevelWall>();
            Lualy<Barotrauma.DestructibleLevelWall>();
#if CLIENT
            Lualy<Barotrauma.LevelWallVertexBuffer>();
            Lualy<Barotrauma.LevelRenderer>();
#endif
            Lualy<Barotrauma.Biome>();
            Lualy<Barotrauma.Map>();
            Lualy<Barotrauma.Radiation>();
            #endregion


            #region Location
            Lualy<Barotrauma.PriceInfo>();
            Lualy<Barotrauma.LocationType>();
            Lualy<Barotrauma.Location>();
            Lualy<Barotrauma.LocationConnection>();
            Lualy<Barotrauma.LocationTypeChange>();
            #endregion

            #region Event
            Lualy<Barotrauma.EventManager>();
            Lualy<Barotrauma.EventManagerSettings>();
            Lualy<Barotrauma.EventPrefab>();
            Lualy<Barotrauma.EventSet>();
            Lualy<Barotrauma.Event>();
            Lualy<Barotrauma.ArtifactEvent>();
            Lualy<Barotrauma.MonsterEvent>();
            Lualy<Barotrauma.ScriptedEvent>();
            Lualy<Barotrauma.MalfunctionEvent>();
            #endregion

            #region Entity
            Lualy<Barotrauma.Entity>();
            Lualy<Barotrauma.EntitySpawner>();
            Lualy<Barotrauma.EntityGrid>();

            Lualy<Barotrauma.MapEntityCategory>();
            Lualy<Barotrauma.MapEntity>();
            Lualy<Barotrauma.Prefab>();
            Lualy<Barotrauma.PrefabWithUintIdentifier>();
            Lualy<Barotrauma.MapEntityPrefab>();
            Lualy<Barotrauma.CoreEntityPrefab>();

            Lualy<Barotrauma.PrefabCollection<Barotrauma.ItemPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.JobPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.CharacterPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.HumanPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.AfflictionPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.TalentPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.TalentTree>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.OrderPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.LevelGenerationParams>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.LocationType>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.EventPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.EventSet>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.EventManagerSettings>>();

#if CLIENT
            Lualy<Barotrauma.PrefabCollection<Barotrauma.GUIPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.SoundPrefab>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.BackgroundMusic>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.GUISound>>();
            Lualy<Barotrauma.PrefabCollection<Barotrauma.DamageSound>>();
            Lualy<Barotrauma.PrefabSelector<Barotrauma.SoundPrefab>>();

            Lualy<Barotrauma.PrefabCollection<Barotrauma.Particles.ParticlePrefab>>();
#endif

            Lualy<Barotrauma.PrefabSelector<Barotrauma.SkillSettings>>();
            #endregion


            #region Character
            Lualy<Barotrauma.CharacterType>();
            Lualy<Barotrauma.CharacterTeamType>();
            Lualy<Barotrauma.CharacterPrefab>();
            Lualy<Barotrauma.CharacterInfo>();
            Lualy<Barotrauma.CharacterInfo.HeadInfo>();
            Lualy<Barotrauma.CharacterInfo.HeadPreset>();
            Lualy<Barotrauma.CharacterInfoPrefab>();
            Lualy<Barotrauma.Character>();
            Lualy<Barotrauma.AICharacter>();
            Lualy<Barotrauma.CharacterHealth>();
            Lualy<Barotrauma.CharacterHealth.LimbHealth>();
            Lualy<Barotrauma.CharacterInventory>();
            Lualy<Barotrauma.CharacterTalent>();

            Lualy<Barotrauma.CharacterParams>();
            Lualy<Barotrauma.CharacterParams.AIParams>();
            Lualy<Barotrauma.CharacterParams.HealthParams>();
            Lualy<Barotrauma.CharacterParams.InventoryParams>();
            Lualy<Barotrauma.CharacterParams.ParticleParams>();
            Lualy<Barotrauma.CharacterParams.SoundParams>();
            Lualy<Barotrauma.CharacterParams.SubParam>();
            Lualy<Barotrauma.CharacterParams.TargetParams>();

            LualyBase(typeof(Barotrauma.CreatureMetrics));
            Lualy<Barotrauma.MapCreatures.Behavior.BallastFloraBehavior>();
            Lualy<Barotrauma.MapCreatures.Behavior.BallastFloraBranch>();
            Lualy<Barotrauma.PetBehavior>();
            Lualy<Barotrauma.SwarmBehavior>();
            Lualy<Barotrauma.LatchOntoAI>();
            #endregion

            Lualy<Barotrauma.OrderCategory>();
            Lualy<Barotrauma.OrderPrefab>();
            Lualy<Barotrauma.Order>();
            Lualy<Barotrauma.OrderTarget>();

            #region AI
            Lualy<Barotrauma.AIState>();

            Lualy<Barotrauma.AIController>();
            Lualy<Barotrauma.EnemyAIController>();
            Lualy<Barotrauma.HumanAIController>();

            Lualy<Barotrauma.AITarget>();
            Lualy<Barotrauma.AITargetMemory>();

            Lualy<Barotrauma.AIChatMessage>();
            Lualy<Barotrauma.AIObjectiveManager>();

            Lualy<Barotrauma.AIObjective>();
            Lualy<Barotrauma.AIObjectiveChargeBatteries>();
            Lualy<Barotrauma.AIObjectiveCleanupItem>();
            Lualy<Barotrauma.AIObjectiveCleanupItems>();
            Lualy<Barotrauma.AIObjectiveCombat>();
            Lualy<Barotrauma.AIObjectiveContainItem>();
            Lualy<Barotrauma.AIObjectiveDeconstructItem>();
            Lualy<Barotrauma.AIObjectiveDeconstructItems>();
            Lualy<Barotrauma.AIObjectiveEscapeHandcuffs>();
            Lualy<Barotrauma.AIObjectiveExtinguishFire>();
            Lualy<Barotrauma.AIObjectiveExtinguishFires>();
            Lualy<Barotrauma.AIObjectiveFightIntruders>();
            Lualy<Barotrauma.AIObjectiveFindDivingGear>();
            Lualy<Barotrauma.AIObjectiveFindSafety>();
            Lualy<Barotrauma.AIObjectiveFixLeak>();
            Lualy<Barotrauma.AIObjectiveFixLeaks>();
            Lualy<Barotrauma.AIObjectiveGetItem>();
            Lualy<Barotrauma.AIObjectiveGetItems>();
            Lualy<Barotrauma.AIObjectiveGoTo>();
            Lualy<Barotrauma.AIObjectiveIdle>();
            Lualy<Barotrauma.AIObjectiveOperateItem>();
            Lualy<Barotrauma.AIObjectivePrepare>();
            Lualy<Barotrauma.AIObjectivePumpWater>();
            Lualy<Barotrauma.AIObjectiveRepairItem>();
            Lualy<Barotrauma.AIObjectiveRepairItems>();
            Lualy<Barotrauma.AIObjectiveRescue>();
            Lualy<Barotrauma.AIObjectiveRescueAll>();
            Lualy<Barotrauma.AIObjectiveReturn>();
            Lualy<AIObjectiveCombat.CombatMode>(null, new string[] { "CombatMode" });
            #endregion


            #region Ragdoll
            Lualy<Barotrauma.LimbType>();
            Lualy<Barotrauma.Limb>();
            Lualy<Barotrauma.LimbJoint>();
            Lualy<Barotrauma.Items.Components.LimbPos>(new string[] { "Components", "LimbPos" });

            Lualy<Barotrauma.Ragdoll>();
            Lualy<Barotrauma.AnimController>();
            Lualy<Barotrauma.FishAnimController>();
            Lualy<Barotrauma.HumanoidAnimController>();

            Lualy<Barotrauma.EditableParams>();
            Lualy<Barotrauma.RagdollParams>();
            Lualy<Barotrauma.AnimationParams>();

            Lualy<Barotrauma.SwimParams>();
            Lualy<Barotrauma.GroundedMovementParams>();

            Lualy<Barotrauma.HumanRagdollParams>();
            Lualy<Barotrauma.HumanGroundedParams>();
            Lualy<Barotrauma.HumanWalkParams>();
            Lualy<Barotrauma.HumanRunParams>();
            Lualy<Barotrauma.HumanCrouchParams>();
            Lualy<Barotrauma.HumanSwimParams>();
            Lualy<Barotrauma.HumanSwimFastParams>();
            Lualy<Barotrauma.HumanSwimSlowParams>();

            Lualy<Barotrauma.FishRagdollParams>();
            Lualy<Barotrauma.FishWalkParams>();
            Lualy<Barotrauma.FishGroundedParams>();
            Lualy<Barotrauma.FishRunParams>();
            Lualy<Barotrauma.FishSwimParams>();
            Lualy<Barotrauma.FishSwimFastParams>();
            Lualy<Barotrauma.FishSwimSlowParams>();

            #endregion


            #region Skill
            Lualy<Barotrauma.Skill>();
            Lualy<Barotrauma.SkillPrefab>();
            Lualy<Barotrauma.SkillSettings>();
            #endregion


            #region Job
            Lualy<Barotrauma.Job>();
            Lualy<Barotrauma.JobPrefab>();
            Lualy<Barotrauma.JobVariant>();
            #endregion

            #region Decal
            Lualy<Barotrauma.Decal>();
            LualyBase(typeof(Barotrauma.DecalManager));
            Lualy<Barotrauma.DecalPrefab>();
            #endregion


            #region Talent
            Lualy<Barotrauma.TalentPrefab>();
            Lualy<Barotrauma.TalentOption>();
            Lualy<Barotrauma.TalentSubTree>();
            Lualy<Barotrauma.TalentTree>();
            #endregion


            #region Item
            Lualy<Barotrauma.ItemPrefab>();
            Lualy<Barotrauma.ItemAssemblyPrefab>();
            Lualy<Barotrauma.Item>();
            Lualy<Barotrauma.ItemInventory>();
            Lualy<Barotrauma.RelatedItem>();
            #endregion


            #region Items.Components
            Lualy<Barotrauma.Items.Components.Holdable>(new string[] { "Components", "Holdable" });
            Lualy<Barotrauma.Items.Components.IdCard>(new string[] { "Components", "IdCard" });
            Lualy<Barotrauma.Items.Components.LevelResource>(new string[] { "Components", "LevelResource" });
            Lualy<Barotrauma.Items.Components.MeleeWeapon>(new string[] { "Components", "MeleeWeapon" });
            Lualy<Barotrauma.Items.Components.Pickable>(new string[] { "Components", "Pickable" });
            Lualy<Barotrauma.Items.Components.Propulsion>(new string[] { "Components", "Propulsion" });
            Lualy<Barotrauma.Items.Components.RangedWeapon>(new string[] { "Components", "RangedWeapon" });
            Lualy<Barotrauma.Items.Components.RepairTool>(new string[] { "Components", "RepairTool" });
            Lualy<Barotrauma.Items.Components.Sprayer>(new string[] { "Components", "Sprayer" });
            Lualy<Barotrauma.Items.Components.Throwable>(new string[] { "Components", "Throwable" });

            Lualy<Barotrauma.Items.Components.Controller>(new string[] { "Components", "Controller" });
            Lualy<Barotrauma.Items.Components.Deconstructor>(new string[] { "Components", "Deconstructor" });
            Lualy<Barotrauma.Items.Components.Engine>(new string[] { "Components", "Engine" });
            Lualy<Barotrauma.Items.Components.Fabricator>(new string[] { "Components", "Fabricator" });
            Lualy<Barotrauma.Items.Components.MiniMap>(new string[] { "Components", "MiniMap" });
            Lualy<Barotrauma.Items.Components.OutpostTerminal>(new string[] { "Components", "OutpostTerminal" });
            Lualy<Barotrauma.Items.Components.OxygenGenerator>(new string[] { "Components", "OxygenGenerator" });
            Lualy<Barotrauma.Items.Components.Pump>(new string[] { "Components", "Pump" });
            Lualy<Barotrauma.Items.Components.Reactor>(new string[] { "Components", "Reactor" });
            Lualy<Barotrauma.Items.Components.Sonar>(new string[] { "Components", "Sonar" });
            Lualy<Barotrauma.Items.Components.SonarTransducer>(new string[] { "Components", "SonarTransducer" });
            Lualy<Barotrauma.Items.Components.Steering>(new string[] { "Components", "Steering" });
            Lualy<Barotrauma.Items.Components.Vent>(new string[] { "Components", "Vent" });

            Lualy<Barotrauma.Items.Components.Powered>(new string[] { "Components", "Powered" });
            Lualy<Barotrauma.Items.Components.PowerContainer>(new string[] { "Components", "PowerContainer" });
            Lualy<Barotrauma.Items.Components.PowerTransfer>(new string[] { "Components", "PowerTransfer" });

            Lualy<Barotrauma.Items.Components.AdderComponent>(new string[] { "Components", "AdderComponent" });
            Lualy<Barotrauma.Items.Components.AndComponent>(new string[] { "Components", "AndComponent" });
            Lualy<Barotrauma.Items.Components.ArithmeticComponent>(new string[] { "Components", "ArithmeticComponent" });
            Lualy<Barotrauma.Items.Components.ButtonTerminal>(new string[] { "Components", "ButtonTerminal" });
            Lualy<Barotrauma.Items.Components.ColorComponent>(new string[] { "Components", "ColorComponent" });
            Lualy<Barotrauma.Items.Components.ConcatComponent>(new string[] { "Components", "ConcatComponent" });
            Lualy<Barotrauma.Items.Components.Connection>(new string[] { "Components", "Connection" });
            Lualy<Barotrauma.Items.Components.ConnectionPanel>(new string[] { "Components", "ConnectionPanel" });
            Lualy<Barotrauma.Items.Components.CustomInterface>(new string[] { "Components", "CustomInterface" });
            Lualy<Barotrauma.Items.Components.DelayComponent>(new string[] { "Components", "DelayComponent" });
            Lualy<Barotrauma.Items.Components.DivideComponent>(new string[] { "Components", "DivideComponent" });
            Lualy<Barotrauma.Items.Components.EqualsComponent>(new string[] { "Components", "EqualsComponent" });
            Lualy<Barotrauma.Items.Components.ExponentiationComponent>(new string[] { "Components", "ExponentiationComponent" });
            Lualy<Barotrauma.Items.Components.FunctionComponent>(new string[] { "Components", "FunctionComponent" });
            Lualy<Barotrauma.Items.Components.GreaterComponent>(new string[] { "Components", "GreaterComponent" });
            Lualy<Barotrauma.Items.Components.LightComponent>(new string[] { "Components", "LightComponent" });
            Lualy<Barotrauma.Items.Components.MemoryComponent>(new string[] { "Components", "MemoryComponent" });
            Lualy<Barotrauma.Items.Components.ModuloComponent>(new string[] { "Components", "ModuloComponent" });
            Lualy<Barotrauma.Items.Components.MotionSensor>(new string[] { "Components", "MotionSensor" });
            Lualy<Barotrauma.Items.Components.MultiplyComponent>(new string[] { "Components", "MultiplyComponent" });
            Lualy<Barotrauma.Items.Components.NotComponent>(new string[] { "Components", "NotComponent" });
            Lualy<Barotrauma.Items.Components.OrComponent>(new string[] { "Components", "OrComponent" });
            Lualy<Barotrauma.Items.Components.OscillatorComponent>(new string[] { "Components", "OscillatorComponent" });
            Lualy<Barotrauma.Items.Components.OxygenDetector>(new string[] { "Components", "OxygenDetector" });
            Lualy<Barotrauma.Items.Components.RegExFindComponent>(new string[] { "Components", "RegExFindComponent" });
            Lualy<Barotrauma.Items.Components.RelayComponent>(new string[] { "Components", "RelayComponent" });
            Lualy<Barotrauma.Items.Components.Signal>();
            Lualy<Barotrauma.Items.Components.SignalCheckComponent>(new string[] { "Components", "SignalCheckComponent" });
            Lualy<Barotrauma.Items.Components.SmokeDetector>(new string[] { "Components", "SmokeDetector" });
            Lualy<Barotrauma.Items.Components.StringComponent>(new string[] { "Components", "StringComponent" });
            Lualy<Barotrauma.Items.Components.SubtractComponent>(new string[] { "Components", "SubtractComponent" });
            Lualy<Barotrauma.Items.Components.Terminal>(new string[] { "Components", "Terminal" });
            Lualy<Barotrauma.Items.Components.TrigonometricFunctionComponent>(new string[] { "Components", "TrigonometricFunctionComponent" });
            Lualy<Barotrauma.Items.Components.WaterDetector>(new string[] { "Components", "WaterDetector" });
            Lualy<Barotrauma.Items.Components.WifiComponent>(new string[] { "Components", "WifiComponent" });
            Lualy<Barotrauma.Items.Components.Wire>(new string[] { "Components", "Wire" });
            Lualy<Barotrauma.Items.Components.XorComponent>(new string[] { "Components", "XorComponent" });

            Lualy<Barotrauma.Items.Components.DockingPort>(new string[] { "Components", "DockingPort" });
            Lualy<Barotrauma.Items.Components.Door>(new string[] { "Components", "Door" });
            Lualy<Barotrauma.Items.Components.ElectricalDischarger>(new string[] { "Components", "ElectricalDischarger" });
            Lualy<Barotrauma.Items.Components.EntitySpawnerComponent>(new string[] { "Components", "EntitySpawnerComponent" });
            Lualy<Barotrauma.Items.Components.GeneticMaterial>(new string[] { "Components", "GeneticMaterial" });
            Lualy<Barotrauma.Items.Components.Growable>(new string[] { "Components", "Growable" });
            Lualy<Barotrauma.Items.Components.ProducedItem>(new string[] { "Components", "ProducedItem" });
            Lualy<Barotrauma.Items.Components.VineTile>(new string[] { "Components", "VineTile" });
            LualyBase(typeof(Barotrauma.Items.Components.GrowthSideExtension));
            Lualy<Barotrauma.Items.Components.ItemComponent>(new string[] { "Components", "ItemComponent" });
            Lualy<Barotrauma.Items.Components.ItemContainer>(new string[] { "Components", "ItemContainer" });
            Lualy<Barotrauma.Items.Components.ItemLabel>(new string[] { "Components", "ItemLabel" });
            Lualy<Barotrauma.Items.Components.Ladder>(new string[] { "Components", "Ladder" });
            Lualy<Barotrauma.Items.Components.NameTag>(new string[] { "Components", "NameTag" });
            Lualy<Barotrauma.Items.Components.Planter>(new string[] { "Components", "Planter" });
            Lualy<Barotrauma.Items.Components.Projectile>(new string[] { "Components", "Projectile" });
            Lualy<Barotrauma.Items.Components.Quality>(new string[] { "Components", "Quality" });
            Lualy<Barotrauma.Items.Components.RemoteController>(new string[] { "Components", "RemoteController" });
            Lualy<Barotrauma.Items.Components.Repairable>(new string[] { "Components", "Repairable" });
            Lualy<Barotrauma.Items.Components.Rope>(new string[] { "Components", "Rope" });
            Lualy<Barotrauma.Items.Components.Scanner>(new string[] { "Components", "Scanner" });
            Lualy<Barotrauma.Items.Components.StatusHUD>(new string[] { "Components", "StatusHUD" });
            Lualy<Barotrauma.Items.Components.TriggerComponent>(new string[] { "Components", "TriggerComponent" });
            Lualy<Barotrauma.Items.Components.Turret>(new string[] { "Components", "Turret" });
            Lualy<Barotrauma.Items.Components.Wearable>(new string[] { "Components", "Wearable" });

            #endregion

            #region Circuit Box
            Lualy<Barotrauma.Items.Components.CircuitBox>();
            Lualy<Barotrauma.CircuitBoxConnection>();
            Lualy<Barotrauma.CircuitBoxComponent>();
            Lualy<Barotrauma.CircuitBoxNode>();
            Lualy<Barotrauma.CircuitBoxWire>();
            Lualy<Barotrauma.CircuitBoxInputOutputNode>();
            Lualy<Barotrauma.CircuitBoxSelectable>();
            LualyBase(typeof(Barotrauma.CircuitBoxSizes));
#if CLIENT
            Lualy<Barotrauma.CircuitBoxWireRenderer>();
            Lualy<Barotrauma.CircuitBoxLabel>();
            Lualy<Barotrauma.CircuitBoxMouseDragSnapshotHandler>();
            Lualy<Barotrauma.CircuitBoxUI>();
#endif
            #endregion

            #region Submarine
            Lualy<Barotrauma.SubmarineInfo>();
            Lualy<Barotrauma.Submarine>();
            Lualy<Barotrauma.CanEnterSubmarine>();
            Lualy<Barotrauma.SubmarineBody>();
            #endregion

            #region Structure
            Lualy<Barotrauma.WallSection>();
            Lualy<Barotrauma.Structure>();
            Lualy<Barotrauma.StructurePrefab>();
            #endregion

            #region Affliction
            Lualy<Barotrauma.AfflictionPrefab>();
            Lualy<Barotrauma.Affliction>();
            Lualy<Barotrauma.AfflictionPrefabHusk>();
            Lualy<Barotrauma.AfflictionHusk>();
            Lualy<Barotrauma.AfflictionBleeding>();
            Lualy<Barotrauma.AfflictionPsychosis>();
            Lualy<Barotrauma.AfflictionSpaceHerpes>();
            #endregion

            #region Attack
            Lualy<Barotrauma.AttackContext>();
            Lualy<Barotrauma.AttackPattern>();
            Lualy<Barotrauma.AttackTarget>();
            Lualy<Barotrauma.Attack>();
            Lualy<Barotrauma.AttackResult>();
            Lualy<Barotrauma.DamageModifier>();
            #endregion

            #region Inventroy and slot
            Lualy<Barotrauma.InvSlotType>();
#if CLIENT
            Lualy<Barotrauma.InventorySlotItem>();
            Lualy<Barotrauma.VisualSlot>();
#endif
            Lualy<Barotrauma.Inventory>();
            #endregion

            #region Command
            Lualy<Barotrauma.Command>();
#if CLIENT
            Lualy<Barotrauma.TransformCommand>();
            Lualy<Barotrauma.AddOrDeleteCommand>();
            Lualy<Barotrauma.PropertyCommand>();
            Lualy<Barotrauma.InventoryMoveCommand>();
            Lualy<Barotrauma.InventoryPlaceCommand>();
#endif
            #endregion

            #region Traitor
            Lualy<Barotrauma.TraitorEvent>();
            Lualy<Barotrauma.TraitorEventPrefab>();
            Lualy<Barotrauma.TraitorManager>();
            Lualy<Barotrauma.TraitorManager.TraitorResults>();
#if SERVER
            Lualy<Barotrauma.TraitorManager.ActiveTraitorEvent>();
#endif
            #endregion

            #region Physic
            Lualy<FarseerPhysics.Collision.AABB>();
            Lualy<FarseerPhysics.Collision.ContactFeature>();
            Lualy<FarseerPhysics.Collision.ManifoldPoint>();
            Lualy<FarseerPhysics.Collision.ContactID>();
            Lualy<FarseerPhysics.Collision.Manifold>();
            Lualy<FarseerPhysics.Collision.RayCastInput>();
            Lualy<FarseerPhysics.Collision.ClipVertex>();
            Lualy<FarseerPhysics.Collision.RayCastOutput>();
            Lualy<FarseerPhysics.Collision.EPAxis>();
            Lualy<FarseerPhysics.Collision.ReferenceFace>();
            LualyBase(typeof(FarseerPhysics.Collision.Collision));

            Lualy<FarseerPhysics.Dynamics.World>();
            Lualy<FarseerPhysics.Dynamics.Fixture>();
            LualyBase(typeof(Barotrauma.Physics));
            Lualy<Barotrauma.PhysicsBody>();

            Lualy<Barotrauma.Hull>();
            Lualy<Barotrauma.Gap>();
            #endregion

            #region Voronoi2
            Lualy<Voronoi2.DoubleVector2>();
            Lualy<Voronoi2.Site>();
            Lualy<Voronoi2.Edge>();
            Lualy<Voronoi2.Halfedge>();
            Lualy<Voronoi2.VoronoiCell>();
            Lualy<Voronoi2.GraphEdge>();
            #endregion

            #region Sprite
            Lualy<Barotrauma.Sprite>();
            Lualy<Barotrauma.SpriteSheet>();
            Lualy<Barotrauma.ConditionalSprite>();
            Lualy<Barotrauma.WearableType>();
            Lualy<Barotrauma.WearableSprite>();
            Lualy<Barotrauma.DeformableSprite>();

#if CLIENT
            LualyBase(typeof(Barotrauma.TextureLoader));
            Lualy<Barotrauma.SpriteRecorder>();
            Lualy<Barotrauma.DecorativeSprite>();
            Lualy<Barotrauma.BrokenItemSprite>();
            Lualy<Barotrauma.ContainedItemSprite>();
            Lualy<Barotrauma.Items.Components.VineSprite>(new string[] { "Components", "VineSprite" });
#endif
            #endregion

            #region Craft
            Lualy<Barotrauma.DeconstructItem>();
            Lualy<Barotrauma.PreferredContainer>();
            Lualy<Barotrauma.SwappableItem>();
            Lualy<Barotrauma.FabricationRecipe>();
            Lualy<Barotrauma.FabricationRecipe.RequiredItemByIdentifier>();
            Lualy<Barotrauma.FabricationRecipe.RequiredItemByTag>();
            Lualy<Barotrauma.FabricationRecipe.RequiredItem>();
            Lualy<Barotrauma.PurchasedItem>();
            Lualy<Barotrauma.PurchasedItemSwap>();
            Lualy<Barotrauma.SoldItem>();
            Lualy<Barotrauma.StartItem>();
            Lualy<Barotrauma.StartItemSet>();
            #endregion

            #region Upgrade
            Lualy<Barotrauma.UpgradeCategory>();
            Lualy<Barotrauma.UpgradePrice>();
            Lualy<Barotrauma.UpgradeManager>();
            Lualy<Barotrauma.UpgradePrefab>();
            Lualy<Barotrauma.Upgrade>();
            Lualy<Barotrauma.PurchasedUpgrade>();
            #endregion

            #region Networking
            Lualy<Barotrauma.Item.EventType>();
            Lualy<Barotrauma.Item.ComponentStateEventData>();
            Lualy<Barotrauma.Item.InventoryStateEventData>();
            Lualy<Barotrauma.Item.ChangePropertyEventData>();
            Lualy<Barotrauma.Item.ApplyStatusEffectEventData>();

            Lualy<Barotrauma.Character.EventType>();
            Lualy<Barotrauma.Character.InventoryStateEventData>();
            Lualy<Barotrauma.Character.ControlEventData>();
            Lualy<Barotrauma.Character.CharacterStatusEventData>();
            Lualy<Barotrauma.Character.TreatmentEventData>();
            Lualy<Barotrauma.Character.SetAttackTargetEventData>();
            Lualy<Barotrauma.Character.ExecuteAttackEventData>();
            Lualy<Barotrauma.Character.AssignCampaignInteractionEventData>();
            Lualy<Barotrauma.Character.ObjectiveManagerStateEventData>();
            Lualy<Barotrauma.Character.AddToCrewEventData>();
            Lualy<Barotrauma.Character.UpdateExperienceEventData>();
            Lualy<Barotrauma.Character.UpdatePermanentStatsEventData>();
            Lualy<Barotrauma.Character.UpdateSkillsEventData>();
            Lualy<Barotrauma.Character.UpdateTalentsEventData>();

            LualyBase(typeof(Barotrauma.Networking.NetConfig));
            Lualy<Barotrauma.Networking.ServerSettings>();

            Lualy<Barotrauma.Networking.ChatMessageType>();
            Lualy<Barotrauma.Networking.ChatMessage>();

            Lualy<Barotrauma.Networking.PacketHeader>();
            Lualy<Barotrauma.Networking.ServerPacketHeader>();
            Lualy<Barotrauma.Networking.ClientPacketHeader>();
            Lualy<Barotrauma.Networking.DeliveryMethod>();
            Lualy<Barotrauma.Networking.ConnectionInitialization>();
            Lualy<Barotrauma.Networking.IWriteMessage>();
            Lualy<Barotrauma.Networking.WriteOnlyMessage>();
            Lualy<Barotrauma.Networking.IReadMessage>();
            Lualy<Barotrauma.Networking.ReadOnlyMessage>();
            Lualy<Barotrauma.Networking.ReadWriteMessage>();

            Lualy<Barotrauma.Networking.ClientPermissions>();
            Lualy<Barotrauma.Networking.Client>();
            Lualy<Barotrauma.Networking.TempClient>();
            Lualy<Barotrauma.INetSerializableStruct>();

            Lualy<Barotrauma.Networking.NetworkConnectionStatus>();
            Lualy<Barotrauma.Networking.NetworkConnection>();
            Lualy<Barotrauma.Networking.PipeConnection>();
            Lualy<Barotrauma.Networking.LidgrenConnection>();
            Lualy<Barotrauma.Networking.SteamP2PConnection>();

            Lualy<Barotrauma.Networking.NetworkMember>();

            Lualy<Barotrauma.Networking.BanList>();
            Lualy<Barotrauma.Networking.BannedPlayer>();

#if SERVER
            Lualy<Barotrauma.Networking.GameServer>();

            Lualy<Barotrauma.Networking.ServerPeer>();
            Lualy<Barotrauma.Networking.LidgrenServerPeer>();
            Lualy<Barotrauma.Networking.P2PServerPeer>();
            Lualy<Barotrauma.Networking.FileSender>();
#endif

#if CLIENT
            Lualy<Barotrauma.Networking.GameClient>();

            Lualy<Barotrauma.Networking.ClientPeer>();
            Lualy<Barotrauma.Networking.LidgrenClientPeer>();
#endif
            #endregion

            #region Keys
            Lualy<Barotrauma.InputType>();
            Lualy<Barotrauma.Key>();
#if CLIENT
            Lualy<EventInput.KeyboardDispatcher>();
            Lualy<EventInput.KeyEventArgs>();
            LualyBase(typeof(Microsoft.Xna.Framework.Input.Keys), new string[] { "Keys" });
#endif
            #endregion

            #region Particles
#if CLIENT
            Lualy<Barotrauma.Particles.ParticleManager>();
            Lualy<Barotrauma.Particles.Particle>();
            Lualy<Barotrauma.Particles.ParticleEmitterProperties>();
            Lualy<Barotrauma.Particles.ParticleEmitter>();
            Lualy<Barotrauma.Particles.ParticlePrefab>();
#endif
            #endregion

            #region GUI
            Lualy<Barotrauma.NumberType>();

#if CLIENT
            Lualy<Barotrauma.ScalableFont>();

            Lualy<Barotrauma.Anchor>(new string[] { "GUI", "Anchor" });
            Lualy<Barotrauma.Pivot>(new string[] { "GUI", "Pivot" });
            Lualy<Barotrauma.ScaleBasis>();

            Lualy<Barotrauma.ChatBox>();
            LualyBase(typeof(Barotrauma.FileSelection));
            Lualy<Barotrauma.Graph>();
            Lualy<Barotrauma.HRManagerUI>();

            LualyBase(typeof(Barotrauma.GUI), new string[] { "GUI", "GUI" });
            Lualy<Barotrauma.GUIPrefab>();
            Lualy<Barotrauma.GUIComponentStyle>();

            Lualy<Barotrauma.SpriteFallBackState>();

            Lualy<Barotrauma.GUISoundType>(new string[] { "GUI", "SoundType" });
            Lualy<Barotrauma.CursorState>(new string[] { "GUI", "CursorState" });

            Lualy<Barotrauma.PlayerInput>();

            Lualy<Barotrauma.GUIFont>();
            Lualy<Barotrauma.GUIFontPrefab>();

            Lualy<Barotrauma.GUISprite>();
            Lualy<Barotrauma.GUISpritePrefab>();
            Lualy<Barotrauma.GUISpriteSheet>();
            Lualy<Barotrauma.GUISpriteSheetPrefab>();
            Lualy<Barotrauma.GUICursor>();
            Lualy<Barotrauma.GUICursorPrefab>();

            Lualy<Barotrauma.GUIButton>(new string[] { "GUI", "Button" });
            Lualy<Barotrauma.GUICanvas>(new string[] { "GUI", "Canvas" });
            Lualy<Barotrauma.GUIColor>();
            Lualy<Barotrauma.GUIColorPrefab>();
            Lualy<Barotrauma.GUIColorPicker>(new string[] { "GUI", "ColorPicker" });
            Lualy<Barotrauma.GUIComponent>(new string[] { "GUI", "Component" });
            Lualy<Barotrauma.GUIComponent.ComponentState>();
            Lualy<Barotrauma.GUIContextMenu>(new string[] { "GUI", "ContextMenu" });
            Lualy<Barotrauma.ContextMenuOption>(new string[] { "GUI", "ContextMenuOption" });
            Lualy<Barotrauma.GUICustomComponent>(new string[] { "GUI", "CustomComponent" });
            Lualy<Barotrauma.GUIDropDown>(new string[] { "GUI", "DropDown" });
            Lualy<Barotrauma.GUIFrame>(new string[] { "GUI", "Frame" });
            Lualy<Barotrauma.GUIImage>(new string[] { "GUI", "Image" });
            Lualy<Barotrauma.GUILayoutGroup>(new string[] { "GUI", "LayoutGroup" });
            Lualy<Barotrauma.GUIListBox>(new string[] { "GUI", "ListBox" });
            Lualy<Barotrauma.GUIMessage>();
            Lualy<Barotrauma.GUIMessageBox>(new string[] { "GUI", "MessageBox" });
            Lualy<Barotrauma.GUINumberInput>(new string[] { "GUI", "NumberInput" });

            Lualy<Barotrauma.GUIProgressBar>(new string[] { "GUI", "ProgressBar" });
            Lualy<Barotrauma.GUIRadioButtonGroup>(new string[] { "GUI", "RadioButtonGroup" }); // NON-EXIST
            Lualy<Barotrauma.GUIDragHandle>(new string[] { "GUI", "DragHandle" });
            Lualy<Barotrauma.GUIScissorComponent>(new string[] { "GUI", "ScissorComponent" });
            Lualy<Barotrauma.GUIScrollBar>(new string[] { "GUI", "ScrollBar" });
            LualyBase(typeof(Barotrauma.GUIStyle), new string[] { "GUI", "GUIStyle" }, new string[] { "GUI", "Style" });
            Lualy<Barotrauma.GUITextBlock>(new string[] { "GUI", "TextBlock" });
            Lualy<Barotrauma.GUITextBox>(new string[] { "GUI", "TextBox" });
            Lualy<Barotrauma.GUITickBox>(new string[] { "GUI", "TickBox" });
            Lualy<Barotrauma.Graph>(new string[] { "GUI", "Graph" });
            LualyBase(typeof(Barotrauma.HUD));
            LualyBase(typeof(Barotrauma.HUDLayoutSettings));
            LualyBase(typeof(Barotrauma.HUDProgressBar));
            Lualy<Barotrauma.LoadingScreen>();
            Lualy<Barotrauma.MedicalClinicUI>();
            Lualy<Barotrauma.ParamsEditor>();
            Lualy<Barotrauma.RectTransform>(new string[] { "GUI", "RectTransform" });
            LualyBase(typeof(Barotrauma.ShapeExtensions));
            Lualy<Barotrauma.Store>();
            Lualy<Barotrauma.SubmarineSelection>();
            Lualy<Barotrauma.UISprite>();
            Lualy<Barotrauma.UpgradeStore>();
            Lualy<Barotrauma.CreditsPlayer>(new string[] { "GUI", "CreditsPlayer" });
            Lualy<Barotrauma.SlideshowPlayer>(new string[] { "GUI", "SlideshowPlayer" });
            Lualy<Barotrauma.VideoPlayer>(new string[] { "GUI", "VideoPlayer" });
            Lualy<Barotrauma.VideoPlayer.TextSettings>(new string[] { "GUI", "VideoPlayer", "TextSettings" });
            Lualy<Barotrauma.VideoPlayer.VideoSettings>(new string[] { "GUI", "VideoPlayer", "VideoSettings" });
            Lualy<Barotrauma.VotingInterface>();
            Lualy<Barotrauma.Widget>();
#endif
            Lualy<Barotrauma.Alignment>(new string[] { "GUI", "Alignment" });
            #endregion

            #region Lights & Sounds
            Lualy<Barotrauma.ChatMode>();
            LualyBase(typeof(Barotrauma.Networking.VoipConfig));
            Lualy<Barotrauma.Networking.VoipQueue>();

#if SERVER
            Lualy<Networking.VoipServer>();
#endif

#if CLIENT
            Lualy<Barotrauma.Lights.LightManager>();
            Lualy<Barotrauma.Lights.LightSource>();
            Lualy<Barotrauma.Lights.LightSourceParams>();


            Lualy<Barotrauma.Sounds.SoundManager>();
            Lualy<Barotrauma.Sounds.OggSound>();
            Lualy<Barotrauma.Sounds.VideoSound>();
            Lualy<Barotrauma.Sounds.VoipSound>();
            Lualy<Barotrauma.Networking.VoipClient>();
            Lualy<Barotrauma.Networking.VoipCapture>();
            Lualy<Barotrauma.Sounds.SoundBuffers>();
            Lualy<Barotrauma.Sounds.SoundChannel>();
            Lualy<Barotrauma.RoundSound>();
            Lualy<Barotrauma.Items.Components.ItemSound>(new string[] { "Components", "ItemSound" });

            LualyBase(typeof(Barotrauma.SoundPlayer));
            Lualy<Barotrauma.SoundPrefab>();
            Lualy<Barotrauma.BackgroundMusic>();
            Lualy<Barotrauma.GUISound>();
            Lualy<Barotrauma.DamageSound>();

            Lualy<Barotrauma.Sounds.LowpassFilter>(new string[] { "Sounds", "LowpassFilter" });
            Lualy<Barotrauma.Sounds.HighpassFilter>(new string[] { "Sounds", "HighpassFilter" });
            Lualy<Barotrauma.Sounds.BandpassFilter>(new string[] { "Sounds", "BandpassFilter" });
            Lualy<Barotrauma.Sounds.NotchFilter>(new string[] { "Sounds", "NotchFilter" });
            Lualy<Barotrauma.Sounds.HighShelfFilter>(new string[] { "Sounds", "HighShelfFilter" });
            Lualy<Barotrauma.Sounds.LowShelfFilter>(new string[] { "Sounds", "LowShelfFilter" });
            Lualy<Barotrauma.Sounds.PeakFilter>(new string[] { "Sounds", "PeakFilter" });
#endif
            #endregion

            #region Screen
#if CLIENT
            Lualy<Barotrauma.CampaignEndScreen>();
            Lualy<Barotrauma.EditorScreen>();
            Lualy<Barotrauma.EventEditorScreen>();
            Lualy<Barotrauma.LevelEditorScreen>();
            Lualy<Barotrauma.MainMenuScreen>();
            Lualy<Barotrauma.ParticleEditorScreen>();
            Lualy<Barotrauma.RoundSummaryScreen>();
            Lualy<Barotrauma.ServerListScreen>();
            Lualy<Barotrauma.SpriteEditorScreen>();
            Lualy<Barotrauma.SubEditorScreen>();
            Lualy<Barotrauma.TestScreen>();
            Lualy<Barotrauma.CharacterEditor.CharacterEditorScreen>();
#endif
            #endregion

            Lualy<Barotrauma.KarmaManager>();
            LualyBase(typeof(Barotrauma.Networking.RespawnManager));
            LualyBase(typeof(Barotrauma.Networking.RespawnManager.State));
            LualyBase(typeof(Barotrauma.Networking.RespawnManager.TeamSpecificState));

            Lualy<Barotrauma.Voting>();
            Lualy<Barotrauma.Networking.VoteType>(new string[] { "VoteType" });

            LualyBase(typeof(Barotrauma.DebugConsole));
            Lualy<Barotrauma.DebugConsole.Command>();
        }
    }
}
