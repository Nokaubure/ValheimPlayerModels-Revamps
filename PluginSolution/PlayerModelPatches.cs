#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Splatform;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ValheimPlayerModels
{
	[HarmonyPatch(typeof(Player), nameof(Player.GetPlayerName))]
	static class Patch_Player_GetPlayerName_AvatarName
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance, ref string __result)
		{
			PlayerModel playerModel = __instance.GetComponent<PlayerModel>();
			string avatarName = playerModel ? playerModel.GetAvatarDisplayName() : null;
			if (!string.IsNullOrEmpty(avatarName))
				__result = avatarName;
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetHoverName))]
	static class Patch_Player_GetHoverName_AvatarName
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance, ref string __result)
		{
			PlayerModel playerModel = __instance.GetComponent<PlayerModel>();
			string avatarName = playerModel ? playerModel.GetAvatarDisplayName() : null;
			if (!string.IsNullOrEmpty(avatarName))
				__result = avatarName;
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.TryGetPlayerByPlatformUserID))]
	static class Patch_ZNet_TryGetPlayerByPlatformUserID_AvatarName
	{
		[HarmonyPostfix]
		static void Postfix(ref ZNet.PlayerInfo __1, bool __result)
		{
			if (!__result) return;
			long characterUserID = __1.m_characterID.UserID;

			PlayerModel playerModel = Object.FindObjectsOfType<PlayerModel>().FirstOrDefault(model =>
				model && model.player &&
				model.player.GetZDOID().UserID == characterUserID);
			if (!playerModel) return;

			string avatarName = playerModel.GetAvatarDisplayName();
			if (!string.IsNullOrEmpty(avatarName))
				__1.m_name = avatarName;
		}
	}

	[HarmonyPatch(typeof(Chat), nameof(Chat.OnNewChatMessage))]
	static class Patch_Chat_OnNewChatMessage_AvatarBubbleName
	{
		[HarmonyPrefix]
		static void Prefix(long senderID, UserInfo sender)
		{
			if (sender == null) return;

			PlayerModel playerModel = Object.FindObjectsOfType<PlayerModel>().FirstOrDefault(model =>
				model && model.player &&
				(model.player.GetPlayerID() == senderID || model.player.GetZDOID().UserID == senderID));
			if (!playerModel) return;

			string avatarName = playerModel.GetAvatarDisplayName();
			if (!string.IsNullOrEmpty(avatarName))
				sender.Name = avatarName;
		}
	}

	[HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), new[]
	{
		typeof(PlatformUserID), typeof(string), typeof(Talker.Type), typeof(bool)
	})]
	static class Patch_Terminal_AddString_KeepShoutCase
	{
		private static string KeepOriginalCase(string text) => text;

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo toUpper = AccessTools.Method(typeof(string), nameof(string.ToUpper), Type.EmptyTypes);
			MethodInfo keepOriginalCase = AccessTools.Method(
				typeof(Patch_Terminal_AddString_KeepShoutCase), nameof(KeepOriginalCase));

			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(toUpper))
				{
					instruction.opcode = OpCodes.Call;
					instruction.operand = keepOriginalCase;
				}

				yield return instruction;
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
	static class Patch_Humanoid_StartAttack_Sounds
	{
		[HarmonyPostfix]
		static void Postfix(Humanoid __instance, bool __result, bool secondaryAttack)
		{
			if (__result && __instance is Player player)
				player.GetComponent<PlayerModel>()?.PlayAvatarSound(secondaryAttack
					? ValheimAvatarSoundType.HeavyAttack
					: ValheimAvatarSoundType.Attack);
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.StartGuardianPower))]
	static class Patch_Player_StartGuardianPower_Sounds
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance, bool __result)
		{
			if (__result)
				__instance.GetComponent<PlayerModel>()?.PlayAvatarSound(ValheimAvatarSoundType.Skill);
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.OnJump))]
	static class Patch_Player_OnJump_Sounds
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance)
		{
			__instance.GetComponent<PlayerModel>()?.PlayAvatarSound(ValheimAvatarSoundType.Jump);
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.OnDamaged))]
	static class Patch_Player_OnDamaged_Sounds
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance)
		{
			__instance.GetComponent<PlayerModel>()?.PlayAvatarSound(ValheimAvatarSoundType.Hurt);
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Stagger))]
	static class Patch_Character_Stagger_AvatarSound
	{
		[HarmonyPostfix]
		static void Postfix(Character __instance)
		{
			if (__instance is Player player)
				player.GetComponent<PlayerModel>()?.PlayAvatarSound(ValheimAvatarSoundType.Flinch);
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
	static class Patch_Player_OnDeath_Sounds
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance)
		{
			__instance.GetComponent<PlayerModel>()?.PlayAvatarSound(ValheimAvatarSoundType.Dead);
		}
	}

	[HarmonyPatch(typeof(Player), "Awake")]
	static class Patch_Player_Awake
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance)
        {
            if(PluginConfig.enablePlayerModels.Value)
                __instance.gameObject.AddComponent<PlayerModel>();
        }
	}

    [HarmonyPatch(typeof(VisEquipment), "UpdateLodgroup")]
    static class Patch_VisEquipment_UpdateLodgroup
    {
        [HarmonyPostfix]
        static void Postfix(VisEquipment __instance)
        {
            if (PluginConfig.enablePlayerModels.Value)
                __instance.GetComponent<PlayerModel>()?.ToggleEquipments();
        }
    }

    [HarmonyPatch(typeof(Ragdoll), "Start")]
    static class Patch_Ragdoll_Start
    {
        [HarmonyPostfix]
        static void Postfix(Ragdoll __instance)
        {
            if (PluginConfig.enableCustomRagdoll.Value)
            {
                if (__instance.gameObject.name.StartsWith("Player"))
                {
                    if (ZNet.instance)
                    {
                        PlayerModel[] playerModels = Object.FindObjectsOfType<PlayerModel>();
                        PlayerModel player = playerModels.FirstOrDefault(p =>
                            p.player.GetZDOID().UserID == __instance.m_nview.m_zdo.m_uid.UserID);

                        if (player) player.SetupRagdoll(__instance);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), "TryRunCommand")]
    static class Patch_Terminal_TryRunCommand
    {
        [HarmonyPostfix]
        static void Postfix(Terminal __instance, string text, bool silentFail = false, bool skipAllowedCheck = false)
        {
            string command = text.ToLower();
            string[] param = command.Split(' ');
            if (command.StartsWith("anim") && param.Length == 3)
            {
                if (PlayerModel.localModel)
                {
                    if (bool.TryParse(param[2], out bool valueBool))
                        if (PlayerModel.localModel.avatar.SetBool(param[1], valueBool)) return;

                    if (int.TryParse(param[2], out int valueInt))
                        if (PlayerModel.localModel.avatar.SetInt(param[1], valueInt)) return;

                    if (float.TryParse(param[2], out float valuefloat))
                        PlayerModel.localModel.avatar.SetFloat(param[1], valuefloat);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    static class Patch_GameCamera_UpdateMouseCapture
    {
        [HarmonyPrefix]
        static bool Prefix(GameCamera __instance)
        {
            if (PluginConfig.enablePlayerModels.Value && (Plugin.showActionMenu || Plugin.showAvatarMenu))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "SetMouseLook")]
    static class Patch_Player_SetMouseLook
    {
        [HarmonyPrefix]
        static bool Prefix(Player __instance)
        {
            if (PluginConfig.enablePlayerModels.Value && (Plugin.showActionMenu || Plugin.showAvatarMenu))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "SetVisible")]
    static class Patch_Character_SetVisible
    {
        [HarmonyPostfix]
        static void Postfix(Character __instance, bool visible)
        {
            if (!__instance.IsPlayer()) return;

            PlayerModel playerModel;

            if (!Plugin.playerModelCharacters.ContainsKey(__instance))
            {
                playerModel = __instance.GetVisual().transform.parent.GetComponent<PlayerModel>();
                Plugin.playerModelCharacters.Add(__instance, playerModel);
            }
            else
            {
                playerModel = Plugin.playerModelCharacters[__instance];
            }

            if (!playerModel || playerModel.avatar == null) return;

            var lodGroup = playerModel.avatar.lodGroup;
            if (!lodGroup) return;

            if (visible)
            {
                lodGroup.localReferencePoint = __instance.m_originalLocalRef;
            }
            else
            {
                lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
            }
        }
    }

    [HarmonyPatch(typeof(EntryPointSceneLoader), nameof(EntryPointSceneLoader.Start))]
    static class Patch_All_SoftReferenceableAssets {
        [HarmonyPrefix]
        static void Prefix() {
            SoftReferenceableAssets.Runtime.MakeAllAssetsLoadable();
        }
    }
}
#endif
