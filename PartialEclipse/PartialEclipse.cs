using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Linq;
//using BepInEx.Configuration;
//using RiskOfOptions;
//using RiskOfOptions.Options;
//using System.IO;
using R2API.Networking;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using RoR2.CharacterAI;

using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace PartialEclipse
{
    // Soft dependancy for EclipseArtifacts
    [BepInDependency("Judgy.EclipseArtifacts", BepInDependency.DependencyFlags.SoftDependency)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is for networking
    [BepInDependency(NetworkingAPI.PluginGUID)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class PartialEclipse : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "RegalTurtle";
        public const string PluginName = "PartialEclipse";
        public const string PluginVersion = "1.4.0";

        internal static PartialEclipse Instance { get; private set; }

        // HashSets for who voted for what
        private static readonly HashSet<NetworkUser> votedForEclipse1 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse2 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse3 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse4 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse5 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse6 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse7 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse8 = new();

        public static bool EclipseArtifactsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Judgy.EclipseArtifacts");
        public static bool RiskOfOptionsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        // This gets anything to work at all, used for the check on who voted for what
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);
        // For Eclipse 1
        private static readonly MethodInfo onBodyStart = typeof(CharacterMaster).GetMethod(nameof(CharacterMaster.OnBodyStart), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 2
        private static readonly MethodInfo isBodyInChargingRadius = typeof(HoldoutZoneController).GetMethod(nameof(HoldoutZoneController.IsBodyInChargingRadius), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo doUpdate = typeof(HoldoutZoneController).GetMethod(nameof(HoldoutZoneController.DoUpdate), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo onStartServer = typeof(NetworkUser).GetMethod(nameof(NetworkUser.OnStartServer), BindingFlags.Instance | BindingFlags.Public);
        // For Eclipse 3
        private static readonly MethodInfo onCharacterHitGroundServer = typeof(GlobalEventManager).GetMethod(nameof(GlobalEventManager.OnCharacterHitGroundServer), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 4
        private static readonly MethodInfo recalculateStats = typeof(CharacterBody).GetMethod(nameof(CharacterBody.RecalculateStats), BindingFlags.Public | BindingFlags.Instance);
        // For Eclipse 5
        private static readonly MethodInfo heal = typeof(HealthComponent).GetMethod(nameof(HealthComponent.Heal), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 6
        private static int goldFromEnemy = 0;
        // For Eclipse 7: not needed as it's the same function as E4
        // For Eclipse 8
        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamageProcess), BindingFlags.NonPublic | BindingFlags.Instance);

        // For configs
        //internal static ConfigEntry<bool> HalfTPSize { get; set; }

        // Eclipse 1 modification
        private static void OnBodyStartEclipse1(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse1)
            ))
            {
                Log.Error("Failed to find Eclipse 1 check (OnBodyStart)");
                return;
            }

            behaviorCursor.Index = checkCursor.Index;
            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<HealthComponent>("get_fullHealth"),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul(),
                x => x.MatchCall<HealthComponent>("set_Networkhealth")
            ))
            {
                Log.Error("Failed to find Eclipse 1 behavior (OnBodyStart)");
                return;
            }

            // do the check
            checkCursor.Emit(OpCodes.Ldarg_0); // load 'this' for the CharacterMaster
            checkCursor.EmitDelegate<Func<CharacterMaster, bool>>(selfMaster =>
            {
                return votedForEclipse1.Any(el => el.master == selfMaster);
            });

            // Branch to Eclipse 1 behavior
            // If true, jump to the half-health assignment, else continue vanilla behavior
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        // Eclipse 2 modification
        private static void IsBodyInChargingRadiusEclipse2(ILContext il)
        {
            var loadRadiusCursor = new ILCursor(il);

            // Go to where the chargingRadiusSqr is being locaded onto the stack
            if (!loadRadiusCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdarg(3)
            ))
            {
                Log.Error("Failed to find loading of chargingRadiusSqr (Eclipse 2, IsBodyInChargingRadius)");
                return;
            }

            // Load the CharacterBody
            loadRadiusCursor.Emit(OpCodes.Ldarg_3);
            // Edit the chargingRadiusSqr if need be
            loadRadiusCursor.EmitDelegate<Func<float, CharacterBody, float>>(
                (chargingRadiusSqr, charBody) =>
                {
                    if (votedForEclipse2.Any(el => el.master == charBody.master))
                    {
                        return chargingRadiusSqr / 4f;
                    }
                    return chargingRadiusSqr;
                }
            );
        }

        private static void DoUpdateEclipse2(ILContext il)
        {
            var radiusCursor = new ILCursor(il);

            // Go to right after the currentRadius is multiplied by 2
            if (!radiusCursor.TryGotoNext(
                MoveType.After,
                x => x.MatchLdcR4(2f),
                x => x.MatchLdarg(0),
                x => x.MatchCall<HoldoutZoneController>("get_currentRadius"),
                x => x.MatchMul()
            ))
            {
                Log.Error("Failed to find loadiong of current radius (Eclipse 2, DoUpdate)");
                return;
            }

            // If the user voted for Eclipse 2, half the radius
            radiusCursor.EmitDelegate<Func<float, float>>(
                (visualRadius) =>
                {
                    if (NetworkUser.readOnlyLocalPlayersList.Count == 0)
                    {
                        return visualRadius;
                    }

                    NetworkUser localUser = NetworkUser.readOnlyLocalPlayersList[0];

                    if (votedForEclipse2.Any(el => el == localUser))
                    {
                        return visualRadius / 2f;
                    }
                    return visualRadius;
                }
            );
        }

        // Eclipse 3 modification
        private static void OnCharacterHitGroundServerEclipse3(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse3)
            ))
            {
                Log.Error("Failed to find Eclipse 3 check (OnCharacterHitGroundServer)");
                return;
            }

            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(7),
                x => x.MatchDup(),
                x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.damage)),
                x => x.MatchLdcR4(2f),
                x => x.MatchMul(),
                x => x.MatchStfld<DamageInfo>(nameof(DamageInfo.damage))
            ))
            {
                Log.Error("Failed to find Eclipse 3 behavior (OnCharacterHitGroundServer)");
                return;
            }

            // do the check
            checkCursor.Emit(OpCodes.Ldarg_1);
            checkCursor.EmitDelegate<Func<CharacterBody, bool>>(selfCharacterBody =>
            {
                return votedForEclipse3.Any(el => el.master == selfCharacterBody.master);
            });

            // Branch to Eclipse 3 behavior
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        // Eclipse 4 modification
        private static void RecalculateStatsEclipse4(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse4)
            ))
            {
                Log.Error("Failed to find Eclipse 4 check (RecalculateStats)");
                return;
            }

            behaviorCursor.Index = checkCursor.Index;
            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(135),
                x => x.MatchLdcR4(0.4f),
                x => x.MatchAdd(),
                x => x.MatchStloc(135)
            ))
            {
                Log.Error("Failed to find Eclipse 4 behavior (RecalculateStats)");
                return;
            }

            // Emit checks
            checkCursor.Emit(OpCodes.Ldarg_0);
            checkCursor.EmitDelegate<Func<CharacterBody, bool>>(thisCharBody =>
            {
                BaseAI thisAi = thisCharBody.master?.GetComponent<BaseAI>();
                CharacterBody thisTarget = thisAi?.currentEnemy?.characterBody;

                return thisTarget != null &&
                    votedForEclipse4.Any(el => el && el.master == thisTarget.master);
            });

            // Branch to Eclipse behavior
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        // Eclipse 5 modification
        private static void HealEclipse5(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse1)
            ))
            {
                Log.Error("Failed to find Eclipse 1 check (OnBodyStart)");
                return;
            }

            behaviorCursor.Index = checkCursor.Index;
            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchLdcR4(2),
                x => x.MatchDiv(),
                x => x.MatchStarg(1)
            ))
            {
                Log.Error("Failed to find Eclipse 1 behavior (OnBodyStart)");
                return;
            }

            // Emit check
            checkCursor.Emit(OpCodes.Ldarg_0);
            checkCursor.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent =>
            {
                return votedForEclipse5.Any(el => el.master == selfHealthComponent.body.master);
            });

            // Branch to E5 behavior
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        public static float NewHalfHealing(HealthComponent that, float healAmount)
        {
            if (votedForEclipse5.Any(el => el.master == that.body.master))
            {
                return healAmount / 2f;
            }
            return healAmount;
        }

        // Eclipse 6 modification
        private static void OnKilledServerEclipse6(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                x => x.MatchCallvirt<TeamManager>("GiveTeamMoney") // need to edit this call to store info on the fact that this gold is from enemies
                ))
            {
                Log.Error("Failed to find GiveTeamMoney call (OnKilledServer, Eclipse 6)");
                return;
            }

            c.Remove();
            c.EmitDelegate<Action<TeamManager, TeamIndex, uint, bool>>(
                (manager, teamIndex, money, bypassStatManager) =>
                {
                    goldFromEnemy++;
                    try
                    {
                        manager.GiveTeamMoney(teamIndex, money, bypassStatManager);
                    }
                    finally
                    {
                        goldFromEnemy--;
                    }
                }
            );
        }

        private static void GiveTeamMoneyEclipse6(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                MoveType.Before,
                x => x.MatchCallvirt<CharacterMaster>("GiveMoney")
            ))
            {
                Log.Error("Failed to find GiveMoney call (Eclipse 6)");
                return;
            }

            c.Remove();
            c.EmitDelegate<Action<CharacterMaster, uint, bool>>(
                (master, money, bypassStatManager) =>
                {
                    if (goldFromEnemy > 0 && votedForEclipse6.Any(el => el.master == master))
                    {
                        money = (uint)(money * 0.8f);
                    }

                    master.GiveMoney(money, bypassStatManager);
                }
            );
        }

        // Eclipse 7 modification
        private static void RecalculateStatsEclipse7(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse7)
            ))
            {
                Log.Error("Failed to find Eclipse 7 check (RecalculateStats)");
                return;
            }

            behaviorCursor.Index = checkCursor.Index;
            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(161),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul(),
                x => x.MatchStloc(161)
            ))
            {
                Log.Error("Failed to find Eclipse 7 behavior (RecalculateStats)");
                return;
            }

            // Emit check
            checkCursor.Emit(OpCodes.Ldarg_0);
            checkCursor.EmitDelegate<Func<CharacterBody, bool>>(thisCharBody =>
            {
                BaseAI thisAi = thisCharBody.master?.GetComponent<BaseAI>();
                CharacterBody thisTarget = thisAi?.currentEnemy?.characterBody;

                return thisTarget != null &&
                    votedForEclipse7.Any(el => el && el.master && el.master.GetBody() == thisTarget);
            });

            // Emit jump
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        // Eclipse 8 modification
        private static void TakeDamageProcessEclipse8(ILContext il)
        {
            var checkCursor = new ILCursor(il);
            var behaviorCursor = new ILCursor(il);

            if (!checkCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchCall<Run>("get_instance"),
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse8)
            ))
            {
                Log.Error("Failed to find Eclipse 8 check (TakeDamageProcess)");
                return;
            }

            behaviorCursor.Index = checkCursor.Index;
            if (!behaviorCursor.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(13),
                x => x.MatchLdarg(0),
                x => x.MatchCall<HealthComponent>("get_fullCombinedHealth"),
                x => x.MatchDiv(),
                x => x.MatchLdcR4(100f),
                x => x.MatchMul()
            ))
            {
                Log.Error("Failed to find Eclipse 8 behavior (TakeDamageProcess)");
                return;
            }

            // Emit check
            checkCursor.Emit(OpCodes.Ldarg_0);
            checkCursor.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent =>
            {
                return votedForEclipse8.Any(
                    el => el && el.master == selfHealthComponent.body.master);
            });

            // Emit jump
            checkCursor.Emit(OpCodes.Brtrue, behaviorCursor.Next);
        }

        private static void PreGameControllerStartRun(Action<PreGameController> orig, PreGameController self)
        {
            votedForEclipse1.Clear();
            votedForEclipse2.Clear();
            votedForEclipse3.Clear();
            votedForEclipse4.Clear();
            votedForEclipse5.Clear();
            votedForEclipse6.Clear();
            votedForEclipse7.Clear();
            votedForEclipse8.Clear();
            var choice1 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse1.On");
            var choice2 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse2.On");
            var choice3 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse3.On");
            var choice4 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse4.On");
            var choice5 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse5.On");
            var choice6 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse6.On");
            var choice7 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse7.On");
            var choice8 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse8.On");
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                var voteController = PreGameRuleVoteController.FindForUser(user);

                var isEclipse1Voted = voteController.IsChoiceVoted(choice1);
                if (isEclipse1Voted)
                {
                    votedForEclipse1.Add(user);
                }

                var isEclipse2Voted = voteController.IsChoiceVoted(choice2);
                if (isEclipse2Voted)
                {
                    votedForEclipse2.Add(user);
                }

                var isEclipse3Voted = voteController.IsChoiceVoted(choice3);
                if (isEclipse3Voted)
                {
                    votedForEclipse3.Add(user);
                }

                var isEclipse4Voted = voteController.IsChoiceVoted(choice4);
                if (isEclipse4Voted)
                {
                    votedForEclipse4.Add(user);
                }

                var isEclipse5Voted = voteController.IsChoiceVoted(choice5);
                if (isEclipse5Voted)
                {
                    votedForEclipse5.Add(user);
                }

                var isEclipse6Voted = voteController.IsChoiceVoted(choice6);
                if (isEclipse6Voted)
                {
                    votedForEclipse6.Add(user);
                }

                var isEclipse7Voted = voteController.IsChoiceVoted(choice7);
                if (isEclipse7Voted)
                {
                    votedForEclipse7.Add(user);
                }

                var isEclipse8Voted = voteController.IsChoiceVoted(choice8);
                if (isEclipse8Voted)
                {
                    votedForEclipse8.Add(user);
                }
            }
            new SyncEclipse2(votedForEclipse2).Send(NetworkDestination.Clients);
            orig(self);
        }

        // This is all for configs, which are no longer needed (hopefully) due to figuring out the .hasAuthority for Eclipse 2
