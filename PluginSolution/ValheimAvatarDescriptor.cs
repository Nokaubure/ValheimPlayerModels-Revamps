using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimPlayerModels
{
    public enum ControlType
    {
        Toggle,
        Button,
        Slider
    }

    public enum ValheimAvatarParameterType
    {
        Bool,
        Int,
        Float
    }

    public enum ValheimAvatarSoundType
    {
        Attack,
        HeavyAttack,
        Jump,
        Hurt,
        Dead,
        Skill
    }

    [Serializable]
    public struct ValheimAvatarParameter
    {
        public string name;
        public ValheimAvatarParameterType type;
        public float defaultValue;
    }

    [Serializable]
    public struct ValheimAvatarActionMenuItem
    {
        public string name;
        public ControlType type;
        public string parameterName;
        public float value;
    }
    
    public class ValheimAvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
    {
        public string avatarName = "player";

        public Transform leftHand;
        public Transform rightHand;
        public Transform helmet;
        public Transform backShield;
        public Transform backMelee;
        public Transform backTwohandedMelee;
        public Transform backBow;
        public Transform backTool;
        public Transform backAtgeir;

        public bool showHelmet;
        public bool showCape;

        [Header("Action Sounds")]
        public AudioClip[] attackSounds = new AudioClip[0];
        public AudioClip[] heavyAttackSounds = new AudioClip[0];
        public AudioClip[] jumpSounds = new AudioClip[0];
        public AudioClip[] hurtSounds = new AudioClip[0];
        public AudioClip[] deadSounds = new AudioClip[0];
        public AudioClip[] skillSounds = new AudioClip[0];
        
        public List<ValheimAvatarParameter> animatorParameters = new List<ValheimAvatarParameter>();

        // unfortunately, BepInEx plugin structs & classes do not work properly with Unity's serialization system
        // we could use https://github.com/xiaoxiao921/FixPluginTypesSerialization to fix this, or maybe JsonUtility
        // but this is a simple workaround that works fine, so we'll use it for now
        public List<string> animatorParameterNames = new List<string>();
        public List<ValheimAvatarParameterType> animatorParameterTypes = new List<ValheimAvatarParameterType>();
        public List<float> animatorParameterDefaultValues = new List<float>();
        
        public List<ValheimAvatarActionMenuItem> actionMenuItems = new List<ValheimAvatarActionMenuItem>();
        
        // parallel lists to serialize actionMenuItems reliably
        [HideInInspector]
        public List<string> actionMenuItemNames = new List<string>();
        [HideInInspector]
        public List<ControlType> actionMenuItemTypes = new List<ControlType>();
        [HideInInspector]
        public List<string> actionMenuItemParameterNames = new List<string>();
        [HideInInspector]
        public List<float> actionMenuItemValues = new List<float>();

        private void Awake()
        {
            Validate();
        }

        public void Validate()
        {
           
        }

        public void OnBeforeSerialize() {
            // clear legacy lists by ensuring they are not used anymore

            animatorParameterNames.Clear();
            animatorParameterTypes.Clear();
            animatorParameterDefaultValues.Clear();

            foreach (var parameter in animatorParameters)
            {
                animatorParameterNames.Add(parameter.name);
                animatorParameterTypes.Add(parameter.type);
                animatorParameterDefaultValues.Add(parameter.defaultValue);
            }

            // prepare action menu parallel lists for serialization
            actionMenuItemNames.Clear();
            actionMenuItemTypes.Clear();
            actionMenuItemParameterNames.Clear();
            actionMenuItemValues.Clear();

            foreach (var item in actionMenuItems)
            {
                actionMenuItemNames.Add(item.name);
                actionMenuItemTypes.Add(item.type);
                actionMenuItemParameterNames.Add(item.parameterName);
                actionMenuItemValues.Add(item.value);
            }
        }
        public void OnAfterDeserialize() {
            //Plugin.Log.LogInfo("actionMenuItems " + actionMenuItems.Count);
            #if PLUGIN
            // only fill in the true parameter list from the individual lists in the plugin
            animatorParameters.Clear();
            for (var i = 0; i < animatorParameterNames.Count; i++)
            {
                animatorParameters.Add(new ValheimAvatarParameter
                {
                    name = animatorParameterNames[i],
                    type = animatorParameterTypes[i],
                    defaultValue = animatorParameterDefaultValues[i]
                });
            }

            // reconstruct actionMenuItems from parallel lists
            actionMenuItems.Clear();
            var count = Math.Min(Math.Min(actionMenuItemNames.Count, actionMenuItemTypes.Count), Math.Min(actionMenuItemParameterNames.Count, actionMenuItemValues.Count));
            for (var i = 0; i < count; i++)
            {
                actionMenuItems.Add(new ValheimAvatarActionMenuItem
                {
                    name = actionMenuItemNames[i],
                    type = actionMenuItemTypes[i],
                    parameterName = actionMenuItemParameterNames[i],
                    value = actionMenuItemValues[i]
                });
            }
            #endif
        }
    }
}
