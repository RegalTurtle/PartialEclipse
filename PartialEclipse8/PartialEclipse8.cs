using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
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
        public const string Version = "0.0.0";

        private static readonly HashSet<NetworkUser> votedForEclipse = new HashSet<NetworkUser>();

        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamage));
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);

        private static void TakeDamage(ILContext il)
        {
            Chat.AddMessage("IL Hook");
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterMaster>>(test);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("body"),
                x => x.MatchCallvirt<CharacterBody>("get_teamComponent"),
                x => x.MatchCallvirt<TeamComponent>("get_teamIndex"),
                x => x.MatchLdcI4(1)
                );
            Chat.AddMessage(c.Index.ToString());
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterMaster>>(test2);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<CharacterMaster, bool>>(ShouldTakeCurse);
            var end = c.Next.Next.Next.Next.Next.Next.Next.Next.Next.Next.Operand;
            c.Emit(OpCodes.Brfalse_S, end);
        }

        public static bool ShouldTakeCurse(CharacterMaster master)
        {
            Chat.AddMessage("ahhhhh");
            if (votedForEclipse.Any(el => el.master == master))
            {
                return false;
            }
            return true;
        }

        public static void test(CharacterMaster master)
        {
            Chat.AddMessage(master.GetType().ToString());
        }

        public static void test2(CharacterMaster master)
        {
            Chat.AddMessage("fuck me2");
        }

        private static void PreGameControllerStartRun(Action<PreGameController> orig, PreGameController self)
        {
            Chat.AddMessage("pregame");
            votedForEclipse.Clear();
            var choice = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse8.On");
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                var voteController = PreGameRuleVoteController.FindForUser(user);
                var isMetamorphosisVoted = voteController.IsChoiceVoted(choice);

                if (isMetamorphosisVoted)
                {
                    votedForEclipse.Add(user);
                    Chat.AddMessage("arti");
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