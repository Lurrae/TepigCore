using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModPet<PBuff> : ModProjectile where PBuff : ModBuff
	{
		public virtual void StaticPetDefaults()
		{
			// This is basically only used for projFrames
		}

		public sealed override void SetStaticDefaults() 
		{
			StaticPetDefaults();

			Main.projPet[Type] = true;
		}
		
		public virtual void SetPetDefaults()
		{
			// This is where things like tileCollide and aiStyle can go
		}

		public sealed override void SetDefaults()
		{
			SetPetDefaults();

			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
		}

		public override bool? CanCutTiles()
		{
			return false;
		}

		public sealed override void AI() // Sealed override means that anything inheriting this class cannot overwrite the base AI
		{
			Player player = Main.player[Projectile.owner];

			if (!CheckActive(player))
				return; // Don't run AI if the player is dead/nonexistent

			PetAI(); // Allows custom AI to be run per-pet
		}

		public virtual void PetAI()
		{
			// Any custom AI will go here
		}
		
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active || !owner.HasBuff(BuffType<PBuff>()))
			{
				owner.ClearBuff(BuffType<PBuff>());
				return false;
			}

			Projectile.timeLeft = 2; // Gives pets infinite duration, while still allowing it to despawn almost instantly after the player loses the buff
			return true;
		}
	}
}