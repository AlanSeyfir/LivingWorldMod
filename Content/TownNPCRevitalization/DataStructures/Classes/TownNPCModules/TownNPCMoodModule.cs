﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hjson;
using LivingWorldMod.Content.TownNPCRevitalization.DataStructures.Records;
using LivingWorldMod.Content.TownNPCRevitalization.DataStructures.Structs;
using LivingWorldMod.Utilities;
using Terraria.Localization;

namespace LivingWorldMod.Content.TownNPCRevitalization.DataStructures.Classes.TownNPCModules;

/// <summary>
///     Class that handles the new "mood" feature that replaces Town NPC happiness.
/// </summary>
public sealed class TownNPCMoodModule : TownNPCModule {
    public const float MaxMoodValue = 100f;
    public const float MinMoodValue = 0f;

    private const float BaseMoodValue = 50;

    private static Dictionary<string, MoodModifier> _moodModifiers;

    private static Dictionary<string, Dictionary<string, LocalizedText>> _npcFlavorTexts;
    private static readonly Regex FlavorTextLoadRegex = new(@"(.+\.(?<Name>.+)\.TownNPCMood|TownNPCMood_(?<Name>.+))\.(?<Mood>.+)");

    private readonly List<MoodModifierInstance> _currentStaticMoodModifiers;
    private readonly List<MoodModifierInstance> _currentDynamicMoodModifiers;

    public IReadOnlyList<MoodModifierInstance> CurrentDynamicMoodModifiers => _currentDynamicMoodModifiers;

    public IReadOnlyList<MoodModifierInstance> CurrentStaticMoodModifiers => _currentStaticMoodModifiers;

    public float CurrentMood => Utils.Clamp(BaseMoodValue + _currentDynamicMoodModifiers.Concat(_currentStaticMoodModifiers).Sum(modifier => modifier.modifierType.MoodOffset), MinMoodValue, MaxMoodValue);

    public TownNPCMoodModule(NPC npc) : base(npc) {
        _currentStaticMoodModifiers = [];
        _currentDynamicMoodModifiers = [];
    }

    public static void Load() {
        JsonObject jsonMoodValues = LWMUtils.GetJSONFromFile("Assets/JSONData/TownNPCMoodValues.json").Qo();
        _moodModifiers = new Dictionary<string, MoodModifier>();
        foreach ((string moodModifierKey, float moodOffset) in jsonMoodValues) {
            _moodModifiers[moodModifierKey] = new MoodModifier($"TowNPCMoodDescription.{moodModifierKey}".Localized(), moodOffset);
        }

        _npcFlavorTexts = new Dictionary<string, Dictionary<string, LocalizedText>>();
        foreach ((string key, LocalizedText text) in LanguageManager.Instance._localizedTexts) {
            if (!key.Contains("TownNPCMood")) {
                continue;
            }

            Match moodMatch = FlavorTextLoadRegex.Match(key);
            if (moodMatch == Match.Empty) {
                continue;
            }

            string npcName = moodMatch.Groups["Name"].Value;
            if (!_npcFlavorTexts.TryGetValue(npcName, out Dictionary<string, LocalizedText> value)) {
                value = new Dictionary<string, LocalizedText>();
                _npcFlavorTexts[npcName] = value;
            }

            value[moodMatch.Groups["Mood"].Value] = text;
        }
    }

    public override void Update() {
        for (int i = 0; i < _currentDynamicMoodModifiers.Count; i++) {
            MoodModifierInstance instance = _currentDynamicMoodModifiers[i];
            if (--instance.duration <= 0) {
                _currentDynamicMoodModifiers.RemoveAt(i--);
            }
        }
    }

    public void AddStaticModifier(string modifierKey, LocalizedText flavorText) {
        if (!_moodModifiers.TryGetValue(modifierKey, out MoodModifier moodModifier)) {
            return;
        }

        _currentStaticMoodModifiers.Add(new MoodModifierInstance(moodModifier, flavorText, 0));
    }

    public void AddDynamicModifier(string modifierKey, LocalizedText flavorText, int duration) {
        if (!_moodModifiers.TryGetValue(modifierKey, out MoodModifier moodModifier)) {
            return;
        }

        _currentDynamicMoodModifiers.Add(new MoodModifierInstance(moodModifier, flavorText, duration));
    }

    public void ResetStaticModifiers() {
        _currentStaticMoodModifiers.Clear();
    }
}