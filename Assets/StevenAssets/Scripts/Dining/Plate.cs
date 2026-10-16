using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Steven.MP2A
{
    /// <summary>
    /// A plated dish. It rides the Pass Conveyor along the designer-defined path (Path Following); grab it and
    /// carry it to the table that ordered it. Its Warmth stat cools down over time (Euler), lowering the tip.
    /// </summary>
    public class Plate : MonoBehaviour
    {
        public KitchenEntity entity;
        public StatBlock stats;
        public XRGrabInteractable grab;
        public Rigidbody body;
        public Renderer foodRenderer;
        public Text label;

        public ResourceId dish;
        public DiningTable table;

        PassConveyor m_Conveyor;
        float m_FloorTimer;

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");

        public bool OnConveyor => m_Conveyor != null;

        public void Setup(ResourceId dishId, DiningTable forTable, PassConveyor conveyor)
        {
            dish = dishId;
            table = forTable;
            m_Conveyor = conveyor;
            var db = KitchenGame.Db;
            var info = db != null ? db.GetResource(dishId) : null;
            if (foodRenderer != null && info != null)
            {
                var block = new MaterialPropertyBlock();
                foodRenderer.GetPropertyBlock(block);
                block.SetColor(s_BaseColor, info.color);
                foodRenderer.SetPropertyBlock(block);
            }
            if (label != null)
                label.text = (forTable != null ? "T" + forTable.tableNumber + "\n" : "") + KitchenGame.ResourceName(dishId);
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        /// <summary>Persistent listener target: XRGrabInteractable.selectEntered.</summary>
        public void OnGrabbed(SelectEnterEventArgs args)
        {
            if (m_Conveyor != null)
            {
                m_Conveyor.Release(this);
                m_Conveyor = null;
            }
            KitchenGame.Tutorial("plate_delivery", transform.position + Vector3.up * 0.3f);
        }

        /// <summary>Persistent listener target: XRGrabInteractable.selectExited.</summary>
        public void OnReleased(SelectExitEventArgs args)
        {
            // XRI restores the "kinematic" state the plate had on the conveyor; make it a normal physics object.
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }

        /// <summary>Called by the conveyor when the plate is removed from the belt without being grabbed.</summary>
        internal void DetachFromConveyor()
        {
            m_Conveyor = null;
        }

        void Update()
        {
            if (OnConveyor || (grab != null && grab.isSelected))
            {
                m_FloorTimer = 0f;
                return;
            }
            // Dropped on the floor: it's ruined after a moment.
            var floorY = KitchenGame.Grid != null ? KitchenGame.Grid.origin.y : 0f;
            if (transform.position.y < floorY + 0.12f)
            {
                m_FloorTimer += Time.deltaTime;
                if (m_FloorTimer > 1.5f)
                    Ruin("Dropped plate!");
            }
            else
            {
                m_FloorTimer = 0f;
            }
        }

        void Ruin(string why)
        {
            if (entity != null && entity.Consumed)
                return;
            KitchenGame.Float(transform.position + Vector3.up * 0.4f, why + " Re-fire the ticket", new Color(1f, 0.4f, 0.3f));
            if (KitchenGame.Res != null)
                KitchenGame.Res.Add(ResourceId.Temper, 6f);
            if (table != null)
                table.PlateLost();
            if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (m_Conveyor != null)
                m_Conveyor.Release(this);
        }
    }
}
