﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace LivingWorldMod.Content.UI.Elements {

    /// <summary>
    /// A better version of Vanilla's UIItemIcon class. Can use position or the center to draw from,
    /// and has hover tooltip functionality.
    /// </summary>
    public class UIBetterItemIcon : UIElement {
        public readonly int context = ItemSlot.Context.InventoryItem;

        /// <summary>
        /// Whether or not this element is currently visible, which is to say, whether or not it
        /// will be drawn. Defaults to true.
        /// </summary>
        public bool isVisible = true;

        private Item displayedItem;
        private float sizeLimit;
        private bool drawFromCenter;

        public UIBetterItemIcon(Item displayedItem, float sizeLimit, bool drawFromCenter) {
            this.displayedItem = displayedItem;
            this.sizeLimit = sizeLimit;
            this.drawFromCenter = drawFromCenter;
        }

        public void SetItem(Item newItem) {
            displayedItem = newItem;
            Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            if (!isVisible) {
                return;
            }

            //Adapted Vanilla Code
            Main.instance.LoadItem(displayedItem.type);

            Texture2D itemTexture = TextureAssets.Item[displayedItem.type].Value;
            Rectangle itemAnimFrame = (Main.itemAnimations[displayedItem.type] == null) ? itemTexture.Frame() : Main.itemAnimations[displayedItem.type].GetFrame(itemTexture);

            Color currentColor = Color.White;
            float itemLightScale = 1f;
            float sizeConstraint = 1f;

            ItemSlot.GetItemLight(ref currentColor, ref itemLightScale, displayedItem);
            sizeConstraint *= itemLightScale;

            if (itemAnimFrame.Width > sizeLimit || itemAnimFrame.Height > sizeLimit) {
                sizeConstraint = itemAnimFrame.Width <= itemAnimFrame.Height ? sizeLimit / itemAnimFrame.Height : sizeLimit / itemAnimFrame.Width;
            }

            sizeConstraint *= displayedItem.scale;

            spriteBatch.Draw(itemTexture,
                drawFromCenter ? GetDimensions().Center() : GetDimensions().Position(),
                itemAnimFrame,
                currentColor,
                0f,
                drawFromCenter ? new Vector2(itemAnimFrame.Width / 2f, itemAnimFrame.Height / 2f) : default,
                sizeConstraint,
                SpriteEffects.None,
                0f);

            //Non-vanilla code
            if (ContainsPoint(Main.MouseScreen)) {
                ItemSlot.MouseHover(ref displayedItem, context);
            }
        }
    }
}