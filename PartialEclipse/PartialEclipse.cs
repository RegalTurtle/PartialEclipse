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
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

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
        public const string PluginVersion = "0.0.1";

        private static readonly HashSet<NetworkUser> votedForEclipse8 = new();

        private static readonly MethodInfo damage = typeof(HealthComponent).GetMethod(nameof(HealthComponent.TakeDamageProcess), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);

        private static void TakeDamageProcess(ILContext il)
        {
            // Log.Info($"Applying IL Hook");

            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("body"),
                x => x.MatchCallvirt<CharacterBody>("get_teamComponent"),
                x => x.MatchCallvirt<TeamComponent>("get_teamIndex"),
                x => x.MatchLdcI4(1)
            );

            c.Emit(OpCodes.Ldloc, 61);
            c.Emit(OpCodes.Ldloc, 8);
            c.Emit(OpCodes.Ldarg_0);

            c.EmitDelegate<Action<CharacterMaster, float, HealthComponent>>(NewTakeCurse);

            // Log.Info($"Done applying IL Hook");
        }

        public static void NewTakeCurse(CharacterMaster master, float damageAmount, HealthComponent that)
        {
            // Log.Info($"Damage taken and hook used");
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
            votedForEclipse8.Clear();
            var choice = RuleCatalog.FindChoiceDef("Artifacts.PartialEclipse8.On");
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                var voteController = PreGameRuleVoteController.FindForUser(user);
                var isMetamorphosisVoted = voteController.IsChoiceVoted(choice);

                if (isMetamorphosisVoted)
                {
                    votedForEclipse8.Add(user);
                }
            }
            System.Console.WriteLine(votedForEclipse8);
            orig(self);
        }

        public void Awake()
        {
            // Log.Init(Logger);

            new PartialEclipse8Artifact();
        }

        public void Destroy()
        {
            HookEndpointManager.Unmodify(damage, (ILContext.Manipulator)TakeDamageProcess);
        }

        public void Start()
        {
            HookEndpointManager.Add(startRun, (Action<Action<PreGameController>, PreGameController>)PreGameControllerStartRun);
            HookEndpointManager.Modify(damage, (ILContext.Manipulator)TakeDamageProcess);
        }
    }
}
