﻿namespace LivingWorldMod.Globals.BaseTypes.Tiles;

/// <summary>
/// Tile that is only loaded when in Debug mode.
/// </summary>
public abstract class DebugTile : ModTile {
    public override bool IsLoadingEnabled(Mod mod) => LWM.IsDebug;
}