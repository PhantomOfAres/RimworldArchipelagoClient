using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    internal class APCraftManager
    {
        public static Dictionary<string, long> craftRecipesToArchipelagoIds = new Dictionary<string, long>();
        public static Dictionary<long, string> archipelagoIdsToCraftRecipes= new Dictionary<long, string>();
        public static long FirstCraftLocationId = -1;

        public static void GenerateArchipelagoCrafts()
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            SlotData slotData = ArchipelagoClient.SlotData;
            ThingDef archipelagoBench = DefDatabase<ThingDef>.GetNamed("ArchipelagoGrinder");
            archipelagoBench.recipes = new List<RecipeDef>();
            SkillDef craftingSkill = DefDatabase<SkillDef>.GetNamed("Crafting");
            StatDef generalLaborSpeedStat = DefDatabase<StatDef>.GetNamed("GeneralLaborSpeed");
            foreach (long locationId in slotData.CraftRecipes.Keys)
            {
                if (FirstCraftLocationId == -1 || locationId < FirstCraftLocationId)
                {
                    FirstCraftLocationId = locationId;
                }
            }

            foreach ((long locationId, List<string> recipe) in slotData.CraftRecipes)
            {
                RecipeDef recipeDef = new RecipeDef();
                List<ThingDef> thingDefs = new List<ThingDef>();
                recipeDef.ingredients = new List<IngredientCount>();
                StringBuilder labelBuilder = new StringBuilder();
                long locationLabel = locationId - FirstCraftLocationId;
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
                archipelagoIdsToCraftRecipes[locationId] = recipeDef.defName;
                archipelagoBench.recipes.Add(recipeDef);
                DefDatabase<RecipeDef>.Add(recipeDef);
            }
        }

        public static bool IsApCraft(string craftRecipeName)
        {
            return craftRecipesToArchipelagoIds.ContainsKey(craftRecipeName);
        }

        public static bool IsApCraft(long archipelagoId)
        {
            return archipelagoIdsToCraftRecipes.ContainsKey(archipelagoId);
        }

        public static long GetLocationId(string craftRecipeName)
        {
            if (craftRecipesToArchipelagoIds.ContainsKey(craftRecipeName))
            {
                return craftRecipesToArchipelagoIds[craftRecipeName];
            }

            return 0;
        }

        public static string GetCraftName(long archipelagoId)
        {
            if (archipelagoIdsToCraftRecipes.ContainsKey(archipelagoId))
            {
                return archipelagoIdsToCraftRecipes[archipelagoId];
            }

            return "";
        }

        public static void CompleteLocations(ReadOnlyCollection<long> checkedArchipelagoIds)
        {
            foreach (long archipelagoId in checkedArchipelagoIds)
            {
                string craftName = GetCraftName(archipelagoId);
                if (IsApCraft(archipelagoId) && !ArchipelagoGameComponent.IsCraftLocationHandled(craftName))
                {
                    RemoveCompletedArchipelagoBills(craftName);
                    ArchipelagoGameComponent.CraftLocationHandled(craftName);
                }
            }
        }

        public static void RemoveCompletedArchipelagoBills(string recipeName)
        {
            IEnumerable<Building_ArchipelagoGrinder> mapGrinders = Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfClass<Building_ArchipelagoGrinder>();
            foreach (Building_ArchipelagoGrinder grinder in mapGrinders)
            {
                List<Bill> toDelete = new List<Bill>();
                foreach (Bill bill in grinder.billStack.Bills)
                {
                    if (bill.recipe.defName == recipeName)
                    {
                        toDelete.Add(bill);
                    }
                }

                foreach (Bill bill in toDelete)
                {
                    grinder.billStack.Delete(bill);
                }
            }
        }
    }
}
