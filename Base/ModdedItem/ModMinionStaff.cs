using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;
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
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
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
			SetMinionStaffDefaults();

			Item.DamageType = DamageClass.Summon;
			Item.crit = -4; // Minions can't crit
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.useTime = Item.useAnimation = 30;
			Item.buffType = BuffType<MBuff>();
			Item.shoot = ProjectileType<MProj>();
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
			if (player.altFunctionUse != 2) // Right-clicking does not summon a minion
			{
				player.AddBuff(BuffType<MBuff>(), 3600);
				player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
			}

			return false;
		}
	}

	public abstract class TieredMinionStaff<MBuff, MProj, MCounter> : ModMinionStaff<MBuff, MProj> where MBuff : ModBuff where MProj : ModProjectile where MCounter : ModProjectile
	{
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (player.altFunctionUse != 2) // Still don't want to summon a minion when right-clicking
			{
				player.AddBuff(BuffType<MBuff>(), 3600);
				player.SpawnMinionOnCursor(source, player.whoAmI, ProjectileType<MCounter>(), Item.damage, knockback);

				if (player.ownedProjectileCounts[ProjectileType<MProj>()] == 0)
				{
					player.SpawnMinionOnCursor(source, player.whoAmI, ProjectileType<MProj>(), Item.damage, knockback);
				}
			}

			return false;
		}
	}
}