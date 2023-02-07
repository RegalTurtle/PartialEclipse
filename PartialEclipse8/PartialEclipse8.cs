using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using R2API;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using MonoMod.RuntimeDetour.HookGen;

[assembly: AssemblyVersion(PartialEclipse8.PartialEclipse8Plugin.Version)]
namespace PartialEclipse8
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.bepis.r2api")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(ItemAPI), nameof(EliteAPI), nameof(ContentAddition))]

    public class PartialEclipse8Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.RegalTurtle.PartialEclipse";
        public const string Name = "Partial Eclipse";
        public const string Version = "1.0.0";

        private static readonly HashSet<NetworkUser> votedForEclipse = new HashSet<NetworkUser>();

        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamage));
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);

        private static void TakeDamage(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("body"),
                x => x.MatchCallvirt<CharacterBody>("get_teamComponent"),
                x => x.MatchCallvirt<TeamComponent  >("get_teamIndex"),
                x => x.MatchLdcI4(1)
                );
            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<Func<CharacterMaster, bool>>(ShouldTakeCurse);
            //var end = c.Index + 10;
            //c.Emit(OpCodes.Brfalse_S, end);
            c.Emit(OpCodes.Ldloc, 42);
            c.Emit(OpCodes.Ldloc, 7);
            c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<Func<CharacterMaster, bool>>(new Func<CharacterMaster, bool>(PartialEclipse8Plugin.ShouldTakeCurse));
            c.EmitDelegate<Action<CharacterMaster, float, HealthComponent>>(NewTakeCurse);
            //object end = c.Next.Next.Next.Next.Next.Next.Next.Next.Next.Next.Operand;
            //c.Emit(OpCodes.Brtrue, end);
        }

        public static bool ShouldTakeCurse(CharacterMaster master)
        {
            if (votedForEclipse.Any(el => el.master == master))
            {
                return true;
            }
            return false;
        }

        public static void NewTakeCurse(CharacterMaster master, float num, HealthComponent that)
        {
            if (votedForEclipse.Any(el => el.master == master))
            {
                float num13 = num / that.fullCombinedHealth * 100f;
                float num14 = 0.4f;
                int num15 = Mathf.FloorToInt(num13 * num14);
                for (int k = 0; k < num15; k++)
                {
                    that.body.AddBuff(RoR2Content.Buffs.PermanentCurse);
                }
            }
        }

        private static void PreGameControllerStartRun(Action<PreGameController> orig, PreGameController self)
        {
            votedForEclipse.Clear();
            var choice = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse8.On");
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                var voteController = PreGameRuleVoteController.FindForUser(user);
                var isMetamorphosisVoted = voteController.IsChoiceVoted(choice);

                if (isMetamorphosisVoted)
                {
                    votedForEclipse.Add(user);
                }
            }
            orig(self);
        }

        public void Awake()
        {
            new PartialEclipse8Artifact();
        }

        public void Destroy()
        {
            HookEndpointManager.Unmodify(damage, (ILContext.Manipulator)TakeDamage);
        }

        public void Start()
        {
            HookEndpointManager.Add(startRun, (Action<Action<PreGameController>, PreGameController>)PreGameControllerStartRun);
            HookEndpointManager.Modify(damage, (ILContext.Manipulator)TakeDamage);
        }
    }
}