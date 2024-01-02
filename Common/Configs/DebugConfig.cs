﻿using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LivingWorldMod.Common.Configs {
    /// <summary>
    /// Config that handles debug related matters in the mod.
    /// </summary>
    public class DebugConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(false)]
        [ReloadRequired]
        public bool forceDebugMode;
    }
}