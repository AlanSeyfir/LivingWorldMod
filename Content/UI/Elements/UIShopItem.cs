﻿using LivingWorldMod.Common.Systems.UI;
using LivingWorldMod.Custom.Enums;
using LivingWorldMod.Custom.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace LivingWorldMod.Content.UI.Elements {

    /// <summary>
    /// UIImage class that holds different UITexts and UIElements that is the index in a given shop
    /// UI list. Holds data on the entire entry for the given item.
    /// </summary>
    public class UIShopItem : UIImage {
        public UIBetterItemIcon itemImage;
        public UIBetterText itemNameText;
        public UICoinDisplay itemCostDisplay;

        public Item displayedItem;

        public int remainingStock;
        public long costPerItem;

        public VillagerType villagerType;

        private float manualUpdateTime;

        public UIShopItem(int itemType, int remainingStock, long costPerItem, VillagerType villagerType) : base(ModContent.Request<Texture2D>($"{IOUtilities.LWMSpritePath}/UI/ShopUI/{villagerType}/ShopItemBox")) {
            displayedItem = new Item();
            displayedItem.SetDefaults(itemType);
            this.remainingStock = remainingStock;
            this.costPerItem = costPerItem;
            this.villagerType = villagerType;
        }

        public override void OnInitialize() {
            float itemImageSize = 32f;

            itemImage = new UIBetterItemIcon(displayedItem, itemImageSize, true) {
                VAlign = 0.5f
            };
            itemImage.Left.Set(38f, 0f);
            itemImage.Width.Set(itemImageSize, 0f);
            itemImage.Height.Set(itemImageSize, 0f);
            Append(itemImage);

            itemNameText = new UIBetterText(displayedItem.HoverName, 1.25f) {
                VAlign = 0.5f,
                horizontalTextConstraint = 194f
            };
            itemNameText.Left.Set(94f, 0f);
            Append(itemNameText);

            itemCostDisplay = new UICoinDisplay(costPerItem, CoinDrawStyle.NoCoinsWithZeroValue, 1.34f) {
                VAlign = 0.5f
            };
            itemCostDisplay.Left.Set(-itemCostDisplay.Width.Pixels - 12f, 1f);
            Append(itemCostDisplay);

            OnClick += OnShopItemClick;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            if (ContainsPoint(Main.MouseScreen)) {
                RasterizerState defaultRasterizerState = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };

                Effect shader = ShopUISystem.hoverFlashShader.Value;

                manualUpdateTime += 1f / 45f;
                if (manualUpdateTime >= MathHelper.TwoPi) {
                    manualUpdateTime = 0f;
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, defaultRasterizerState, shader, Main.UIScaleMatrix);

                //So I am unsure as to why exactly this needed to be done, cause this is definitely the definition of a band-aid fix.
                //In short, when using this shader, uTime isn't being updated at all, causing the shader to just stay one color instead of breathing in a sine wave fashion like intended.
                //Thus, for the time being, until I can figure out why uTime isn't being automatically updated, I am manually setting this new Parameter
                shader.Parameters["manualUTime"].SetValue(manualUpdateTime);
                base.DrawSelf(spriteBatch);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, defaultRasterizerState, null, Main.UIScaleMatrix);
            }
            else {
                base.DrawSelf(spriteBatch);
            }
        }

        private void OnShopItemClick(UIMouseEvent evt, UIElement listeningElement) {


        }
    }
}