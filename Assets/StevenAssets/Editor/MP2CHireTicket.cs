using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// Tools > Steven > Polish > Restore Hire Cook Ticket  (open StevenScene first)
    /// Remove Hidden Kitchen Content deleted the Staff Board's HIRE COOK ticket. This rebuilds it from the supplier's
    /// ticket (same look), wires it to the Staff Board and shows it whenever the Staff Board is unlocked.
    /// </summary>
    public static class MP2CHireTicket
    {
        [MenuItem("Tools/Steven/Polish/Restore Hire Cook Ticket")]
        public static void Restore()
        {
            var scene = SceneManager.GetActiveScene();
            CraftingStation staff = null, supplier = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var st in root.GetComponentsInChildren<CraftingStation>(true))
                {
                    if (st.recipeIds.Contains("hire_cook"))
                        staff = st;
                    else if (st.stationKey == "Supplier")
                        supplier = st;
                }
            if (staff == null || supplier == null)
            {
                EditorUtility.DisplayDialog("Hire Cook Ticket", "Staff Board or Supplier Board not found. Open StevenScene first.", "OK");
                return;
            }

            var index = staff.recipeIds.IndexOf("hire_cook");
            while (staff.recipeCards.Count <= index)
                staff.recipeCards.Add(null);
            if (staff.recipeCards[index] != null)
            {
                EditorUtility.DisplayDialog("Hire Cook Ticket", "The Staff Board already has its HIRE COOK ticket.", "OK");
                return;
            }

            PressableCard template = null;
            foreach (var c in supplier.recipeCards)
                if (c != null)
                {
                    template = c;
                    break;
                }
            if (template == null)
            {
                EditorUtility.DisplayDialog("Hire Cook Ticket", "No supplier ticket to copy.", "OK");
                return;
            }

            var copy = Object.Instantiate(template.gameObject, staff.transform);
            copy.name = "Ticket_hire_cook";
            copy.transform.localPosition = new Vector3(0f, -0.3f, 0f);   // where the original was
            copy.transform.localRotation = Quaternion.identity;
            copy.SetActive(true);
            var card = copy.GetComponent<PressableCard>();
            card.tutorialId = "staff_board";
            while (card.onPressed.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(card.onPressed, 0);
            UnityEventTools.AddIntPersistentListener(card.onPressed, staff.StartRecipeAt, index);
            card.SetState("Line Cook", "", new Color(0.12f, 0.38f, 0.16f, 0.95f));
            staff.recipeCards[index] = card;
            EditorUtility.SetDirty(card);
            EditorUtility.SetDirty(staff);

            // Show it whenever the Staff Board itself is showing (the board unlocks at Dinner Service).
            foreach (var root in scene.GetRootGameObjects())
                foreach (var simp in root.GetComponentsInChildren<KitchenSimplifier>(true))
                {
                    if (!simp.startRecipes.Contains("hire_cook"))
                        simp.startRecipes.Add("hire_cook");
                    EditorUtility.SetDirty(simp);
                }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = copy;
            EditorUtility.DisplayDialog("Hire Cook Ticket", "Rebuilt the HIRE COOK ticket on the Staff Board. Scene saved.", "OK");
        }
    }
}
