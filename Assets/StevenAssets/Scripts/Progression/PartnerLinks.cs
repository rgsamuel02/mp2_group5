using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Cross-scene unlocks the kitchen gives to teammates' sims. Put it on the Progression object.
    /// After the kitchen clears goal <see cref="unlockOrchestraAfterGoal"/> ("Partner Deliveries" by default), Bryce's
    /// Orchestra may send instruments to the restaurant (Integrator.UnlockRestaurant). The unlock survives leaving the
    /// kitchen because Integrator keeps it in a static field.
    /// </summary>
    public class PartnerLinks : MonoBehaviour
    {
        [Tooltip("Goals the kitchen must clear before the Orchestra can send instruments here (5 = after Partner Deliveries).")]
        public int unlockOrchestraAfterGoal = 5;
        public KitchenProgression progression;

        bool m_OrchestraUnlocked;
        float m_Timer;

        void Start()
        {
            if (progression == null)
                progression = GetComponent<KitchenProgression>();
        }

        void Update()
        {
            if (m_OrchestraUnlocked || progression == null)
                return;
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f)
                return;
            m_Timer = 0.5f;
            if (progression.GoalsCleared < unlockOrchestraAfterGoal)
                return;

            m_OrchestraUnlocked = true;
            var first = !Integrator.rEnabled;
            Integrator.UnlockRestaurant();
            if (first)
            {
                var game = KitchenGame.Instance;
                var head = game != null ? game.PlayerHeadPosition : Vector3.up * 1.6f;
                KitchenGame.Float(head + Vector3.forward * 0.9f + Vector3.up * 0.2f,
                    "The Orchestra can now send instruments to your dining room!", new Color(0.7f, 0.6f, 1f), 1.1f);
            }
        }
    }
}
