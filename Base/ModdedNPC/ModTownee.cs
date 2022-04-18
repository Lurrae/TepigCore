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
			TowneeStaticDefaults();

			NPCID.Sets.DangerDetectRange[Type] = 700;
			
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new(0)
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

		public virtual WeightedRandom<string> ModChat()
		{
			return null;
		}

		public override string GetChat()
		{
			WeightedRandom<string> chat = new();
			string keyLocation = DialogueKey;
			LocalizedText[] dialogues = Language.FindAll(Lang.CreateDialogFilter(keyLocation));

			if (ModChat() != null)
			{
				chat.Add(ModChat());
			}

			int guide = NPC.FindFirstNPC(NPCID.Guide);
			int clothier = NPC.FindFirstNPC(NPCID.Clothier);

			// Events
			if (Main.bloodMoon)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("BloodMoon"))
					{
						if (!dialogue.Key.Contains("_Guide"))
						{
							chat.Add(Language.GetTextValue(dialogue.Key), 3);
						}
						else if (guide >= 0)
						{
							chat.Add(Language.GetTextValue(dialogue.Key, Main.npc[guide].GivenName));
						}
					}
				}
			}
			if (Main.IsItStorming)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Storm"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}
			else
			{
				if (Main.IsItRaining)
				{
					foreach (LocalizedText dialogue in dialogues)
					{
						if (dialogue.Key.Contains("Rain"))
						{
							chat.Add(Language.GetTextValue(dialogue.Key), 3);
						}
					}
				}
				if (Main.IsItAHappyWindyDay)
				{
					foreach (LocalizedText dialogue in dialogues)
					{
						if (dialogue.Key.Contains("WindyDay"))
						{
							chat.Add(Language.GetTextValue(dialogue.Key), 3);
						}
					}
				}
			}
			if (BirthdayParty.PartyIsUp)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Party"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}
			if (Sandstorm.Happening && Main.LocalPlayer.ZoneDesert)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Sandstorm"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}
			if (Main.slimeRain)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("SlimeRain"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}

			// Biomes/Environment
			if (Main.LocalPlayer.ZoneGraveyard)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Graveyard"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}
			if (NPC.homeless)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Homeless"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}

			if (Main.dayTime)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Day"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}
			else
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("Night"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key), 3);
					}
				}
			}

			// Post-boss
			if (NPC.downedBoss1 || NPC.downedSlimeKing || (NPC.downedSlimeKing && NPC.downedQueenSlime) || NPC.downedBoss2 || NPC.downedBoss3 || Main.hardMode)
			{
				foreach (LocalizedText dialogue in dialogues)
				{
					if (dialogue.Key.Contains("PostBoss"))
					{
						//Main.NewText(dialogue.Key);
						if (!dialogue.Key.Contains("_Clothier"))
						{
							chat.Add(Language.GetTextValue(dialogue.Key));
						}
						else if (clothier >= 0)
						{
							chat.Add(Language.GetTextValue(dialogue.Key, Main.npc[clothier].GivenName));
						}
					}
					else
					{
						if (!dialogue.Key.Contains("Hint_") && ((dialogue.Key.Contains("Hardmode") && Main.hardMode) || (dialogue.Key.Contains("KingSlime") && NPC.downedSlimeKing) || (dialogue.Key.Contains("QueenSlime") && NPC.downedQueenSlime) || (dialogue.Key.Contains("SlimeRoyals") && NPC.downedSlimeKing && NPC.downedQueenSlime)))
						{
							if (!dialogue.Key.Contains("_Player"))
							{
								chat.Add(Language.GetTextValue(dialogue.Key));
							}
							else
							{
								chat.Add(Language.GetTextValue(dialogue.Key, Main.LocalPlayer.name));
							}
						}
					}
				}
			}

			// Other
			foreach (LocalizedText dialogue in dialogues)
			{
				if (dialogue.Key.Contains("Normal"))
				{
					// Even though we don't handle keys with Serena's name here, we still need to exclude them from the "default" things
					if (!dialogue.Key.Contains("_Serena") && !dialogue.Key.Contains("_BC") && !dialogue.Key.Contains("_NoBC"))
					{
						chat.Add(Language.GetTextValue(dialogue.Key));
					}
					else
					{
						if ((dialogue.Key.Contains("_BC") && ModLoader.TryGetMod("BossChecklist", out Mod _)) || (dialogue.Key.Contains("_NoBC") && !ModLoader.TryGetMod("BossChecklist", out Mod _)))
						{
							chat.Add(Language.GetTextValue(dialogue.Key));
						}
					}
				}
			}

			return chat.Get();
		}

		public override bool CanGoToStatue(bool toKingStatue) => (toKingStatue && IsMale) || (!toKingStatue && !IsMale);
	}
}