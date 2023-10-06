using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModMinion<MBuff> : ModProjectile where MBuff : ModBuff
	{
		public abstract bool IsTrueMinion { get; } // Does this minion take up minion slots?
		public virtual float MinionSlots() { return 1f; } // How many minion slots does this minion take up? Unused if IsTrueMinion is false
		public virtual void StaticMinionDefaults()
		{
			// This is basically only used for projFrames
		}

		public sealed override void SetStaticDefaults() 
		{
			Main.projPet[Type] = true;

			ProjectileID.Sets.CultistIsResistantTo[Type] = true; // Reduced damage against Lunatic Cultist, because all homing projectiles and minions do that
			ProjectileID.Sets.MinionTargettingFeature[Type] = IsTrueMinion; // "True" minions' weapons usually utilize the minion targetting feature
			ProjectileID.Sets.MinionSacrificable[Type] = IsTrueMinion; // "True" minions also require minion slots to function, and this automatically handles all of that

			StaticMinionDefaults();
		}
		
		public virtual void SetMinionDefaults()
		{
			// This is where things like tileCollide and aiStyle can go
		}

		public sealed override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;

			if (IsTrueMinion) // Allows "true" minions to take up minion slots
			{
				Projectile.minion = true;
				Projectile.minionSlots = MinionSlots();
			}

			SetMinionDefaults();
		}
		
		public sealed override void AI() // Sealed override means that anything inheriting this class cannot overwrite the base AI
		{
			Player player = Main.player[Projectile.owner];

			if (!CheckActive(player))
				return; // Don't run AI if the player is dead/nonexistent

			MinionAI(); // Allows custom AI to be run per-minion
		}

		public virtual void MinionAI()
		{
			// Any custom AI will go here
		}
		
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(BuffType<MBuff>());
				return false;
			}

			if (owner.HasBuff(BuffType<MBuff>()))
				Projectile.timeLeft = 2; // Gives minions infinite duration, while still allowing it to despawn almost instantly after the player loses the buff

			return true;
		}
	}

	public abstract class TieredMinion<MBuff> : ModProjectile where MBuff : ModBuff
	{
		public virtual void StaticMinionDefaults()
		{
			// This is basically only used for projFrames
		}

		public sealed override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;

			ProjectileID.Sets.CultistIsResistantTo[Type] = true; // Reduced damage against Lunatic Cultist, because all homing projectiles and minions do that
			ProjectileID.Sets.MinionTargettingFeature[Type] = true; // Tiered minions' weapons usually utilize the minion targetting feature, just like "true" minions
			ProjectileID.Sets.MinionSacrificable[Type] = true; // Tiered minions can be replaced by other minions

			StaticMinionDefaults();
		}

		public virtual void SetMinionDefaults()
		{
			// This is where things like tileCollide and aiStyle can go
		}

		public sealed override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;

			Projectile.minion = true;
			Projectile.minionSlots = 1;

			SetMinionDefaults();
		}

		public sealed override void AI() // Sealed override means that anything inheriting this class cannot overwrite the base AI
		{
			Player player = Main.player[Projectile.owner];

			if (!CheckActive(player))
				return; // Don't run AI if the player is dead/nonexistent

			MinionAI(); // Allows custom AI to be run per-minion
		}

		public virtual void MinionAI()
		{
			// Any custom AI will go here
		}

		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(BuffType<MBuff>());
				return false;
			}

			if (owner.HasBuff(BuffType<MBuff>()))
				Projectile.timeLeft = 2; // Gives minions infinite duration, while still allowing it to despawn almost instantly after the player loses the buff

			return true;
		}

		public virtual bool ShouldBeActive()
		{
			return false;
		}

		public override void OnKill(int timeLeft) // Re-summon the minion if it dies prematurely
		{
			Player player = Main.player[Projectile.owner];

			if (ShouldBeActive())
			{
				Projectile newP = Main.projectile[player.SpawnMinionOnCursor(new EntitySource_Parent(Projectile), player.whoAmI, Type, Projectile.originalDamage, Projectile.knockBack)];
				newP.minionSlots += player.maxMinions - player.slotsMinions - 1;
				newP.rotation = Projectile.rotation;
			}
		}
	}
}