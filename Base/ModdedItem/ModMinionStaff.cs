using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using static Terraria.ModLoader.ModContent;

namespace TepigCore.Base.ModdedItem
{
	public abstract class ModMinionStaff<MBuff, MProj> : ModItem where MBuff : ModBuff where MProj : ModProjectile
	{
		public virtual void StaticMinionStaffDefaults()
		{
			// Not sure whether anything will need to go here or not, but just in case
		}

		public sealed override void SetStaticDefaults()
		{
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true; // Allows players using a controller to spawn the minion easily
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;

			StaticMinionStaffDefaults();
		}
		
		public virtual void SetMinionStaffDefaults()
		{
			// Things that are set per-summon item go here
		}

		public sealed override void SetDefaults()
		{
			Item.DefaultToMinion(BuffType<MBuff>(), ProjectileType<MProj>(), 30);

			SetMinionStaffDefaults();
		}

		// Enable right-clicking so the player can use the minion targeting feature with this item
		public override bool AltFunctionUse(Player player)
		{
			return true;
		}

		// Set the player's minion target on right-click
		public override bool? UseItem(Player player)
		{
			if (player.altFunctionUse == 2)
				player.MinionNPCTargetAim(false);
			
			return base.UseItem(player);
		}

		// Spawn our minion
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(BuffType<MBuff>(), 3600);
			player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);

			return false;
		}
	}

	public abstract class TieredMinionStaff<MBuff, MProj> : ModItem where MBuff : ModBuff where MProj : ModProjectile
	{
		public virtual void StaticMinionStaffDefaults()
		{
			// Not sure whether anything will need to go here or not, but just in case
		}

		public sealed override void SetStaticDefaults()
		{
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true; // Allows players using a controller to spawn the minion easily
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;

			StaticMinionStaffDefaults();
		}

		public virtual void SetMinionStaffDefaults()
		{
			// Things that are set per-summon item go here
		}

		public sealed override void SetDefaults()
		{
			Item.DamageType = DamageClass.Summon;
			Item.crit = -4; // Minions can't crit
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.useTime = Item.useAnimation = 30;
			Item.buffType = BuffType<MBuff>();
			Item.shoot = ProjectileType<MProj>();

			SetMinionStaffDefaults();
		}

		// Enable right-clicking so the player can use the minion targeting feature with this item
		public override bool AltFunctionUse(Player player)
		{
			return true;
		}

		// Set the player's minion target on right-click
		public override bool? UseItem(Player player)
		{
			if (player.altFunctionUse == 2)
			{
				player.MinionNPCTargetAim(false);
			}

			return base.UseItem(player);
		}

		// Spawn our minion
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// Find all active minions to find out how many minion slots are available
			float slotsTaken = 0;
			foreach (Projectile proj in Main.projectile)
			{
				if (proj.minionSlots > 0 && proj.active && proj.owner == player.whoAmI)
				{
					slotsTaken += proj.minionSlots;
				}
			}
			
			float slotsLeft = player.maxMinions - slotsTaken;

			// Find our projectile and increase its tier
			foreach (Projectile proj in Main.projectile)
			{
				if (proj.type == type && proj.active && proj.owner == player.whoAmI)
				{
					if (slotsLeft >= 1) // We need to have a slot left to add a tier to our minion
					{
						SoundEngine.PlaySound(SoundID.AbigailUpgrade, player.Center);
						proj.minionSlots++;
					}
					return false; // We already found our projectile and added a tier to it, so we can return now
				}
			}

			// Add the buff and spawn the minion
			player.AddBuff(BuffType<MBuff>(), 3600);
			player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);

			return false;
		}
	}
}