/*        public void DoConfigs()
        {
            HalfTPSize = Config.Bind(
                "Eclipse 2",
                "Half Teleporter Size",
                false,
                "Halves the visual teleporter radius"
            );

            try
            {
                ModSettingsManager.SetModDescription("Partial Eclipse", PluginGUID, PluginName);

                string pathString = System.IO.Path.GetDirectoryName(Instance.Info.Location);
                string iconPath = pathString.Substring(0, pathString.Length - 21);
                var iconStream = File.ReadAllBytes(System.IO.Path.Combine(iconPath, "icon.png"));
                var tex = new Texture2D(256, 256);
                tex.LoadImage(iconStream);
                var icon = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
                ModSettingsManager.SetModIcon(icon, PluginGUID, PluginName);
            } catch (Exception ex)
            {
                Log.Info("Couldn't set up description and icon");
            }
           

            ModSettingsManager.AddOption(new CheckBoxOption(HalfTPSize));
        }
*/

        public void Awake()
        {
            Log.Init(Logger);

            // Also no longer needed because of .hasAuthority
            // DoConfigs();

            new PartialEclipse1Artifact();
            new PartialEclipse2Artifact();
            new PartialEclipse3Artifact();
            new PartialEclipse4Artifact();
            new PartialEclipse5Artifact();
            new PartialEclipse6Artifact();
            new PartialEclipse7Artifact();
            new PartialEclipse8Artifact();

            NetworkingAPI.RegisterMessageType<SyncEclipse2>();
        }

        public void OnDestroy()
        {
            // For finding who voted for what
            HookEndpointManager.Remove(startRun, PreGameControllerStartRun);
            // For Eclipse 1
            HookEndpointManager.Unmodify(onBodyStart, OnBodyStartEclipse1);
            // For Eclipse 2
            HookEndpointManager.Unmodify(isBodyInChargingRadius, IsBodyInChargingRadiusEclipse2);
            HookEndpointManager.Unmodify(doUpdate, DoUpdateEclipse2);
            // For Eclipse 3
            HookEndpointManager.Unmodify(onCharacterHitGroundServer, OnCharacterHitGroundServerEclipse3);
            // For Eclipse 5
            HookEndpointManager.Unmodify(heal, HealEclipse5);
            // For Eclipse 8
            HookEndpointManager.Unmodify(damage, TakeDamageProcessEclipse8);
        }

        public void Start()
        {
            // For finding who voted for what
            HookEndpointManager.Add(startRun, PreGameControllerStartRun);
            // For Eclipse 1
            HookEndpointManager.Modify(onBodyStart, OnBodyStartEclipse1);
            // For Eclipse 2
            HookEndpointManager.Modify(isBodyInChargingRadius, IsBodyInChargingRadiusEclipse2);
            HookEndpointManager.Modify(doUpdate, DoUpdateEclipse2);
            // For Eclipse 3
            HookEndpointManager.Modify(onCharacterHitGroundServer, OnCharacterHitGroundServerEclipse3);
            // For Eclipse 4
            HookEndpointManager.Modify(recalculateStats, RecalculateStatsEclipse4);
            // For Eclipse 5
            HookEndpointManager.Modify(heal, HealEclipse5);
            // For Eclipse 6
            //HookEndpointManager.Modify()
            // For Eclipse 7
            HookEndpointManager.Modify(recalculateStats, RecalculateStatsEclipse7);
            // For Eclipse 8
            HookEndpointManager.Modify(damage, TakeDamageProcessEclipse8);
        }

        public static void RegisterEclipse2(HashSet<NetworkUser> receivedVotes)
        {
            votedForEclipse2.Clear();
            votedForEclipse2.UnionWith(receivedVotes);
        }
    }

    public class SyncEclipse2 : INetMessage
    {
        HashSet<NetworkUser> votedForEclipse2 = new();

        public SyncEclipse2()
        {
        }

        public SyncEclipse2(HashSet<NetworkUser> votes)
        {
            votedForEclipse2 = [.. votes];
        }

        public void Deserialize(NetworkReader reader)
        {
            votedForEclipse2 = new HashSet<NetworkUser>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var userObject = reader.ReadGameObject();

                if (!userObject)
                    continue;

                var user = userObject.GetComponent<NetworkUser>();

                if (user)
                    votedForEclipse2.Add(user);
            }
        }

        public void OnReceived()
        {
            PartialEclipse.RegisterEclipse2(votedForEclipse2);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(votedForEclipse2.Count);

            foreach (NetworkUser user in votedForEclipse2)
            {
                writer.Write(user ? user.gameObject : null);
            }
        }
    }
}
