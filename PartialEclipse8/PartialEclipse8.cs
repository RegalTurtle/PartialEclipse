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

        private static void TakeDamage(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(x => x.MatchCall(typeof(RoR2Content.Artifacts), "get_randomSurvivorOnRespawnArtifactDef"));
            c.Index += 3;
            var endIf = c.Previous.Operand;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<CharacterMaster, bool>>(ShouldTakeCurse);
            c.Emit(OpCodes.Brfalse_S, endIf);
        }

        public static bool ShouldTakeCurse(CharacterMaster master)
        {
            return true;
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
    }
}