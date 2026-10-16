using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    [Serializable]
    public class CollisionRule
    {
        public string label;
        [Tooltip("Instances of this kind trigger the rule when they collide with this prefab.")]
        public EntityKind incomingKind;
        public StatId targetStat;
        public float targetDelta;
        public bool hasSecondaryStat;
        public StatId secondaryStat;
        public float secondaryDelta;
        [Tooltip("Only react while the target stat is above this value (e.g. a mouse trap needs Bait).")]
        public bool requireTargetStatAbove;
        public float requiredValue;
        [Tooltip("Destroy the incoming Instance (it is 'used up' by the collision).")]
        public bool destroyIncoming = true;
        [Tooltip("Player resource changes (negative amounts are penalties).")]
        public List<ResourceAmount> rewards = new List<ResourceAmount>();
        public Color color = Color.white;
        public string tutorialId;
    }

    /// <summary>
    /// Collision script: when an Instance of a listed kind collides with this prefab, the incoming Instance is
    /// Destroyed and this Instance's Stats change (ice cube -> fryer Heat drops, rat -> mouse trap Bait drops...).
    /// </summary>
    public class CollisionReactor : MonoBehaviour
    {
        public StatBlock target;
        public List<CollisionRule> rules = new List<CollisionRule>();
        public Transform feedbackPoint;

        public event Action<CollisionRule, KitchenEntity> Reacted;

        void OnTriggerEnter(Collider other) => Handle(other);

        void OnCollisionEnter(Collision collision) => Handle(collision.collider);

        void Handle(Collider other)
        {
            if (target == null || target.IsDead)
                return;
            var incoming = KitchenEntity.FromCollider(other);
            if (incoming == null || incoming.Consumed)
                return;
            if (incoming.transform == transform || incoming.transform.IsChildOf(transform))
                return;

            foreach (var rule in rules)
            {
                if (rule.incomingKind != incoming.kind)
                    continue;
                if (!target.Has(rule.targetStat))
                    continue;
                if (rule.requireTargetStatAbove && target.Get(rule.targetStat) <= rule.requiredValue)
                    continue;
                Apply(rule, incoming);
                break;
            }
        }

        void Apply(CollisionRule rule, KitchenEntity incoming)
        {
            var pos = feedbackPoint != null ? feedbackPoint.position : transform.position + Vector3.up;
            if (rule.destroyIncoming)
                incoming.Consume();

            target.Add(rule.targetStat, rule.targetDelta);
            if (rule.hasSecondaryStat && target.Has(rule.secondaryStat))
                target.Add(rule.secondaryStat, rule.secondaryDelta);

            var res = KitchenGame.Res;
            if (res != null)
                foreach (var r in rule.rewards)
                    res.Add(r.id, r.amount);

            KitchenGame.Float(pos, rule.label, rule.color);
            KitchenGame.Blast(incoming.transform.position, rule.color, 0.35f);
            KitchenGame.Tutorial("collision", pos);
            if (!string.IsNullOrEmpty(rule.tutorialId))
                KitchenGame.Tutorial(rule.tutorialId, pos);
            Reacted?.Invoke(rule, incoming);
        }
    }
}
