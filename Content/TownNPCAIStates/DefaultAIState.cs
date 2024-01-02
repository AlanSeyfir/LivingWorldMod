﻿using LivingWorldMod.Common.GlobalNPCs;
using LivingWorldMod.Common.ModTypes;
using LivingWorldMod.Custom.Utilities;
using Terraria;

namespace LivingWorldMod.Content.TownNPCAIStates;

/// <summary>
/// The state where the NPC is standing still and simply occasionally
/// looking left and right. This state is the one all Town NPCs will be
/// in if they have absolutely nothing to do.
/// </summary>
public class DefaultAIState : TownNPCAIState {
    public override int ReservedStateInteger => 0;

    public override void DoState(TownAIGlobalNPC globalNPC, NPC npc) {
        if (npc.breath == 0) {
            TownAIGlobalNPC.RefreshToState<WanderAIState>(npc);
            return;
        }

        if (--npc.ai[1] > 0) {
            return;
        }

        if (Main.rand.NextBool(4, 10)) {
            TownAIGlobalNPC.RefreshToState<WanderAIState>(npc);
            return;
        }

        npc.ai[1] = Main.rand.Next(UnitUtils.RealLifeSecond * 5, UnitUtils.RealLifeSecond * 8);
        npc.direction = -npc.direction;
        npc.netUpdate = true;
    }
}