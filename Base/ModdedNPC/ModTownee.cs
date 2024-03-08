using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace TepigCore.Base.ModdedNPC
{
	public abstract class ModTownee : ModNPC
	{
		public virtual void TowneeStaticDefaults()
		{
			/* List of things that should be put here:
			 * All things related to frame count (npcFrameCount, ExtraFramesCount, AttackFrameCount, etc.)
			 * AttackType, AttackTime, and AttackAverageChance
			 * Happiness data
			 */
		}

		public virtual void TowneeSetDefaults()
		{
			/* List of things that should be put here:
			 * AnimationType
			 * Possibly HitSound/DeathSound? By default all NPCs use the same sounds, but if an exception needs to be made that should be done here
			 */
		}

		public abstract bool IsMale { get; } // Are you a boy or a girl? - Professor Oak
		public abstract string DialogueKey { get; } // Gets an individual NPC's dialogue's localization string

		public sealed override void SetStaticDefaults()
		{
			NPCID.Sets.DangerDetectRange[Type] = 700;
			NPCID.Sets.ShimmerTownTransform[Type] = true;

			TowneeStaticDefaults();
			
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
			{
				Velocity = 1f,
				Direction = -1
			};

			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public sealed override void SetDefaults()
		{
			// Putting HitSound and DeathSound above the TowneeSetDefaults allows them to be overriden if needed!
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;

			TowneeSetDefaults();

			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 10;
			NPC.defense = 15;
			NPC.lifeMax = 250;
			NPC.knockBackResist = 0.5f;
		}

		// Will automatically load most kinda of dialogue
		// Currently does not support most boss downed bools, certain biomes, world info, and misc. things
		// See https://github.com/tModLoader/tModLoader/pull/3386/files#diff-be053587123833ab8908af47484c75c893a1857d83495bc6049451d326adc0d9R436 for a full list of substitutions NOT included by default
		public override string GetChat()
		{
			var substitutions = Lang.CreateDialogSubstitutionObject(NPC);
			var filter = Lang.CreateDialogFilter(DialogueKey, substitutions);

			return Language.SelectRandom(filter).FormatWith(substitutions);
		}

		public override bool CanGoToStatue(bool toKingStatue) => (toKingStatue && IsMale) || (!toKingStatue && !IsMale);
	}
}