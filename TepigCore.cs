using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TepigCore
{
	public class TepigCore : Mod
	{
		public override void AddRecipeGroups()
		{
			RecipeGroup group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.DemoniteBar"), new int[]
			{
				ItemID.DemoniteBar,
				ItemID.CrimtaneBar
			});
			RecipeGroup.RegisterGroup("TepigCore:EvilBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.ShadowScale"), new int[]
			{
				ItemID.ShadowScale,
				ItemID.TissueSample
			});
			RecipeGroup.RegisterGroup("TepigCore:EvilBossDrops", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.CopperBar"), new int[]
			{
				ItemID.CopperBar,
				ItemID.TinBar
			});
			RecipeGroup.RegisterGroup("TepigCore:CopperBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.SilverBar"), new int[]
			{
				ItemID.SilverBar,
				ItemID.TungstenBar
			});
			RecipeGroup.RegisterGroup("TepigCore:SilverBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.GoldBar"), new int[]
			{
				ItemID.GoldBar,
				ItemID.PlatinumBar
			});
			RecipeGroup.RegisterGroup("TepigCore:GoldBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.CobaltBar"), new int[]
			{
				ItemID.CobaltBar,
				ItemID.PalladiumBar
			});
			RecipeGroup.RegisterGroup("TepigCore:CobaltBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.MythrilBar"), new int[]
			{
				ItemID.MythrilBar,
				ItemID.OrichalcumBar
			});
			RecipeGroup.RegisterGroup("TepigCore:MythrilBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("ItemName.AdamantiteBar"), new int[]
			{
				ItemID.AdamantiteBar,
				ItemID.TitaniumBar
			});
			RecipeGroup.RegisterGroup("TepigCore:TitaniumBars", group);

			group = new RecipeGroup(() => Language.GetTextValue("Mods.TepigCore.RecipeGroup.Gems"), new int[]
			{
				ItemID.Amethyst,
				ItemID.Topaz,
				ItemID.Sapphire,
				ItemID.Emerald,
				ItemID.Ruby,
				ItemID.Diamond,
				ItemID.Amber
			});
			RecipeGroup.RegisterGroup("TepigCore:Gems", group);
		}
	}
}