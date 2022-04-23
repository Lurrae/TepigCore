using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedItem
{
	public abstract class ModWhip : ModItem
	{
		public override void SetStaticDefaults()
		{
			TepigCore.ResearchAmt(Type, 1);
			ItemID.Sets.SummonerWeaponThatScalesWithAttackSpeed[Type] = true;
		}

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
			// This allows whip defaults to be used
			WhipSetDefaults();

			// This sets up this weapon as a whip
			// It automatically sets almost every necessary value for the weapon, we just need to put a value and rarity
			Item.DefaultToWhip(Item.shoot, Damage, KnockBack, ShootSpeed, UseTime);
		}

		// This code is VERY necessary, because without it whips will begin to spawn duplicate projectiles when used with autoswing
		public override bool CanUseItem(Player player)
		{
			return player.ownedProjectileCounts[Item.shoot] < 1;
		}
	}
}