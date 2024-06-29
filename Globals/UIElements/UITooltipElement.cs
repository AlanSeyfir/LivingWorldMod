﻿using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.UI;

namespace LivingWorldMod.Globals.UIElements;

public class UITooltipElement(LocalizedText tooltipText, params object[] formatObjects) : UIElement {
    private static readonly Item DummyItem = new(ItemID.None, 0) {
        // Can be any item - just can't be 0 (otherwise the tooltip won't draw)
        type = ItemID.IronPickaxe
    };

    private string _formattedTooltipText = tooltipText.Format(formatObjects);

    public void SetText(LocalizedText newText, params object[] formatObjects) {
        tooltipText = newText;
        _formattedTooltipText = newText.Format(formatObjects);
    }

    public void ReformatText(params object[] formatObjects) {
        _formattedTooltipText = tooltipText.Format(formatObjects);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);

        if (!ContainsPoint(Main.MouseScreen)) {
            return;
        }

        DummyItem.SetNameOverride(_formattedTooltipText);
        Main.HoverItem = DummyItem;
        Main.instance.MouseText("");
        Main.mouseText = true;
    }
}