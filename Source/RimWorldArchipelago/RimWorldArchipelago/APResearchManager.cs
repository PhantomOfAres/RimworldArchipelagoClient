using Verse;

namespace RimworldArchipelago
{
    internal class APResearchManager
    {
        public static void DisableNormalResearch()
        {
            foreach (ResearchProjectDef researchProject in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (researchProject != null)
                {
                    researchProject.prerequisites = null;
                    researchProject.hiddenPrerequisites = null;
                }
            }
        }
    }
}
