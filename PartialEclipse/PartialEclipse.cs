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
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using System.IO;

namespace PartialEclipse
{
    // Soft dependancy for EclipseArtifacts
    [BepInDependency("Judgy.EclipseArtifacts", BepInDependency.DependencyFlags.SoftDependency)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

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
        private static readonly HashSet<NetworkUser> votedForEclipse5 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse8 = new();

        public static bool EclipseArtifactsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Judgy.EclipseArtifacts");
        public static bool RiskOfOptionsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        // This gets anything to work at all
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);
        // For Eclipse 1
        private static readonly MethodInfo onBodyStart = typeof(CharacterMaster).GetMethod(nameof(CharacterMaster.OnBodyStart), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 2
        private static readonly MethodInfo isBodyInChargingRadius = typeof(HoldoutZoneController).GetMethod(nameof(HoldoutZoneController.IsBodyInChargingRadius), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo doUpdate = typeof(HoldoutZoneController).GetMethod(nameof(HoldoutZoneController.DoUpdate), BindingFlags.Instance | BindingFlags.NonPublic);
        // For Eclipse 3
        private static readonly MethodInfo onCharacterHitGroundServer = typeof(GlobalEventManager).GetMethod(nameof(GlobalEventManager.OnCharacterHitGroundServer), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 5
        private static readonly MethodInfo heal = typeof(HealthComponent).GetMethod(nameof(HealthComponent.Heal), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 8
        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamageProcess), BindingFlags.NonPublic | BindingFlags.Instance);

        // For configs
        //internal static ConfigEntry<bool> HalfTPSize { get; set; }

        // Eclipse 1 modification
        private static void OnBodyStart(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction eclipseBehavior;

            // Behavior here only for EclipseArtifacts installed
            if (EclipseArtifactsInstalled)
            {
                if (!c.TryGotoNext(
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty")))
                {
                    Log.Error("Failed to find Eclipse 1 check, EclipseArtifacts loaded");
                    return;
                }

                // This is 3 more .Nexts to skip over the behavior added by EclipseArtifacts
                eclipseBehavior = c.Next.Next.Next.Next.Next.Next.Next;
            }
            // Typical behavior
            else
            {
                if (!c.TryGotoNext( // default behavior
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                    x => x.MatchLdcI4((int)DifficultyIndex.Eclipse1),
                    x => x.MatchBlt(out _)))
                {
                    Log.Error("Failed to find Eclipse 1 check");
                    return;
                }

                eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior
            }

            c.Index--;

            c.Emit(OpCodes.Ldarg_0); // load 'this' for the CharacterMaster
            c.EmitDelegate<Func<CharacterMaster, bool>>(selfMaster =>
            {
                return votedForEclipse1.Any(el => el.master == selfMaster);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior); // if false, branch to the elseLabel health = fullHealth line
        }

        // Eclipse 2 modification
        private static void IsBodyInChargingRadiusEdit(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                x => x.MatchLdarg(2)))
            {
                Log.Error("Failed to find loading of arg 2 (for Eclipse 2)");
                return;
            }

            c.Index++;

            c.Emit(OpCodes.Ldarg_3);

            c.EmitDelegate<Func<float, CharacterBody, float>>((chargingRadiusSq, charBody) =>
            {
                if (votedForEclipse2.Any(el => el.master == charBody.master))
                {
                    return chargingRadiusSq / 4f;
                }
                return chargingRadiusSq;
            });
        }

        private static void DoUpdateEdit(ILContext il)
        {
            var c = new ILCursor(il);

            // Run.instance.selectedDifficulty
            if (!c.TryGotoNext(
                x => x.MatchLdcR4(2f),
                x => x.MatchLdarg(0),
                x => x.MatchCall<HoldoutZoneController>("get_currentRadius"),
                x => x.MatchMul()
            ))
            {
                Log.Error("Eclipse 2: Failed to find radius hook");
                return;
            }

            c.Index++;

            c.EmitDelegate<Func<float, float>>((tpMult) =>
            {
                //if (votedForEclipse2.Any(el => el.master == charBody.master))
                //if (HalfTPSize.Value)
                if (votedForEclipse2.Any(el => el.hasAuthority))
                {
                    return 1f;
                }
                return 2f;
            });
        }

        // Eclipse 3 modification
        private static void OnCharacterHitGroundServerEdit(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction eclipseBehavior;

            // Behavior here only for EclipseArtifacts installed
            if (EclipseArtifactsInstalled)
            {
                if (!c.TryGotoNext(
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty")))
                {
                    Log.Error("Failed to find Eclipse 3 check, EclipseArtifacts loaded");
                    return;
                }

                // This is 3 more .Nexts to skip over the behavior added by EclipseArtifacts
                eclipseBehavior = c.Next.Next.Next.Next.Next.Next.Next;
            }
            // Typical behavior
            else
            {
                if (!c.TryGotoNext( // default behavior
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                    x => x.MatchLdcI4((int)DifficultyIndex.Eclipse3),
                    x => x.MatchBlt(out _)))
                {
                    Log.Error("Failed to find Eclipse 3 check");
                    return;
                }

                eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior
            }

            c.Index--;

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<CharacterBody, bool>>(selfCharacterBody =>
            {
                return votedForEclipse3.Any(el => el.master == selfCharacterBody.master);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior);
        }

        // Eclipse 5 modification
        private static void Heal(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction eclipseBehavior;

            // Behavior here only for EclipseArtifacts installed
            if (EclipseArtifactsInstalled)
            {
                if (!c.TryGotoNext(
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty")))
                {
                    Log.Error("Failed to find Eclipse 5 check, EclipseArtifacts loaded");
                    return;
                }

                // This is 3 more .Nexts to skip over the behavior added by EclipseArtifacts
                eclipseBehavior = c.Next.Next.Next.Next.Next.Next.Next;
            }
            // Typical behavior
            else
            {
                if (!c.TryGotoNext( // default behavior
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                    x => x.MatchLdcI4((int)DifficultyIndex.Eclipse5),
                    x => x.MatchBlt(out _)))
                {
                    Log.Error("Failed to find Eclipse 5 check");
                    return;
                }

                eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior
            }

            c.Index--;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent =>
            {
                return votedForEclipse5.Any(el => el.master == selfHealthComponent.body.master);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior);
        }

        public static float NewHalfHealing(HealthComponent that, float healAmount)
        {
            if (votedForEclipse5.Any(el => el.master == that.body.master))
            {
                return healAmount / 2f;
            }
            return healAmount;
        }

        // Eclipse 8 modification
        private static void TakeDamageProcess(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction eclipseBehavior;

            // Behavior here only for EclipseArtifacts installed
            if (EclipseArtifactsInstalled)
            {
                if (!c.TryGotoNext(
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty")))
                {
                    Log.Error("Failed to find Eclipse 8 check, EclipseArtifacts loaded");
                    return;
                }

                // This is 3 more .Nexts to skip over the behavior added by EclipseArtifacts
                eclipseBehavior = c.Next.Next.Next.Next.Next.Next.Next;
            }
            // Typical behavior
            else
            {
                if (!c.TryGotoNext( // default behavior
                    x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                    x => x.MatchLdcI4((int)DifficultyIndex.Eclipse8),
                    x => x.MatchBlt(out _)))
                {
                    Log.Error("Failed to find Eclipse 8 check");
                    return;
                }

                eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior
            }

            c.Index--;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent =>
            {
                return votedForEclipse8.Any(el => el.master == selfHealthComponent.body.master);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior);
        }

        public static void NewTakeCurse(CharacterMaster master, float damageAmount, HealthComponent that)
        {
            // If any of the people that voted for eclipse match the master of the object taking damage, apply curse
            if (votedForEclipse8.Any(el => el.master == master))
            {
                float percentOfHealth = damageAmount / that.fullCombinedHealth * 100f;
                float curseProportion = 0.4f;
                int numStacks = Mathf.FloorToInt(percentOfHealth * curseProportion);
                for (int k = 0; k < numStacks; k++)
                {
                    that.body.AddBuff(RoR2Content.Buffs.PermanentCurse);
                }
            }
        }

        private static void PreGameControllerStartRun(Action<PreGameController> orig, PreGameController self)
        {
            votedForEclipse1.Clear();
            votedForEclipse2.Clear();
            votedForEclipse3.Clear();
            votedForEclipse5.Clear();
            votedForEclipse8.Clear();
            var choice1 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse1.On");
            var choice2 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse2.On");
            var choice3 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse3.On");
            var choice5 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse5.On");
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

                var isEclipse5Voted = voteController.IsChoiceVoted(choice5);
                if (isEclipse5Voted)
                {
                    votedForEclipse5.Add(user);
                }

                var isEclipse8Voted = voteController.IsChoiceVoted(choice8);
                if (isEclipse8Voted)
                {
                    votedForEclipse8.Add(user);
                }
            }
            System.Console.WriteLine(votedForEclipse8);
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
            //DoConfigs();

            new PartialEclipse1Artifact();
            new PartialEclipse2Artifact();
            new PartialEclipse3Artifact();
            new PartialEclipse5Artifact();
            new PartialEclipse8Artifact();
        }

        public void Destroy()
        {
            // For finding who voted for what
            HookEndpointManager.Remove(startRun, PreGameControllerStartRun);
            // For Eclipse 1
            HookEndpointManager.Unmodify(onBodyStart, OnBodyStart);
            // For Eclipse 2
            HookEndpointManager.Unmodify(isBodyInChargingRadius, IsBodyInChargingRadiusEdit);
            HookEndpointManager.Unmodify(doUpdate, DoUpdateEdit);
            // For Eclipse 3
            HookEndpointManager.Unmodify(onCharacterHitGroundServer, OnCharacterHitGroundServerEdit);
            // For Eclipse 5
            HookEndpointManager.Unmodify(heal, Heal);
            // For Eclipse 8
            HookEndpointManager.Unmodify(damage, TakeDamageProcess);
        }

        public void Start()
        {
            // For finding who voted for what
            HookEndpointManager.Add(startRun, PreGameControllerStartRun);
            // For Eclipse 1
            HookEndpointManager.Modify(onBodyStart, OnBodyStart);
            // For Eclipse 2
            HookEndpointManager.Modify(isBodyInChargingRadius, IsBodyInChargingRadiusEdit);
            HookEndpointManager.Modify(doUpdate, DoUpdateEdit);
            // For Eclipse 3
            HookEndpointManager.Modify(onCharacterHitGroundServer, OnCharacterHitGroundServerEdit);
            // For Eclipse 5
            HookEndpointManager.Modify(heal, Heal);
            // For Eclipse 8
            HookEndpointManager.Modify(damage, TakeDamageProcess);
        }
    }
}
