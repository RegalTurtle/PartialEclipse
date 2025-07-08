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

namespace PartialEclipse
{
    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

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
        public const string PluginVersion = "1.3.1";

        private static readonly HashSet<NetworkUser> votedForEclipse1 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse3 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse5 = new();
        private static readonly HashSet<NetworkUser> votedForEclipse8 = new();

        // This gets anything to work at all
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);
        // For Eclipse 1
        private static readonly MethodInfo onBodyStart = typeof(CharacterMaster).GetMethod(nameof(CharacterMaster.OnBodyStart), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 3
        // This one should work with BindingFlags.Public
        private static readonly MethodInfo onCharacterHitGroundServer = typeof(GlobalEventManager).GetMethod(nameof(GlobalEventManager.OnCharacterHitGroundServer), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 5
        private static readonly MethodInfo heal = typeof(HealthComponent).GetMethod(nameof(HealthComponent.Heal), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // For Eclipse 8
        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamageProcess), BindingFlags.NonPublic | BindingFlags.Instance);

        // Eclipse 1 modification
        private static void OnBodyStart(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse1),
                x => x.MatchBlt(out _)))
            {
                Log.Error("PartialEclipse: Failed to find Eclipse check");
                return;
            }

            var eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior

            c.Index--;

            c.Emit(OpCodes.Ldarg_0); // load 'this' for the CharacterMaster
            c.EmitDelegate<Func<CharacterMaster, bool>>(selfMaster => {
                return votedForEclipse1.Any(el => el.master == selfMaster);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior); // if false, branch to the elseLabel health = fullHealth line
        }

        // Eclipse 3 modification
        private static void OnCharacterHitGroundServerEdit(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse3),
                x => x.MatchBlt(out _)))
            {
                Log.Error("PartialEclipse: Failed to find Eclipse check");
                return;
            }
            var eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior

            c.Index--;

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<CharacterBody, bool>>(selfCharacterBody => {
                return votedForEclipse3.Any(el => el.master == selfCharacterBody.master);
            });

            c.Emit(OpCodes.Brtrue_S, eclipseBehavior);
        }

        // Eclipse 5 modification
        private static void Heal(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse5),
                x => x.MatchBlt(out _)))
            {
                Log.Error("PartialEclipse: Failed to find Eclipse check");
                return;
            }
            var eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior

            c.Index--;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent => {
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

            if (!c.TryGotoNext(
                x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)DifficultyIndex.Eclipse8),
                x => x.MatchBlt(out _)))
            {
                Log.Error("PartialEclipse: Failed to find Eclipse check");
                return;
            }

            var eclipseBehavior = c.Next.Next.Next.Next; // the operand after the branch to vanilla behavior

            c.Index--;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HealthComponent, bool>>(selfHealthComponent => {
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
            votedForEclipse3.Clear();
            votedForEclipse5.Clear();
            votedForEclipse8.Clear();
            var choice1 = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse1.On");
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

        public void Awake()
        {
            Log.Init(Logger);

            new PartialEclipse1Artifact();
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
            // For Eclipse 3
            HookEndpointManager.Modify(onCharacterHitGroundServer, OnCharacterHitGroundServerEdit);
            // For Eclipse 5
            HookEndpointManager.Modify(heal, Heal);
            // For Eclipse 8
            HookEndpointManager.Modify(damage, TakeDamageProcess);
        }
    }
}
