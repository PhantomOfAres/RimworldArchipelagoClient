using RimWorld;
using RimworldArchipelago;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

public class Dialog_Grinder : Window
{
    private Vector2 unqueuedListScrollPos;
    private Vector2 queuedListScrollPos;
    private Building_ArchipelagoGrinder grinder;

    private string selectedRecipe;
    public override Vector2 InitialSize => new Vector2(960f, 640f);


    public Dialog_Grinder(Building_ArchipelagoGrinder grinder)
    {
        closeOnClickedOutside = true;
        doCloseX = true;
        this.grinder = grinder;
    }

    public override void DoWindowContents(Rect inRect)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        Rect buttonSection = inRect.BottomPartPixels(100f);
        Rect topSection = inRect.TopPartPixels(inRect.height - 100f);

        Rect labelSection = topSection.TopPartPixels(20f);
        TextAnchor oldAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect leftLabel = labelSection.LeftHalf();
        Widgets.Label(leftLabel, "Unsent Craft Locations");

        Rect rightLabel = labelSection.RightHalf();
        Widgets.Label(rightLabel, "Queued Craft Locations");
        Text.Anchor = oldAnchor;

        Rect contentSection = topSection.BottomPartPixels(topSection.height - 20f);
        Rect unqueuedSection = contentSection.LeftHalf();
        DoUnqueuedList(unqueuedSection);
        Rect queuedSection = contentSection.RightHalf();
        DoQueuedList(queuedSection);

        bool selectedRecipeQueued = !string.IsNullOrEmpty(selectedRecipe) && grinder.billStack.Bills.Find(x => x.recipe.defName == selectedRecipe) != null;
        Rect addSectionRect = buttonSection.LeftHalf();
        Rect addButtonRect = new Rect(addSectionRect.x + 8f, addSectionRect.y + 8f, (addSectionRect.width - 16f) / 2 , 46f);
        if (Widgets.ButtonText(addButtonRect, "Add", active: !string.IsNullOrEmpty(selectedRecipe) && !selectedRecipeQueued))
        {
            RecipeDef selectedRecipeDef = DefDatabase<RecipeDef>.GetNamed(selectedRecipe);
            Bill bill = BillUtility.MakeNewBill(selectedRecipeDef);
            grinder.billStack.AddBill(bill);
        }
        Rect addAllButtonRect = new Rect(addSectionRect.x + addSectionRect.width / 2 + 8f, addSectionRect.y + 8f, (addSectionRect.width - 16f) / 2 - 8f, 46f);
        if (Widgets.ButtonText(addAllButtonRect, "Add All"))
        {
            foreach (string recipeId in APCraftManager.craftRecipesToArchipelagoIds.Keys)
            {
                if (!ArchipelagoGameComponent.IsCraftLocationHandled(recipeId) && grinder.billStack.Bills.Find(x => x.recipe.defName == recipeId ) == null)
                {
                    RecipeDef selectedRecipeDef = DefDatabase<RecipeDef>.GetNamed(recipeId);
                    Bill bill = BillUtility.MakeNewBill(selectedRecipeDef);
                    grinder.billStack.AddBill(bill);
                }
            }
        }


        Rect removeSectionRect = buttonSection.RightHalf();
        Rect removeButtonRect = new Rect(removeSectionRect.x + 8f, removeSectionRect.y + 8f, (removeSectionRect.width - 16f) / 2, 46f);
        if (Widgets.ButtonText(removeButtonRect, "Remove", active: !string.IsNullOrEmpty(selectedRecipe) && selectedRecipeQueued))
        {
            Bill toDelete = null;
            foreach (Bill bill in grinder.billStack.Bills)
            {
                if (bill.recipe.defName == selectedRecipe)
                {
                    toDelete = bill;
                    break;
                }
            }

            if (toDelete != null)
            {
                grinder.billStack.Delete(toDelete);
            }
        }
        Rect removeAllButtonRect = new Rect(removeSectionRect.x + removeSectionRect.width / 2 + 8f, removeSectionRect.y + 8f, (removeSectionRect.width - 16f) / 2 - 8f, 46f);
        if (Widgets.ButtonText(removeAllButtonRect, "Remove All"))
        {
            List<Bill> billsCopy = new List<Bill>(grinder.billStack.Bills);
            foreach (Bill bill in billsCopy)
            {
                grinder.billStack.Delete(bill);
            }
        }

