﻿using LivingWorldMod.Globals.BaseTypes.Items;
using LivingWorldMod.Globals.Systems;
using Microsoft.Xna.Framework;

namespace LivingWorldMod.Content.Villages.HarpyVillage.Tiles.Torches;

public class StarTorchItem : BaseItem {
    public override void SetStaticDefaults() {
        Item.ResearchUnlockCount = 30;

        ItemID.Sets.Torches[Type] = true;
        ItemID.Sets.WaterTorches[Type] = true;
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ShimmerTorch;
    }

    public override void SetDefaults() {
        Item.CloneDefaults(ItemID.IchorTorch);
        Item.rare = ItemRarityID.Blue;
        Item.placeStyle = 0;
        Item.value = Item.buyPrice(silver: 5);
        Item.createTile = ModContent.TileType<StarTorchTile>();
        Item.flame = false;
    }

    public override void HoldItem(Player player) {
        if (Main.rand.NextBool(player.itemAnimation > 0 ? 40 : 80)) {
            Dust.NewDust(new Vector2(player.itemLocation.X + 16f * player.direction, player.itemLocation.Y - 14f * player.gravDir), 4, 4, DustID.YellowStarDust);
        }
        Vector2 position = player.RotatedRelativePoint(new Vector2(player.itemLocation.X + 12f * player.direction + player.velocity.X, player.itemLocation.Y - 14f + player.velocity.Y), true);
        Lighting.AddLight(position, BlockLightSystem.Instance.starTorchColor.ToVector3());
    }

    public override void PostUpdate() {
        Lighting.AddLight(new Vector2((Item.position.X + Item.width / 2f) / 16f, (Item.position.Y + Item.height / 2f) / 16f), BlockLightSystem.Instance.starTorchColor.ToVector3());
    }

    public override void AddRecipes() {
        CreateRecipe(3)
            .AddIngredient(ItemID.FallenStar)
            .AddIngredient(ItemID.Torch, 3)
            .Register();
    }
}