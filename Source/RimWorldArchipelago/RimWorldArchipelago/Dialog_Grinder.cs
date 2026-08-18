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
    public string selectedRecipe;
    public override Vector2 InitialSize => new Vector2(960f, 640f);

    public Dialog_Grinder()
    {
        closeOnClickedOutside = true;
        doCloseX = true;
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

        List<string> queuedRecipes = ArchipelagoGameComponent.GetCraftRecipesQueued();
        Rect addSectionRect = buttonSection.LeftHalf();
        Rect addButtonRect = new Rect(addSectionRect.x + 8f, addSectionRect.y + 8f, (addSectionRect.width - 16f) / 2 , 46f);
        if (Widgets.ButtonText(addButtonRect, "Add", active: !string.IsNullOrEmpty(selectedRecipe) && !queuedRecipes.Contains(selectedRecipe)))
        {
            ArchipelagoGameComponent.QueueCraftLocation(selectedRecipe);
        }
        Rect addAllButtonRect = new Rect(addSectionRect.x + addSectionRect.width / 2 + 8f, addSectionRect.y + 8f, (addSectionRect.width - 16f) / 2 - 8f, 46f);
        if (Widgets.ButtonText(addAllButtonRect, "Add All"))
        {
            ArchipelagoGameComponent.QueueAllCrafts();
        }


        Rect removeSectionRect = buttonSection.RightHalf();
        Rect removeButtonRect = new Rect(removeSectionRect.x + 8f, removeSectionRect.y + 8f, (removeSectionRect.width - 16f) / 2, 46f);
        if (Widgets.ButtonText(removeButtonRect, "Remove", active: !string.IsNullOrEmpty(selectedRecipe) && queuedRecipes.Contains(selectedRecipe)))
        {
           ArchipelagoGameComponent.RemoveQueuedCraftLocation(selectedRecipe);
        }
        Rect removeAllButtonRect = new Rect(removeSectionRect.x + removeSectionRect.width / 2 + 8f, removeSectionRect.y + 8f, (removeSectionRect.width - 16f) / 2 - 8f, 46f);
        if (Widgets.ButtonText(removeAllButtonRect, "Remove All"))
        {
            ArchipelagoGameComponent.ClearQueuedCrafts();
        }

        Rect confirmButtonRect = buttonSection.BottomHalf();
        confirmButtonRect = confirmButtonRect.MiddlePartPixels(120f, 46f);
        confirmButtonRect = new Rect(confirmButtonRect.x, confirmButtonRect.y + 8f, confirmButtonRect.width, confirmButtonRect.height - 8f);
        if (Widgets.ButtonText(confirmButtonRect, "Confirm"))
        {
            Close();
        }
    }

    private void DoUnqueuedList(Rect inRect)
    {
        List<string> queuedRecipes = ArchipelagoGameComponent.GetCraftRecipesQueued();
        Widgets.DrawMenuSection(inRect);
        var position = inRect.position;
        position += new Vector2(8f, 8f);
        Rect viewRect = new Rect
        {
            x = position.x,
            y = position.y,
            width = inRect.width,
            height = 16 + 80 * APCraftManager.craftRecipesToArchipelagoIds.Count
        };
        position += new Vector2(8f, 8f);
        Widgets.BeginScrollView(inRect, ref unqueuedListScrollPos, viewRect, true);
        foreach (string id in APCraftManager.craftRecipesToArchipelagoIds.Keys)
        {
            if (queuedRecipes.Contains(id))
            {
                continue;
            }

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
                Log.Message($"Selected {id}");
                selectedRecipe = id;
            }
            position.y += 80f;
        }
        Widgets.EndScrollView();
    }

    private void DoQueuedList(Rect inRect)
    {
        List<string> queuedRecipes = ArchipelagoGameComponent.GetCraftRecipesQueued();
        Widgets.DrawMenuSection(inRect);
        var position = inRect.position;
        position += new Vector2(8f, 8f);
        Rect viewRect = new Rect
        {
            x = position.x,
            y = position.y,
            width = inRect.width,
            height = 16 + 80 * queuedRecipes.Count
        };
        position += new Vector2(8f, 8f);
        Widgets.BeginScrollView(inRect, ref queuedListScrollPos, viewRect, true);
        foreach (string id in queuedRecipes)
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
                Log.Message($"Selected {id}");
                selectedRecipe = id;
            }
            position.y += 80f;
        }
        Widgets.EndScrollView();
    }
}