        Rect confirmButtonRect = buttonSection.BottomHalf();
        confirmButtonRect = confirmButtonRect.MiddlePartPixels(180f, 46f);
        confirmButtonRect = new Rect(confirmButtonRect.x, confirmButtonRect.y + 8f, confirmButtonRect.width, confirmButtonRect.height - 8f);
        if (Widgets.ButtonText(confirmButtonRect, "Confirm"))
        {
            Close();
        }
    }

    private void DoUnqueuedList(Rect inRect)
    {
        List<string> allArchipelagoRecipes = new List<string>(APCraftManager.craftRecipesToArchipelagoIds.Keys);
        List<string> unqueuedRecipes = new List<string>();
        foreach (string recipeName in allArchipelagoRecipes)
        {
            if (!ArchipelagoGameComponent.IsCraftLocationHandled(recipeName))
            {
                unqueuedRecipes.Add(recipeName);
            }
        }

        foreach (Bill bill in grinder.billStack.Bills)
        {
            if (allArchipelagoRecipes.Contains(bill.recipe.defName))
            {
                unqueuedRecipes.Remove(bill.recipe.defName);
            }
        }

        Widgets.DrawMenuSection(inRect);
        var position = inRect.position;
        position += new Vector2(8f, 8f);
        Rect viewRect = new Rect
        {
            x = position.x,
            y = position.y,
            width = inRect.width,
            height = 16 + 80 * unqueuedRecipes.Count
        };
        position += new Vector2(8f, 8f);
        Widgets.BeginScrollView(inRect, ref unqueuedListScrollPos, viewRect, true);
        foreach (string id in unqueuedRecipes)
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(id);
            Rect rect = new Rect(position, new Vector2(viewRect.width - 28f, 72f));
            WidgetRow widgetRow = new WidgetRow();
            if (id == selectedRecipe)
            {
                Widgets.DrawBoxSolidWithOutline(rect, new Color(1f, 0.8f, 0.2f, 0.2f), Color.white);
            }
            else
            {
                Widgets.DrawBoxSolidWithOutline(rect, Color.black, Color.white);
            }

            Rect numberRect = rect.LeftPartPixels(36f);
            rect = rect.RightPartPixels(rect.width - 36f);

            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            string indexLabel = (APCraftManager.craftRecipesToArchipelagoIds[id] - APCraftManager.FirstCraftLocationId).ToString();
            Widgets.Label(numberRect, indexLabel);
            Text.Anchor = oldAnchor;

            Rect restOfRect = rect.TopHalf();
            Widgets.DefLabelWithIcon(restOfRect, recipe.ingredients[0].FixedIngredient);

            restOfRect = rect.BottomHalf();
            Widgets.DefLabelWithIcon(restOfRect, recipe.ingredients[1].FixedIngredient);

            if (Widgets.ButtonInvisible(rect))
            {
                selectedRecipe = id;
            }
            position.y += 80f;
        }
        Widgets.EndScrollView();
    }

    private void DoQueuedList(Rect inRect)
    {
        Widgets.DrawMenuSection(inRect);
        var position = inRect.position;
        position += new Vector2(8f, 8f);
        Rect viewRect = new Rect
        {
            x = position.x,
            y = position.y,
            width = inRect.width,
            height = 16 + 80 * grinder.billStack.Bills.Count
        };
        position += new Vector2(8f, 8f);
        Widgets.BeginScrollView(inRect, ref queuedListScrollPos, viewRect, true);
        foreach (Bill bill in grinder.billStack.Bills)
        {
            string id = bill.recipe.defName;
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(id);
            Rect rect = new Rect(position, new Vector2(viewRect.width - 28f, 72f));

            WidgetRow widgetRow = new WidgetRow();
            if (id == selectedRecipe)
            {
                Widgets.DrawBoxSolidWithOutline(rect, new Color(1f, 0.8f, 0.2f, 0.2f), Color.white);
            }
            else
            {
                Widgets.DrawBoxSolidWithOutline(rect, Color.black, Color.white);
            }

            Rect numberRect = rect.LeftPartPixels(36f);
            rect = rect.RightPartPixels(rect.width - 36f);

            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            string indexLabel = (APCraftManager.craftRecipesToArchipelagoIds[id] - APCraftManager.FirstCraftLocationId).ToString();
            Widgets.Label(numberRect, indexLabel);
            Text.Anchor = oldAnchor;

            Rect restOfRect = rect.TopHalf();
            Widgets.DefLabelWithIcon(restOfRect, recipe.ingredients[0].FixedIngredient);

            restOfRect = rect.BottomHalf();
            Widgets.DefLabelWithIcon(restOfRect, recipe.ingredients[1].FixedIngredient);

            if (Widgets.ButtonInvisible(rect))
            {
                selectedRecipe = id;
            }
            position.y += 80f;
        }
        Widgets.EndScrollView();
    }
}
