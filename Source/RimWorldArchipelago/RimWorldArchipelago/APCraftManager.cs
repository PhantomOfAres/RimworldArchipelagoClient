using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using System;
using System.Collections.Generic;
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
            SlotData slotData = ArchipelagoClient.SlotData;
            ThingDef archipelagoBench = DefDatabase<ThingDef>.GetNamed("ArchipelagoGrinder");
            archipelagoBench.recipes = new List<RecipeDef>();
            SkillDef craftingSkill = DefDatabase<SkillDef>.GetNamed("Crafting");
            StatDef generalLaborSpeedStat = DefDatabase<StatDef>.GetNamed("GeneralLaborSpeed");
            foreach ((long locationId, List<string> recipe) in slotData.CraftRecipes)
            {
                RecipeDef recipeDef = new RecipeDef();
                List<ThingDef> thingDefs = new List<ThingDef>();
                recipeDef.ingredients = new List<IngredientCount>();
                StringBuilder labelBuilder = new StringBuilder();
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
                    labelBuilder.Append(ingredient.label);
                }
                recipeDef.defaultIngredientFilter = new ThingFilter();
                recipeDef.label = labelBuilder.ToString();
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
