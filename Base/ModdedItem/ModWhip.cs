using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedItem
{
	public abstract class ModWhip : ModItem
	{
		public abstract int Damage { get; }
		public abstract float KnockBack { get; }
		public abstract float ShootSpeed { get; }
		public abstract int UseTime { get; } // Default should be 30

		public virtual void WhipSetDefaults()
		{
			// Things specific to an individual whip- like rarity or buy/sell price- will go here
			// Also Item.shoot MUST be set, otherwise the whip won't work at all
		}

		public override void SetDefaults() 
		{
			// This sets up this weapon as a whip
			// It automatically sets almost every necessary value for the weapon, we just need to put a value and rarity
			// Item.shoot is set in WhipSetDefaults(), so we don't need it here
			Item.DefaultToWhip(ProjectileID.None, Damage, KnockBack, ShootSpeed, UseTime);

			// This allows whip defaults to be used
			// Anything set by Item.DefaultToWhip() can be overridden here
			WhipSetDefaults();
		}

		// This code is VERY necessary, because without it whips will begin to spawn duplicate projectiles when used with autoswing
		public override bool CanUseItem(Player player)
		{
			return player.ownedProjectileCounts[Item.shoot] < 1;
		}
	}
}