using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModMinion : ModProjectile
	{
		public abstract int MinionBuff { get; } // The ID of the buff this minion is associated with
		public abstract bool ActiveHitbox { get; } // Does this minion damage things by passively walking/flying into them, or can it only damage things while aggroed?
		public abstract bool IsTrueMinion { get; } // Does this minion take up minion slots?
		public virtual float MinionSlots() { return 1f; } // How many minion slots does this minion take up? Unused if IsTrueMinion is false
		public virtual bool TieredMinion() { return false; } // If true, this minion gets stronger when re-summoned, similar to Abigail or the Desert Tiger

		public virtual void StaticMinionDefaults()
		{
			// This is basically only used for projFrames
		}

		public sealed override void SetStaticDefaults() 
		{
			StaticMinionDefaults();

			Main.projPet[Type] = true;

			ProjectileID.Sets.CultistIsResistantTo[Type] = true; // Reduced damage against Lunatic Cultist, because all homing projectiles and minions do that

			ProjectileID.Sets.MinionTargettingFeature[Type] = IsTrueMinion; // "True" Minions will prioritize targets which the player has right-clicked or hit with a whip
			ProjectileID.Sets.MinionSacrificable[Type] = IsTrueMinion; // "True" Minions also require minion slots to function, and this automatically handles all of that

			if (TieredMinion()) // Tiered Minions are "true" minions, but don't count towards the minion count
				ProjectileID.Sets.MinionSacrificable[Type] = false;
		}
		
		public virtual void SetMinionDefaults()
		{
			// This is where things like tileCollide and aiStyle can go
		}

		public sealed override void SetDefaults()
		{
			SetMinionDefaults();

			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;

			if (IsTrueMinion && !TieredMinion()) // Allows "true", non-tiered minions to take up minion slots
			{
				Projectile.minion = true;
				Projectile.minionSlots = MinionSlots();
			}
		}

		public override bool MinionContactDamage() 
		{
			return ActiveHitbox;
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
				owner.ClearBuff(MinionBuff);
				return false;
			}

			if (owner.HasBuff(MinionBuff))
				Projectile.timeLeft = 2; // Gives minions infinite duration, while still allowing it to despawn almost instantly after the player loses the buff

			return true;
		}
	}
}