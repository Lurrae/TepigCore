using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedItem
{
	public abstract class ModRocketLauncher : ModItem
	{
		public virtual Dictionary<int, int> AmmoTypes()
		{
			return new Dictionary<int, int>()
			{
				{ ItemID.RocketI, ProjectileID.RocketI },
				{ ItemID.RocketII, ProjectileID.RocketII },
				{ ItemID.RocketIII, ProjectileID.RocketIII },
				{ ItemID.RocketIV, ProjectileID.RocketIV },
				{ ItemID.ClusterRocketI, ProjectileID.ClusterRocketI },
				{ ItemID.ClusterRocketII, ProjectileID.ClusterRocketII },
				{ ItemID.MiniNukeI, ProjectileID.MiniNukeRocketI },
				{ ItemID.MiniNukeII, ProjectileID.MiniNukeRocketII },
				{ ItemID.DryRocket, ProjectileID.DryRocket },
				{ ItemID.WetRocket, ProjectileID.WetRocket },
				{ ItemID.HoneyRocket, ProjectileID.HoneyRocket },
				{ ItemID.LavaRocket, ProjectileID.LavaRocket }
			};
		}

		public virtual float ShootSpeed() { return 10f; }
		public virtual int UseTime() { return 30; }
		public virtual bool AutoReuse() { return false; }

		public virtual void LauncherStaticDefaults()
		{
			// AmmoID.Sets.SpecificLauncherAmmoProjectileMatches MUST be added by each individual rocket launcher! Otherwise things will break fsr
		}

		public sealed override void SetStaticDefaults()
		{
			// See localization files for name and tooltip
			LauncherStaticDefaults();

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}
		
		public virtual void SetLauncherDefaults()
		{
			// Set width, height, damage, knockback, value, rarity, use sound, etc. here
		}

		public sealed override void SetDefaults()
		{
			Item.DefaultToRangedWeapon(ProjectileID.RocketI, AmmoID.Rocket, UseTime(), ShootSpeed(), AutoReuse());
			
			SetLauncherDefaults();
		}
	}
}