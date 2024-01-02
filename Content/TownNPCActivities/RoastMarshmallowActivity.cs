using System;
using System.Collections.Generic;
using System.Linq;
using LivingWorldMod.Common.GlobalNPCs;
using LivingWorldMod.Common.ModTypes;
using LivingWorldMod.Custom.Classes;
using LivingWorldMod.Custom.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace LivingWorldMod.Content.TownNPCActivities {
    /// <summary>
    /// Activity where the given NPC will look for the nearest campfire and try to roast
    /// a marshmallow over it.
    /// </summary>
    public class RoastMarshmallowActivity : TownNPCActivity {
        public override int ReservedStateInteger => 26;

        private Point _standingLocation;
        private bool _isAtStandingLocation;

        public override void FrameNPC(TownAIGlobalNPC globalNPC, NPC npc, int frameHeight) {
            if (!_isAtStandingLocation) {
                return;
            }

            npc.frame.Y = frameHeight * 17;
        }

        public override void PostDrawNPC(TownAIGlobalNPC globalNPC, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!_isAtStandingLocation) {
                return;
            }

            Main.instance.LoadItem(ItemID.MarshmallowonaStick);
            spriteBatch.Draw(
                TextureAssets.Item[ItemID.MarshmallowonaStick].Value,
                npc.Center - screenPos + new Vector2(10f, -8f),
                drawColor
            );
        }

        public override void DoState(TownAIGlobalNPC globalNPC, NPC npc) {
            if ((npc.BottomLeft + new Vector2(0, -2)).ToTileCoordinates() == _standingLocation) {
                _isAtStandingLocation = true;
                npc.direction = 1;
                return;
            }

            globalNPC.PathfinderModule.RequestPathfind(_standingLocation, null);
            _isAtStandingLocation = false;
        }

        public override bool CanDoActivity(TownAIGlobalNPC globalNPC, NPC npc) {
            Point origin = globalNPC.PathfinderModule.TopLeftOfPathfinderZone;
            List<Point> campfires = new();
            for (int i = origin.X; i < origin.X + TownNPCPathfinderModule.PathFinderZoneSideLength; i++) {
                for (int j = origin.Y; j < origin.Y + TownNPCPathfinderModule.PathFinderZoneSideLength; j++) {
                    Tile tile = Main.tile[i, j];
                    if (!TileID.Sets.Campfire[tile.TileType]) {
                        continue;
                    }

                    Point bottomLeftOfCampfire = TileUtils.GetCornerOfMultiTile(tile, i, j, TileUtils.CornerType.BottomLeft);
                    if (campfires.Contains(bottomLeftOfCampfire)) {
                        continue;
                    }

                    int npcTileWidth = (int)Math.Ceiling(npc.width / 16f);
                    int npcTileHeight = (int)Math.Ceiling(npc.height / 16f);

                    // Local var for formatting
                    bool canNPCFitInSpace = WorldUtils.Find(
                        new Point(bottomLeftOfCampfire.X - 2, bottomLeftOfCampfire.Y - npcTileHeight + 1),
                        Searches.Chain(
                            new Searches.Rectangle(npcTileWidth, npcTileHeight),
                            new Conditions.IsSolid().Not()
                        ),
                        out _
                    );

                    if (!canNPCFitInSpace) {
                        continue;
                    }

                    campfires.Add(bottomLeftOfCampfire);
                }
            }

            if (!campfires.Any()) {
                return false;
            }

            _standingLocation = campfires.MinBy(point => npc.Distance(point.ToWorldCoordinates(0f, 0f))) + new Point(-2, 0);
            return globalNPC.PathfinderModule.HasPath(_standingLocation);
        }
    }
}