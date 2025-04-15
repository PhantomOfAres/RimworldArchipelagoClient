using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    internal class APCraftManager
    {
        private static Dictionary<string, long> craftRecipesToArchipelagoIds = new Dictionary<string, long>();

        public static void GenerateArchipelagoCrafts()
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            SlotData slotData = ArchipelagoClient.SlotData;
            ThingDef archipelagoBench = DefDatabase<ThingDef>.GetNamed("ArchipelagoGrinder");
            archipelagoBench.recipes = new List<RecipeDef>();
            SkillDef craftingSkill = DefDatabase<SkillDef>.GetNamed("Crafting");
            StatDef generalLaborSpeedStat = DefDatabase<StatDef>.GetNamed("GeneralLaborSpeed");
            long firstLocationId = -1;
            foreach (long locationId in slotData.CraftRecipes.Keys)
            {
                if (firstLocationId == -1 || locationId < firstLocationId)
                {
                    firstLocationId = locationId;
                }
            }

            foreach ((long locationId, List<string> recipe) in slotData.CraftRecipes)
            {
                RecipeDef recipeDef = new RecipeDef();
                List<ThingDef> thingDefs = new List<ThingDef>();
                recipeDef.ingredients = new List<IngredientCount>();
                StringBuilder labelBuilder = new StringBuilder();
                long locationLabel = locationId - firstLocationId;
                foreach (string item in recipe)
                {
                    ThingDef ingredient = DefDatabase<ThingDef>.GetNamed(item);
                    IngredientCount ingredientCount = new IngredientCount();
                    ingredientCount.filter.SetAllow(ingredient, true);
                    ingredientCount.SetBaseCount(1);
                    recipeDef.ingredients.Add(ingredientCount);

                    if (labelBuilder.Length > 0)
                    {
                        labelBuilder.Append(" + ");
                    }
                    labelBuilder.Append(textInfo.ToTitleCase(ingredient.label));
                }
                recipeDef.defaultIngredientFilter = new ThingFilter();
                recipeDef.label = $"({locationLabel}) {labelBuilder}";
                recipeDef.defName = $"{recipeDef.label}{locationId}";
                recipeDef.description = "Craft the specified things together to send a check to Archipelago!";
                recipeDef.jobString = "Sending an Archipelago check";
                recipeDef.workAmount = 500;
                recipeDef.workSpeedStat = generalLaborSpeedStat;
                recipeDef.workSkill = craftingSkill;
                recipeDef.workSkillLearnFactor = 0;

                craftRecipesToArchipelagoIds[recipeDef.defName] = locationId;
                archipelagoBench.recipes.Add(recipeDef);
                DefDatabase<RecipeDef>.Add(recipeDef);
            }

            SortBenchRecipes();
        }

        public static void SortBenchRecipes()
        {
            ThingDef archipelagoBench = DefDatabase<ThingDef>.GetNamed("ArchipelagoGrinder");
            archipelagoBench.recipes.Sort(delegate(RecipeDef def1, RecipeDef def2)
            {
                long locationId1 = GetLocationId(def1.defName);
                long locationId2 = GetLocationId(def2.defName);
                bool def1Complete = ArchipelagoClient.IsLocationComplete(locationId1);
                bool def2Complete = ArchipelagoClient.IsLocationComplete(locationId2);
                if (def1Complete && !def2Complete) { return 1; }
                if (def2Complete && !def1Complete) { return -1; }
                return (int) (locationId1 - locationId2);
            });

            archipelagoBench.ClearCachedData();
        }

        public static bool IsApCraft(string craftRecipeName)
        {
            return craftRecipesToArchipelagoIds.ContainsKey(craftRecipeName);
        }

        public static long GetLocationId(string craftRecipeName)
        {
            if (craftRecipesToArchipelagoIds.ContainsKey(craftRecipeName))
            {
                return craftRecipesToArchipelagoIds[craftRecipeName];
            }

            return 0;
        }
    }
}
