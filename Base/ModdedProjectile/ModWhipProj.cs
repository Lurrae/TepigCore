using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModWhipProj : ModProjectile
	{
		// Note: This code was adapted from JohnSnail's ExampleWhipProjectile, though some things had to be changed
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.IsAWhip[Type] = true;
		}

		public virtual void WhipSetDefaults()
		{
			// Anything that needs to be set per-whip can be done here, like ownerHitCheck or extraUpdates
		}

		public override void SetDefaults()
		{
			WhipSetDefaults();

			Projectile.width = 22;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.DamageType = DamageClass.Summon;
		}

		public abstract int TagDebuff { get; } // What debuff causes the tag damage
		public virtual int ExtraDebuff() { return 0; } // If this whip has an extra debuff, it goes here. Defaults to 0 for no extra debuff
		public abstract float Falloff { get; } // By what percentage does damage get reduced by each time the whip hits something?
		public abstract bool ExtraDust { get; } // Whether or not the whip has dust as it travels, like Kaleidoscope
		public virtual int ExtraDustID() { return 0; } // If this whip summons extra dust particles, it should use this ID
		public virtual float DustVelMult() { return 2f; } // If this whip summons extra dust particles, how fast should they travel?
		public abstract bool Fullbright { get; } // Should the whip be drawn without taking brightness into account?
		public abstract bool HasLine { get; } // Whether or not the whip draws a line behind its sprite
		public virtual Color LineColor() { return Color.White; } // If the whip should draw a line, this color is used for it
		public virtual float ScaleMult() { return 1f; } // How large should the whip's sprite be?
		public abstract bool RandomMidSegments { get; } // Should the middle segments of the whips be selected at random (true) or based on the order in the sprite sheet (false)?

		private float Timer
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			Timer++;

			Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);

			if (Timer >= timeToFlyOut || owner.itemAnimation <= 0)
			{
				Projectile.Kill();
				return;
			}
			owner.heldProj = Projectile.whoAmI;
			owner.itemAnimation = owner.itemAnimationMax - (int)(Timer / Projectile.MaxUpdates);
			owner.itemTime = owner.itemAnimation;

			if (Timer == (int)(timeToFlyOut / 2))
			{
				List<Vector2> points = Projectile.WhipPointsForCollision;
				Projectile.FillWhipControlPoints(Projectile, points);
				SoundEngine.PlaySound(SoundID.Item153, points[^1]);
			}

			// Draw some extra dust as the whip travels
			if (ExtraDust)
			{
				float t = Timer / timeToFlyOut;
				if (Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true) > 0.1f)
				{
					Projectile.WhipPointsForCollision.Clear();
					Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);
					Rectangle beltTip = Utils.CenteredRectangle(Projectile.WhipPointsForCollision[^1], new Vector2(30f, 30f));

					Vector2 vel = Projectile.WhipPointsForCollision[^2].DirectionTo(Projectile.WhipPointsForCollision[^1]).SafeNormalize(Vector2.Zero);

					Dust whipDust = Dust.NewDustDirect(beltTip.TopLeft(), beltTip.Width, beltTip.Height, ExtraDustID());
					whipDust.velocity = vel * DustVelMult();
				}
			}
		}

		// This triggers the tag damage on enemies and sets the minion target
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(TagDebuff, 240);
			if (ExtraDebuff() != 0)
				target.AddBuff(ExtraDebuff(), 240);

			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

			// Reducing damage per enemy hit based on falloff value
			Projectile.damage = (int)(Projectile.damage * (1f - Falloff));
		}

		// TODO: Figure out how to use the scaleMult parameter
		private static void DrawLine(List<Vector2> list, bool fullBright, Color lineColor, float scaleMult)
		{
			Texture2D texture = TextureAssets.FishingLine.Value;
			Rectangle frame = texture.Frame();
			Vector2 origin = new((frame.Width - 8) / 2, 2);

			Vector2 pos = list[0];
			for (int i = 0; i < list.Count - 2; i++)
			{
				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = lineColor;
				if (!fullBright)
				{
					color = Lighting.GetColor(element.ToTileCoordinates(), lineColor);
				}
				Vector2 scale = new(1f, (diff.Length() + 2) / frame.Height);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

				pos += diff;
			}
		}

		private bool HasRandomizedMid = false;
		private int MidSprite = 0;
		private readonly Dictionary<int, int> ChosenMidSprites = new();

		public override bool PreDraw(ref Color lightColor)
		{
			List<Vector2> list = new();
			Projectile.FillWhipControlPoints(Projectile, list);

			if (HasLine)
				DrawLine(list, Fullbright, LineColor(), ScaleMult());
			SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			Main.instance.LoadProjectile(Projectile.type);
			Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

			Vector2 pos = list[0];

			for (int i = 0; i < list.Count - 1; i++)
			{
				Rectangle frame = new(0, 0, 22, 28);
				Vector2 origin = new(5, 8);
				float scale = 1;

				// Determine which sprite from the sheet to use
				// frame.Y marks the start of the area to grab, with frame.Height being the height of the area
				if (i == list.Count - 2)
				{
					frame.Y = 112;
					frame.Height = 28;

					Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
					float t = Timer / timeToFlyOut;
					scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
				}
				else if (i > 0)
				{
					int j = 0;
					int[] MidSprites = new int[] { 28, 56, 84 };

					if (i % 8 == 0)
						j++;

					if (RandomMidSegments)
					{
						if (!HasRandomizedMid)
						{
							MidSprite = Main.rand.Next(MidSprites);
							ChosenMidSprites.Add(i, MidSprite);
						}
						else
						{
							MidSprite = ChosenMidSprites[i];
						}
					}
					else
					{
						MidSprite = MidSprites[Math.Max(j, 2)];
					}
					frame.Y = MidSprite;
					frame.Height = 28;
				}

				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = Color.White;

				if (!Fullbright)
				{
					color = Lighting.GetColor(element.ToTileCoordinates(), Color.White);
				}

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale * ScaleMult(), flip, 0);

				pos += diff;
			}

			// Doing this check here so that it goes through all of the segments and randomizes them first
			if (!HasRandomizedMid && RandomMidSegments)
			{
				HasRandomizedMid = true;
			}

			return false;
		}
	}
}