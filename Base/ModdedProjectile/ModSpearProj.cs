using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModSpearProj : ModProjectile
	{
		public virtual float HoldoutRangeMin => 24;
		public virtual float HoldoutRangeMax => 96;

		public sealed override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.Spear);
			SetSpearDefaults();
		}

		public virtual void SetSpearDefaults() { }

		public virtual void ExtraSpearAI() { }

		float Progress
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		// Code copied from ExampleMod's ExampleSpearProjectile
		public sealed override bool PreAI()
		{
			Player player = Main.player[Projectile.owner]; // Since we access the owner player instance so much, it's useful to create a helper local variable for this
			int duration = player.itemAnimationMax; // Define the duration the projectile will exist in frames

			player.heldProj = Projectile.whoAmI; // Update the player's held projectile id

			// Reset projectile time left if necessary
			if (Projectile.timeLeft > duration)
			{
				Projectile.timeLeft = duration;
			}

			Projectile.velocity = Vector2.Normalize(Projectile.velocity); // Velocity isn't used in this spear implementation, but we use the field to store the spear's attack direction

			float halfDuration = duration * 0.5f;
			
			// Here 'progress' is set to a value that goes from 0.0 to 1.0 and back during the item use animation.
			if (Projectile.timeLeft < halfDuration)
			{
				Progress = Projectile.timeLeft / halfDuration;
			}
			else
			{
				Progress = (duration - Projectile.timeLeft) / halfDuration;
			}

			// Move the projectile from the HoldoutRangeMin to the HoldoutRangeMax and back, using SmoothStep for easing the movement
			Projectile.Center = player.MountedCenter + Vector2.SmoothStep(Projectile.velocity * HoldoutRangeMin, Projectile.velocity * HoldoutRangeMax, Progress);

			// Apply proper rotation to the sprite
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Projectile.spriteDirection == -1)
			{
				// If sprite is facing left, rotate 45 degrees
				Projectile.rotation += MathHelper.ToRadians(45f);
			}
			else
			{
				// If sprite is facing right, rotate 135 degrees
				Projectile.rotation += MathHelper.ToRadians(135f);
			}

			ExtraSpearAI();

			return false; // Don't execute vanilla AI
		}


		// Code copied/adapted from vanilla's Main.DrawProj_Spear() in Main.cs, line 35126
		// This had to be done to support animated spears such as the Lurrenium Spear from my content mod
		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects dir = SpriteEffects.None;
			float num = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 2.355f;
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Player player = Main.player[Projectile.owner];
			Rectangle value = tex.Frame(1, Main.projFrames[Type], frameY: Projectile.frame);
			Vector2 zero = Vector2.Zero;
			
			if (player.direction > 0)
			{
				dir = SpriteEffects.FlipHorizontally;
				zero.X = tex.Width;
				num -= MathHelper.PiOver2;
			}

			if (player.gravDir == -1f)
			{
				if (Projectile.direction == 1)
				{
					dir = (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
					zero = new Vector2(tex.Width, tex.Height / Main.projFrames[Type]);
					num -= MathHelper.PiOver2;
				}
				else if (Projectile.direction == -1)
				{
					dir = SpriteEffects.FlipVertically;
					zero = new Vector2(0f, tex.Height / Main.projFrames[Type]);
					num += MathHelper.PiOver2;
				}
			}

			Vector2.Lerp(zero, value.Center.ToVector2(), 0.25f);
			Vector2 vector = Projectile.Center + new Vector2(0f, Projectile.gfxOffY);

			Main.EntitySpriteDraw(tex, vector - Main.screenPosition, value, Projectile.GetAlpha(lightColor), num, zero, Projectile.scale, dir, 0f);
			return false;
		}
	}
}