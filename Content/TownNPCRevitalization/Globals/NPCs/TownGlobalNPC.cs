﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LivingWorldMod.Content.TownNPCRevitalization.AIStates;
using LivingWorldMod.Content.TownNPCRevitalization.DataStructures.Classes.TownNPCModules;
using LivingWorldMod.Content.TownNPCRevitalization.Globals.ModTypes;
using LivingWorldMod.DataStructures.Classes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.IO;

namespace LivingWorldMod.Content.TownNPCRevitalization.Globals.NPCs;

/// <summary>
///     Global NPC that handles exclusively the AI overhaul for Town NPCs.
/// </summary>
public class TownGlobalNPC : GlobalNPC {
    /// <summary>
    ///     How many activities are remembered by this NPC for the
    ///     purpose of preventing activity repetition.
    /// </summary>
    private const int LastActivityMemoryLimit = 5;

    public static IReadOnlyDictionary<int, (Texture2D, Texture2D)> talkBlinkOverlays;

    private static IReadOnlyDictionary<int, TownNPCAIState> _stateDict;
    private static IReadOnlyList<TownNPCActivity> _allActivities;

    private ForgetfulArray<TownNPCActivity> _lastActivities;

    public override bool InstancePerEntity => true;

    /// <summary>
    ///     Instance of the module that handles all path-finding
    ///     for this specific Town NPC.
    /// </summary>
    public TownNPCPathfinderModule PathfinderModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles the mood for this
    ///     specific Town NPC.
    /// </summary>
    public TownNPCMoodModule MoodModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles attacks & general
    ///     combat for this specific Town NPC.
    /// </summary>
    public TownNPCCombatModule CombatModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles NPCs being chatted to
    ///     by players or talking to other NPCs.
    /// </summary>
    public TownNPCChatModule ChatModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles any miscellaneous drawing
    ///     tasks.
    /// </summary>
    public TownNPCSpriteModule SpriteModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles housing tasks and bounding.
    /// </summary>
    public TownNPCHousingModule HousingModule {
        get;
        private set;
    }

    /// <summary>
    ///     Instance of the module that handles special collision logic.
    /// </summary>
    public TownNPCCollisionModule CollisionModule {
        get;
        private set;
    }

    public static void RefreshToState<T>(NPC npc) where T : TownNPCAIState => RefreshToState(npc, TownNPCAIState.GetStateInteger<T>());

    public static bool IsValidStandingPosition(NPC npc, Point tilePos) {
        bool foundTileToStandOn = false;
        for (int i = 0; i < (int)Math.Ceiling(npc.width / 16f); i++) {
            Tile floorTile = Main.tile[tilePos + new Point(i, 1)];
            if (!floorTile.HasUnactuatedTile || floorTile.IsHalfBlock || (!Main.tileSolidTop[floorTile.TileType] && !Main.tileSolid[floorTile.TileType])) {
                continue;
            }

            foundTileToStandOn = true;
            break;
        }

        if (!foundTileToStandOn) {
            return false;
        }

        int npcTileHeight = (int)Math.Ceiling(npc.height / 16f);
        for (int i = 0; i < npcTileHeight; i++) {
            tilePos.Y--;
            Tile upTile = Main.tile[tilePos];
            if (upTile.HasUnactuatedTile && Main.tileSolid[upTile.TileType]) {
                return false;
            }
        }

        return true;
    }

    public static void RefreshToState(NPC npc, int stateValue) {
        npc.ai[0] = stateValue;
        npc.ai[1] = npc.ai[2] = npc.ai[3] = 0;
        npc.netUpdate = true;
    }

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => lateInstantiation
        && entity.aiStyle == NPCAIStyleID.Passive
        && entity.townNPC
        && !NPCID.Sets.IsTownPet[entity.type]
        && !NPCID.Sets.IsTownSlime[entity.type]
        && entity.type != NPCID.OldMan
        && entity.type != NPCID.TravellingMerchant;

    public override void Load() {
        TownNPCCombatModule.Load();
    }

    public override void Unload() {
        if (talkBlinkOverlays is null) {
            return;
        }

        Main.QueueMainThreadAction(
            () => {
                foreach ((Texture2D talkTexture, Texture2D blinkTexture) in talkBlinkOverlays.Values) {
                    talkTexture.Dispose();
                    blinkTexture.Dispose();
                }
            }
        );
    }

    public override void SetStaticDefaults() {
        List<TownNPCAIState> states = ModContent.GetContent<TownNPCAIState>().ToList();

        if (states.Count != states.DistinctBy(state => state.ReservedStateInteger).Count()) {
            throw new Exception("Multiple TownNPCAIState instances with the same ReservedStateInteger");
        }

        // Must be loaded due to tML refreshing the happiness database in-between Load and SetStaticDefaults.
        TownNPCMoodModule.Load();

        _stateDict = states.ToDictionary(state => state.ReservedStateInteger);
        //_allActivities = states.OfType<TownNPCActivity>().ToList();
    }

