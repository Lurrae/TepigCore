using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace TepigCore.Base.ModdedProjectile
{
	public abstract class ModWhipProj<TagDebuff> : ModProjectile where TagDebuff : ModBuff
	{
		public virtual Texture2D FishingLineOverrideTexture => null;
		public virtual Vector2? OverrideTextureOrigin => null;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.IsAWhip[Type] = true;
		}

		/// <summary>
		/// Anything that needs to be set per-whip can be done here, mainly Projectile.WhipSettings values
		/// </summary>
		public virtual void WhipSetDefaults()
		{
		}

		public float Falloff = 0.3f; // By what percentage does damage get reduced by each time the whip hits something? 0% means no damage falloff, 100% means only the first enemy hit takes any damage
		public Vector2 SpriteSize = new(22, 28); // The size of one frame of the whip's size
		public int ExtraDustID = -1; // If this whip summons extra dust particles, it should use this ID. If this is -1, no extra dust is spawned
		public float DustVelMult = 2; // If this whip summons extra dust particles, how fast should they travel?
		public Color? DustColorOverride = null; // Allows one to override the default draw color of extra dust spawned by the whip
		public bool IsFullbright = false; // Should the whole whip be drawn without taking brightness into account?
		public bool FullbrightLine = false; // Should only the line be drawn without brightness?
		public bool HasLine = true; // Whether or not the whip draws a line behind its sprite
		public Color LineColor = Color.White; // If the whip should draw a line, this color is used for it
		public float ScaleMultiplier = 1; // How large should the whip's sprite be?
		public bool UseRandomSegments = false; // Should the middle segments of the whips be selected at random (true) or based on the order in the sprite sheet (false)?
		public bool DisableTipScaling = false; // Should the tip of the whip scale up as the whip flies out?

		public override void SetDefaults()
		{
			Projectile.DefaultToWhip();

			WhipSetDefaults();
		}

		private float Timer
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		/// <summary>
		/// Use this to add any extra behavior to a whip
		/// </summary>
		public virtual void WhipAI()
		{
		}

		public sealed override void AI()
		{
			// Draw some extra dust as the whip travels
			if (ExtraDustID > -1)
			{
				Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
				
				float t = Timer / timeToFlyOut;
				if (Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true) > 0.1f)
				{
					Projectile.WhipPointsForCollision.Clear();
					Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);
					Rectangle beltTip = Utils.CenteredRectangle(Projectile.WhipPointsForCollision[^1], new Vector2(30f, 30f));

					Vector2 vel = Projectile.WhipPointsForCollision[^2].DirectionTo(Projectile.WhipPointsForCollision[^1]).SafeNormalize(Vector2.Zero);

					Dust whipDust = Dust.NewDustDirect(beltTip.TopLeft(), beltTip.Width, beltTip.Height, ExtraDustID, newColor: DustColorOverride ?? default);
					whipDust.velocity = vel * DustVelMult;
				}
			}

			WhipAI();
		}

		/// <summary>
		/// Additional on-hit whip effects, like a secondary debuff or a visual effect, can be applied here
		/// </summary>
		/// <param name="target"></param>
		/// <param name="hit"></param>
		/// <param name="damageDone"></param>
		public virtual void OnHitExtras(NPC target, NPC.HitInfo hit, int damageDone)
		{
		}

		// This triggers the tag damage on enemies and sets the minion target
		public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffType<TagDebuff>(), 240);

			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

			// Reducing damage per enemy hit based on falloff value
			Projectile.damage = (int)(Projectile.damage * (1f - Falloff));

			OnHitExtras(target, hit, damageDone);
		}

		private void DrawLine(List<Vector2> list, bool fullBright, Color lineColor, float scaleMult)
		{
			Texture2D texture = FishingLineOverrideTexture ?? TextureAssets.FishingLine.Value;
			Rectangle frame = texture.Frame();
			Vector2 origin = OverrideTextureOrigin ?? new(frame.Width / 2, 0);

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
				Vector2 scale = new(1, (diff.Length() + 2) / frame.Height);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale * scaleMult, SpriteEffects.None, 0);

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
				DrawLine(list, IsFullbright || FullbrightLine, LineColor, ScaleMultiplier);
			SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			Texture2D texture = (Texture2D)Request<Texture2D>(Texture);

			Vector2 pos = list[0];
			int j = 0;

			for (int i = 0; i < list.Count - 1; i++)
			{
				Rectangle frame = new(0, 0, (int)SpriteSize.X, (int)SpriteSize.Y);
				Vector2 origin = new((int)SpriteSize.X / 2, (int)SpriteSize.Y / 2);
				float scale = 1;

				// Determine which sprite from the sheet to use
				// frame.Y marks the start of the area to grab, with frame.Height being the height of the area
				if (i == list.Count - 2)
				{
					frame.Height = (int)SpriteSize.Y;
					frame.Y = frame.Height * 4;

					Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out _, out _);
					float t = Timer / timeToFlyOut;
					if (!DisableTipScaling)
						scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
				}
				else if (i > 0)
				{
					int[] MidSprites = new int[] { frame.Height, frame.Height * 2, frame.Height * 3 };

					Projectile.GetWhipSettings(Projectile, out _, out int segments, out _);
					if (i % MathF.Floor(segments / 3) == 0)
					{
						j++;
						if (j > 2)
							j = 0;
					}

					if (UseRandomSegments)
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
					frame.Height = (int)SpriteSize.Y;
				}

				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = Color.White;

				if (!IsFullbright)
					color = Lighting.GetColor(element.ToTileCoordinates(), Color.White);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale * ScaleMultiplier, flip, 0);

				pos += diff;
			}

			// Doing this check here so that it goes through all of the segments and randomizes them first
			if (!HasRandomizedMid && UseRandomSegments)
			{
				HasRandomizedMid = true;
			}

			return false;
		}
	}
}