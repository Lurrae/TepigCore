using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;
using Terraria.DataStructures;

namespace TepigCore.Base.ModdedItem
{
	public abstract class ModMinionStaff : ModItem
	{
		public abstract int MinionBuff { get; } // The ID of the buff this item's minion is associated with
		public abstract int MinionProj { get; } // The ID of the minion projectile
		public virtual bool SpawnThroughWalls() { return true; } // Can this minion be spawned if the player doesn't have line of sight to it? In vanilla this defaults to true
		public virtual Vector2 SpawnPos() { return Main.MouseWorld; } // Where should the minion be summoned? Most minions in vanilla spawn at the mouse position, so that's the default here too
		public virtual bool TieredMinion() { return false; }
		public virtual int TieredMinionProj() { return 0; }

		public virtual void StaticMinionStaffDefaults()
		{
			// Not sure whether anything will need to go here or not, but just in case
		}

		public sealed override void SetStaticDefaults()
		{
			StaticMinionStaffDefaults();

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // Allows players using a controller to spawn the minion easily
		}
		
		public virtual void SetMinionStaffDefaults()
		{
			// Things that are set per-summon item go here
		}

		public sealed override void SetDefaults()
		{
			SetMinionStaffDefaults();

			Item.DamageType = DamageClass.Summon;
			Item.crit = 0; // Minions can't crit
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.useTime = Item.useAnimation = 30;
			Item.buffType = MinionBuff;
			Item.shoot = MinionProj;
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
				player.MinionNPCTargetAim(true);
			}
			// Stop player from spawning minions through walls if they shouldn't be
			if (!SpawnThroughWalls() && !Collision.CanHit(player.position, 1, 1, Main.MouseWorld, 1, 1))
				return false;

			return base.UseItem(player);
		}

		// Update the position the minion spawns at
		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			position = SpawnPos();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (player.altFunctionUse == 2) // Right-clicking does not summon a minion
			{
				return false;
			}

			// Apply the buff so the minion doesn't die instantly
			player.AddBuff(MinionBuff, 2);

			// If this is a tiered minion, spawn the counter for that minion
			// Also return early if we already have one of the main minions so that we don't spawn duplicates of that
			if (TieredMinion() && TieredMinionProj() != 0)
			{
				Projectile.NewProjectile(source, position, Vector2.Zero, TieredMinionProj(), 0, 0, player.whoAmI);
				//if (player.ownedProjectileCounts[type] > 0)
					//return false;
			}

			// Spawn the minion and set its original damage (so its damage can scale dynamically with damage boosts applied after it's summoned)
			Projectile proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;

			return false;
		}
	}
}