    public override GlobalNPC NewInstance(NPC target) {
        TownGlobalNPC instance = (TownGlobalNPC)base.NewInstance(target);

        instance.PathfinderModule = new TownNPCPathfinderModule(target);
        instance.MoodModule = new TownNPCMoodModule(target);
        instance.CombatModule = new TownNPCCombatModule(target);

        if (talkBlinkOverlays is null || Main.netMode == NetmodeID.Server || !talkBlinkOverlays.ContainsKey(target.type)) {
            instance.ChatModule = new TownNPCChatModule(target, null);
            instance.SpriteModule = new TownNPCSpriteModule(target, null);
        }
        else {
            instance.ChatModule = new TownNPCChatModule(target, talkBlinkOverlays[target.type].Item1);
            instance.SpriteModule = new TownNPCSpriteModule(target, talkBlinkOverlays[target.type].Item2);
        }

        instance.HousingModule = new TownNPCHousingModule(target);
        instance.CollisionModule = new TownNPCCollisionModule(target);
        instance._lastActivities = new ForgetfulArray<TownNPCActivity>(LastActivityMemoryLimit);

        return instance;
    }

    public override bool PreAI(NPC npc) {
        npc.aiStyle = -1;
        return true;
    }

    public override void AI(NPC npc) {
        SetMiscNPCFields(npc);

        SpriteModule.Update();
        ChatModule.Update();
        CombatModule.Update();
        HousingModule.Update();

        if (_stateDict.TryGetValue((int)npc.ai[0], out TownNPCAIState state)) {
            state.DoState(this, npc);

            /*
            // New activities can only be selected when the npc is in the default state
            if (state is DefaultAIState && Main.rand.Next(_allActivities.SkipWhile(_lastActivities.Contains).ToList()) is { } activity && activity.CanDoActivity(this, npc)) {
                _lastActivities.Add(activity);
                activity.InitializeActivity(npc);

                return;
            }*/
        }
        else {
            RefreshToState<DefaultAIState>(npc);
        }

        MoodModule.Update();
        PathfinderModule.Update();
    }

    public override void PostAI(NPC npc) {
        // To make vanilla still draw extras properly
        npc.aiStyle = NPCAIStyleID.Passive;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) {
        PathfinderModule.SendNetworkData(bitWriter, binaryWriter);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) {
        PathfinderModule.ReceiveNetworkData(bitReader, binaryReader);
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        if (_stateDict.TryGetValue((int)npc.ai[0], out TownNPCAIState state)) {
            state.PreDrawNPC(this, npc, spriteBatch, screenPos, drawColor);
        }

        return true;
    }

    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        if (_stateDict.TryGetValue((int)npc.ai[0], out TownNPCAIState state)) {
            state.PostDrawNPC(this, npc, spriteBatch, screenPos, drawColor);
        }

        SpriteModule.DrawNPCOverlays(spriteBatch, screenPos, drawColor);
        ChatModule.DoChatDrawing(spriteBatch, screenPos, drawColor);

        if (!LWM.IsDebug) {
            return;
        }

        PathfinderModule.DebugDrawPath(spriteBatch, screenPos);
        HousingModule.DebugDraw(spriteBatch);
    }

    public override void FindFrame(NPC npc, int frameHeight) {
        SpriteModule.FrameNPC(frameHeight);
        if (_stateDict.TryGetValue((int)npc.ai[0], out TownNPCAIState state)) {
            state.FrameNPC(this, npc, frameHeight);
        }
    }

    private void SetMiscNPCFields(NPC npc) {
        npc.dontTakeDamage = false;
        npc.rotation = 0f;
        NPC.ShimmeredTownNPCs[npc.type] = npc.IsShimmerVariant;
        if (npc.HasBuff(BuffID.Shimmer)) {
            PathfinderModule.CancelPathfind();
        }

        if (npc.type == NPCID.SantaClaus && Main.netMode != NetmodeID.MultiplayerClient && !Main.xMas) {
            npc.StrikeInstantKill();
            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, 9999f);
            }
        }

        switch (npc.type) {
            case NPCID.Golfer:
                NPC.savedGolfer = true;
                break;
            case NPCID.TaxCollector:
                NPC.savedTaxCollector = true;
                break;
            case NPCID.GoblinTinkerer:
                NPC.savedGoblin = true;
                break;
            case NPCID.Wizard:
                NPC.savedWizard = true;
                break;
            case NPCID.Mechanic:
                NPC.savedMech = true;
                break;
            case NPCID.Stylist:
                NPC.savedStylist = true;
                break;
            case NPCID.Angler:
                NPC.savedAngler = true;
                break;
            case NPCID.DD2Bartender:
                NPC.savedBartender = true;
                break;
        }

        if (npc.type == NPCID.TaxCollector) {
            NPC.taxCollector = true;
        }

        npc.directionY = -1;
        if (npc.direction == 0) {
            npc.direction = 1;
        }

        if (npc.velocity.Y == 0f && !PathfinderModule.IsPathfinding) {
            npc.velocity *= 0.75f;
        }

        if (npc.type != NPCID.Mechanic) {
            return;
        }

        int wrenchWhoAmI = NPC.lazyNPCOwnedProjectileSearchArray[npc.whoAmI];
        bool wrenchFound = false;

        if (Main.projectile.IndexInRange(wrenchWhoAmI)) {
            Projectile projectile = Main.projectile[wrenchWhoAmI];
            if (projectile.active && projectile.type == 582 && projectile.ai[1] == npc.whoAmI) {
                wrenchFound = true;
            }
        }

        npc.localAI[0] = wrenchFound.ToInt();
    }
}