using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace TepigCore.Base.ModdedProjectile
{
	// This abstract class was adapted from Qwerty's mod's flail abstract class, which itself basically copy/pasted vanilla's AI_015 method with a few changes
	public enum FlailStateID
	{
		Channel,	// Currently spinning around player
		FlyOut,		// Player just stopped channeling and has launched this flail
		Return,		// Flail reached its max distance after FlyOut and is now returning
		Unused,
		FallReturn,	// Player dropped the flail and then let go of LMB
		BounceBack,	// Hit a tile while in FlyOut state, and bounced off the block
		Falling		// Player pressed LMB while in FlyOut, Return, or BounceBack state, and the flail has become stationary and heavily gravity-affected
	}

	public abstract class ModFlailProj : ModProjectile
	{
		public abstract string[] ChainTex { get; }

		public virtual void SetStats(ref int throwTime, ref float throwSpeed, ref float recoverDistance, ref float recoverDistance2, ref int hitCooldown, ref int channelHitCooldown)
		{
			// See the AI() field for explanations of what all of these variables do
		}

		public virtual void FlailDefaults()
		{

		}

		public sealed override void SetDefaults()
		{
			FlailDefaults();

			Projectile.DamageType = DamageClass.Melee;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.ContinuouslyUpdateDamageStats = true;
		}

		// Draw the chain from the player to the mace
		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 playerCenter = Main.player[Projectile.owner].MountedCenter;
			Vector2 center = Projectile.Center;
			Vector2 distToProj = playerCenter - Projectile.Center;
			float projRotation = distToProj.ToRotation() - MathHelper.PiOver2;
			float distance = distToProj.Length();
			int textureNum = 0;
			for (int i = 0; i < 1000; i++)
			{
				if (distance > 4f && !float.IsNaN(distance) && ChainTex.Length > 0) // Ensure the flail is far enough away from the player and that a chain texture was provided
				{
					textureNum++;
					if (textureNum >= ChainTex.Length)
						textureNum = 0;
					Texture2D chainTex = Request<Texture2D>(ChainTex[textureNum]).Value;

					distToProj.Normalize();
					distToProj *= chainTex.Height;
					center += distToProj;
					distToProj = playerCenter - center;
					distance = distToProj.Length();

					Color drawColor = lightColor;
					Rectangle sourceRect = new(0, 0, chainTex.Width, chainTex.Height);
					Vector2 origin = new(chainTex.Width * 0.5f, chainTex.Height * 0.5f);

					Main.EntitySpriteDraw(chainTex, new Vector2(center.X - Main.screenPosition.X, center.Y - Main.screenPosition.Y), sourceRect, drawColor, projRotation, origin, 1f, SpriteEffects.None, 0);
				}
			}

			return true;
		}

		public override void AI()
		{
			// Kill projectile if it goes too far away from the player, or if the player dies/disconnects or opens the fullscreen map
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead || player.noItems || player.CCed || Vector2.Distance(Projectile.Center, player.Center) > 900f)
			{
				Projectile.Kill();
				return;
			}
			if (Main.myPlayer == Projectile.owner && Main.mapFullscreen)
			{
				Projectile.Kill();
				return;
			}

			Vector2 playerCenter = player.MountedCenter;
			
			const float maxFlailDistance = 800f;	// Flails that are beyond this distance will instantly despawn
			float maxSpdPerFrame = 3f;				// The amount of distance the flail can travel every frame at most

			// Set the stats for this flail
			int throwDist = 10;				// When initially launched, how far does this flail go before beginning to retract?
			float throwSpeed = 24f;			// How much initial velocity the flail has when launched
			float recoverDistance = 16f;	// How close to the player must this flail be while retracting before it can despawn?
			float recoverDistance2 = 48f;	// Same as above, but for flails returning after falling
			int hitCooldown = 10;			// How long does it take before an enemy can be hit by this flail again?
			int channelHitCooldown = 20;	// Same as above, but applies only while channeling
			SetStats(ref throwDist, ref throwSpeed, ref recoverDistance, ref recoverDistance2, ref hitCooldown, ref channelHitCooldown);

			int bounceDist = throwDist + 5; // Handles how far the flail travels when bouncing off a block

			float meleeSpeedMod = 1f / player.GetAttackSpeed(DamageClass.Melee);
			throwSpeed *= meleeSpeedMod;
			maxSpdPerFrame *= meleeSpeedMod;
			recoverDistance *= meleeSpeedMod;
			recoverDistance2 *= meleeSpeedMod;
			float modifiedThrowSpd = throwSpeed * throwDist;
			float fallReturnDist = modifiedThrowSpd + 160f;
			
			Projectile.localNPCHitCooldown = hitCooldown;
			
			// Main AI handling
			// Each value that Projectile.ai[0] can be set to corresponds to a different state, hence why I set up an enum for that very purpose
			switch ((int)Projectile.ai[0])
			{
				case (int)FlailStateID.Channel: // Channeling flail, meaning it is circling around the player
				{
					if (Projectile.owner == Main.myPlayer)
					{
						Vector2 origin = playerCenter;
						Vector2 mouseWorld = Main.MouseWorld;
						Vector2 vectorToMouse = origin.DirectionTo(mouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
						player.ChangeDir((vectorToMouse.X > 0f) ? 1 : (-1));
						
						// Player let go, prepare to launch out	
						if (!player.channel)
						{
							Projectile.ai[0] = (int)FlailStateID.FlyOut;
							Projectile.ai[1] = 0f;
							Projectile.velocity = vectorToMouse * throwSpeed + player.velocity;
							Projectile.Center = playerCenter;
							Projectile.netUpdate = true;
							for (int k = 0; k < 200; k++)
							{
								Projectile.localNPCImmunity[k] = 0;
							}
							Projectile.localNPCHitCooldown = hitCooldown;
							break; // No more need to handle rotation, so don't bother running that code
						}
					}

					// localAI[1] handles the rotation of the flail around the player
					Projectile.localAI[1] += 1f;
					Vector2 rotationVector = new Vector2(player.direction).RotatedBy((float)Math.PI * 10f * (Projectile.localAI[1] / 60f) * player.direction);
					rotationVector.Y *= 0.8f;
					if (rotationVector.Y * player.gravDir > 0f)
					{
						rotationVector.Y *= 0.5f;
					}
					Projectile.Center = playerCenter + rotationVector * 30f;
					Projectile.velocity = Vector2.Zero; // We're handling movement manually here, so we don't need to deal with velocity
					Projectile.localNPCHitCooldown = channelHitCooldown;
					break;
				}
				case (int)FlailStateID.FlyOut: // Just launched out, but have not reached max distance or hit a block yet
				{
					bool atMaxRange = Projectile.ai[1]++ >= throwDist;
					atMaxRange |= Projectile.Distance(playerCenter) >= maxFlailDistance;
					if (player.controlUseItem) // Player pressed LMB, begin falling
					{
						Projectile.ai[0] = (int)FlailStateID.Falling;
						Projectile.ai[1] = 0f;
						Projectile.netUpdate = true;
						Projectile.velocity *= 0.2f;
						break;
					}
					if (atMaxRange) // Reached max range, go back to player
					{
						Projectile.ai[0] = (int)FlailStateID.Return;
						Projectile.ai[1] = 0f;
						Projectile.netUpdate = true;
						Projectile.velocity *= 0.3f;
						OnStartRetracting();
					}

					// Set player's direction and enemy i-frames
					player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
					Projectile.localNPCHitCooldown = hitCooldown;
					break;
				}
				case (int)FlailStateID.Return: // Flail reached its maximum distance and is now returning to the player
				{
					Vector2 value2 = Projectile.DirectionTo(playerCenter).SafeNormalize(Vector2.Zero);
					if (Projectile.Distance(playerCenter) <= recoverDistance) // Flail has returned to player, delete it
					{
						Projectile.Kill();
						return;
					}
					if (player.controlUseItem) // Player might not press LMB until after it starts returning, so we gotta account for that here
					{
						Projectile.ai[0] = (int)FlailStateID.Falling;
						Projectile.ai[1] = 0f;
						Projectile.netUpdate = true;
						Projectile.velocity *= 0.2f;
					}
					else // Pull flail towards player
					{
						Projectile.velocity *= 0.98f;
						Projectile.velocity = Projectile.velocity.MoveTowards(value2 * recoverDistance, maxSpdPerFrame);
						player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1)); // Player should always face flail
					}
					break;
				}
				case 3: // Unused
					break;
				case (int)FlailStateID.FallReturn: // Returning after having fallen, player can no longer press LMB to drop the flail again
				{
					Projectile.tileCollide = false; // Ignore blocks so we don't get obstructed on our way back
					Vector2 vector = Projectile.DirectionTo(playerCenter).SafeNormalize(Vector2.Zero);
					if (Projectile.Distance(playerCenter) <= recoverDistance2) // Flail is close enough to player to delete it again
					{
						Projectile.Kill();
						return;
					}

					// Pull flail towards player
					Projectile.velocity *= 0.98f;
					Projectile.velocity = Projectile.velocity.MoveTowards(vector * recoverDistance2, maxSpdPerFrame * 2f);
					
					Vector2 target = Projectile.Center + Projectile.velocity;
					Vector2 value = playerCenter.DirectionFrom(target).SafeNormalize(Vector2.Zero);
					if (Vector2.Dot(vector, value) < 0f) // I think this kills the flail if it isn't actually flying at an angle that will get it closer to the player? Not 100% sure tho
					{
						Projectile.Kill();
						return;
					}

					// Once again, player must face flail
					player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
					break;
				}
				case (int)FlailStateID.BounceBack: // Hit a block while flying out, should bounce back before returning to player
				{
					if (Projectile.ai[1]++ >= bounceDist) // Flail has bounced far enough, return to player
					{
						Projectile.ai[0] = (int)FlailStateID.Falling;
						Projectile.ai[1] = 0f;
						Projectile.netUpdate = true;
					}
					else // Allow the flail to bounce, and ensure that the player still faces towards it as usual
					{
						Projectile.localNPCHitCooldown = hitCooldown;
						Projectile.velocity.Y += 0.6f;
						Projectile.velocity.X *= 0.95f;
						player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
					}
					break;
				}
				case (int)FlailStateID.Falling: // Player pressed LMB while flail was flying out or returning, so it should fall
				{
					if (!player.controlUseItem || Projectile.Distance(playerCenter) > fallReturnDist) // Player is too far away, return flail to player
					{
						Projectile.ai[0] = (int)FlailStateID.FallReturn;
						Projectile.ai[1] = 0f;
						Projectile.netUpdate = true;
					}
					else // Flail is heavily affected by gravity and barely moves left/right, player still faces it tho
					{
						Projectile.velocity.Y += 0.8f;
						Projectile.velocity.X *= 0.95f;
						player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
					}
					break;
				}
			}
			
			// Handle setting the direction of the flail to face whatever direction it's moving in
			Projectile.direction = ((Projectile.velocity.X > 0f) ? 1 : (-1));
			Projectile.spriteDirection = Projectile.direction;

			// While channeling, cannot hit enemies through blocks unless the enemy moves through blocks (like Ghosts or Wraiths)
			Projectile.ownerHitCheck = Projectile.ai[0] == (int)FlailStateID.Channel;

			// Rotate the flail
			if (Projectile.velocity.Length() > 1f)
			{
				Projectile.rotation = Projectile.velocity.ToRotation() + Projectile.velocity.X * 0.1f;
			}
			else
			{
				Projectile.rotation += Projectile.velocity.X * 0.1f;
			}

			// Prevents the flail from despawning
			Projectile.timeLeft = 2;

			// Player can't switch items while using the flail, and their arm always points towards the flail
			player.heldProj = Projectile.whoAmI;
			player.SetDummyItemTime(2);
			player.itemRotation = Projectile.DirectionFrom(playerCenter).ToRotation();
			if (Projectile.Center.X < playerCenter.X)
			{
				player.itemRotation += (float)Math.PI;
			}
			player.itemRotation = MathHelper.WrapAngle(player.itemRotation);
			
			// If a specific flail needs to do anything unusual, that happens after everything else
			ExtraAI();
		}

		/// <summary>
		/// When the flail has just switched from <see cref="FlailStateID.FlyOut"/> to <see cref="FlailStateID.Return"/>, this method is called. 
		/// This can be used to, say, fire a secondary projectile like the Drippler Crippler does
		/// </summary>
		public virtual void OnStartRetracting()
		{
			// This method can be used to do something as soon as the projectile switches from FlyOut to Return
			// Useful for replicating things like Drippler Crippler's secondary projectile
		}

		/// <summary>
		/// Additional AI method for flails. Runs at the end of the main <see cref="AI()"/> field
		/// </summary>
		public virtual void ExtraAI()
		{
			// If a projectile has anything extra it needs to do, that goes here
		}

		// Reduce damage by 40% while spinning the flail
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Projectile.ai[0] == (int)FlailStateID.Channel)
			{
				Projectile.damage = (int)(Projectile.originalDamage * 0.6f);
			}
		}

		// When a flail hits a tile, it needs to bounce back and create some dust
		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			int hitCooldown = 10;
			int extraDust = 0;
			Vector2 velocity = Projectile.velocity;
			
			// Bounce the projectile away from tiles on impact
			float bounceVel = 0.2f;
			if (Projectile.ai[0] == (int)FlailStateID.FlyOut || Projectile.ai[0] == (int)FlailStateID.BounceBack)
			{
				bounceVel = 0.4f;
			}
			if (Projectile.ai[0] == (int)FlailStateID.Falling) // When falling it doesn't bounce
			{
				bounceVel = 0f;
			}
			if (oldVelocity.X != Projectile.velocity.X)
			{
				if (Math.Abs(oldVelocity.X) > 4f)
				{
					extraDust = 1;
				}
				Projectile.velocity.X = (0f - oldVelocity.X) * bounceVel;
				Projectile.localAI[0] += 1f;
			}
			if (oldVelocity.Y != Projectile.velocity.Y)
			{
				if (Math.Abs(oldVelocity.Y) > 4f)
				{
					extraDust = 1;
				}
				Projectile.velocity.Y = (0f - oldVelocity.Y) * bounceVel;
				Projectile.localAI[0] += 1f;
			}

			// While flying out, make it bounce back and create some dust
			if (Projectile.ai[0] == (int)FlailStateID.FlyOut)
			{
				Projectile.ai[0] = (int)FlailStateID.BounceBack;
				Projectile.localNPCHitCooldown = hitCooldown;
				Projectile.netUpdate = true;
				Point scanAreaStart = Projectile.TopLeft.ToTileCoordinates();
				Point scanAreaEnd = Projectile.BottomRight.ToTileCoordinates();
				extraDust = 2;
				CreateImpactExplosion(2, Projectile.Center, ref scanAreaStart, ref scanAreaEnd, Projectile.width, out var causedShockwaves);
				CreateImpactExplosion2_FlailTileCollision(Projectile.Center, causedShockwaves, velocity);
				Projectile.position -= velocity; // Move the projectile backwards so it doesn't clip into the ground
			}

			// A little bit of extra dust
			if (extraDust > 0)
			{
				Projectile.netUpdate = true;
				for (int i = 0; i < extraDust; i++)
				{
					Collision.HitTiles(Projectile.position, velocity, Projectile.width, Projectile.height);
				}
				SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
			}

			// Have the flail return to the player without allowing them to make it fall
			if ((Projectile.ai[0] == (int)FlailStateID.FlyOut || Projectile.ai[0] == (int)FlailStateID.Return) && Projectile.localAI[0] >= 10f)
			{
				Projectile.ai[0] = (int)FlailStateID.FallReturn;
				Projectile.netUpdate = true;
			}
			return false;
		}

		private static void CreateImpactExplosion(int dustAmountMultiplier, Vector2 explosionOrigin, ref Point scanAreaStart, ref Point scanAreaEnd, int explosionRange, out bool causedShockwaves)
		{
			causedShockwaves = false;
			const int mult = 4;
			for (int i = scanAreaStart.X; i <= scanAreaEnd.X; i++)
			{
				for (int j = scanAreaStart.Y; j <= scanAreaEnd.Y; j++)
				{
					if (Vector2.Distance(explosionOrigin, new Vector2(i * 16, j * 16)) > explosionRange)
					{
						continue;
					}
					Tile tile = Framing.GetTileSafely(i, j);
					if (tile.IsActuated || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] || Main.tileFrameImportant[tile.TileType])
					{
						continue;
					}
					Tile tile2 = Framing.GetTileSafely(i, j - 1);
					if (!tile2.IsActuated && Main.tileSolid[tile2.TileType] && !Main.tileSolidTop[tile2.TileType])
					{
						continue;
					}
					int dustAmt = WorldGen.KillTile_GetTileDustAmount(fail: true, tile, i, j) * dustAmountMultiplier;
					for (int k = 0; k < dustAmt; k++)
					{
						Dust obj = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, tile)];
						obj.velocity.Y -= 3f + mult * 1.5f;
						obj.velocity.Y *= Main.rand.NextFloat();
						obj.scale += mult * 0.03f;
					}
					for (int l = 0; l < dustAmt - 1; l++)
					{
						Dust obj2 = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, tile)];
						obj2.velocity.Y -= 1f + mult;
						obj2.velocity.Y *= Main.rand.NextFloat();
					}
					if (dustAmt > 0)
					{
						causedShockwaves = true;
					}
				}
			}
		}

		private void CreateImpactExplosion2_FlailTileCollision(Vector2 explosionOrigin, bool causedShockwaves, Vector2 velocityBeforeCollision)
		{
			Vector2 spinningpoint = new(7f, 0f);
			Vector2 value = new(1f, 0.7f);
			Color color = Color.White * 0.5f;
			Vector2 value2 = velocityBeforeCollision.SafeNormalize(Vector2.Zero);
			for (float num = 0f; num < 8f; num += 1f)
			{
				Vector2 value3 = spinningpoint.RotatedBy(num * ((float)Math.PI * 2f) / 8f) * value;
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.alpha = 0;
				if (!causedShockwaves)
				{
					dust.alpha = 50;
				}
				dust.color = color;
				dust.position = explosionOrigin + value3;
				dust.velocity.Y -= 0.8f;
				dust.velocity.X *= 0.8f;
				dust.fadeIn = 0.3f + Main.rand.NextFloat() * 0.4f;
				dust.scale = 0.4f;
				dust.noLight = true;
				dust.velocity += value2 * 2f;
			}
			if (!causedShockwaves)
			{
				for (float num2 = 0f; num2 < 8f; num2 += 1f)
				{
					Vector2 value4 = spinningpoint.RotatedBy(num2 * ((float)Math.PI * 2f) / 8f) * value;
					Dust dust2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
					dust2.alpha = 100;
					dust2.color = color;
					dust2.position = explosionOrigin + value4;
					dust2.velocity.Y -= 1f;
					dust2.velocity.X *= 0.4f;
					dust2.fadeIn = 0.3f + Main.rand.NextFloat() * 0.4f;
					dust2.scale = 0.4f;
					dust2.noLight = true;
					dust2.velocity += value2 * 1.5f;
				}
			}
		}
	}
}
