using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Steven.MP2A
{
    /// <summary>
    /// Building Affordance ("Equipment Catalog" metaphor): a miniature of a piece of kitchen equipment sits on
    /// the catalog shelf. Grab it, hold it over the kitchen floor and a full-size snapped ghost appears on the grid.
    /// Release (or squeeze the trigger to "stamp" and keep holding) to install it for its price.
    /// The mini then springs back to its catalog slot.
    /// XR events are wired to the public methods below as persistent UnityEvent listeners in the Inspector.
    /// </summary>
    public class BlueprintMini : MonoBehaviour
    {
        public BuildingId buildingId;
        public XRGrabInteractable grab;
        public Rigidbody body;

        [Tooltip("Where the mini returns to on the catalog shelf (an empty Transform on the shelf).")]
        public Transform homeAnchor;

        public Text priceLabel;

        [Tooltip("Seconds to fly back to the shelf after release.")]
        public float returnDuration = 0.45f;

        [Tooltip("The mini must be at least this far from its shelf slot to try building (prevents accidental builds).")]
        public float minDistanceFromHome = 0.35f;

        PlacementGhost m_Ghost;
        bool m_Held;
        bool m_Returning;
        float m_ReturnT;
        Vector3 m_ReturnFromPos;
        Quaternion m_ReturnFromRot;
        Vector2Int m_Cell;
        int m_RotationSteps;
        PlacementResult m_Check;
        float m_StampCooldown;

        BuildingInfo Info => KitchenGame.Db != null ? KitchenGame.Db.GetBuilding(buildingId) : null;

        void Start()
        {
            RefreshPriceLabel();
        }

        public void RefreshPriceLabel()
        {
            var info = Info;
            if (priceLabel == null || info == null || KitchenGame.Db == null)
                return;
            priceLabel.text = info.displayName + "\n" + KitchenGame.Db.Describe(info.cost);
        }

        // ------------------------------------------------------------ XR event targets (persistent listeners)

        public void OnGrabbed(SelectEnterEventArgs args)
        {
            m_Held = true;
            m_Returning = false;
            var info = Info;
            if (info != null && info.ghostPrefab != null && m_Ghost == null)
            {
                var go = Instantiate(info.ghostPrefab);
                m_Ghost = go.GetComponent<PlacementGhost>();
                if (m_Ghost == null)
                    m_Ghost = go.AddComponent<PlacementGhost>();
                go.SetActive(false);
            }
            KitchenGame.Tutorial("catalog", transform.position);
            if (info != null && info.isPathway)
                KitchenGame.Tutorial("walkway", transform.position);
        }

        public void OnReleased(SelectExitEventArgs args)
        {
            m_Held = false;
            if (IsAwayFromHome())
                TryBuild();
            DestroyGhost();
            BeginReturn();
        }

        public void OnActivated(ActivateEventArgs args)
        {
            if (m_Held && m_StampCooldown <= 0f)
            {
                TryBuild();
                m_StampCooldown = 0.25f;
            }
        }

        // ------------------------------------------------------------ logic

        bool IsAwayFromHome() => homeAnchor == null || (transform.position - homeAnchor.position).sqrMagnitude > minDistanceFromHome * minDistanceFromHome;

        void Update()
        {
            if (m_StampCooldown > 0f)
                m_StampCooldown -= Time.deltaTime;

            if (m_Held)
            {
                UpdateGhost();
                return;
            }

            if (m_Returning && homeAnchor != null)
            {
                m_ReturnT += Time.deltaTime / Mathf.Max(0.01f, returnDuration);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_ReturnT));
                transform.SetPositionAndRotation(Vector3.Lerp(m_ReturnFromPos, homeAnchor.position, t),
                    Quaternion.Slerp(m_ReturnFromRot, homeAnchor.rotation, t));
                if (m_ReturnT >= 1f)
                    m_Returning = false;
            }
        }

        void UpdateGhost()
        {
            var grid = KitchenGame.Grid;
            if (grid == null || m_Ghost == null)
                return;

            m_Cell = grid.WorldToCell(transform.position);
            m_RotationSteps = YawToSteps(transform.eulerAngles.y);

            if (!grid.InBounds(m_Cell) || !IsAwayFromHome())
            {
                if (m_Ghost.gameObject.activeSelf)
                    m_Ghost.gameObject.SetActive(false);
                return;
            }

            m_Check = grid.CheckPlacement(buildingId, m_Cell, true);
            var pos = grid.CellCenter(m_Cell);
            pos.y = grid.SurfaceHeight(m_Cell);
            if (!m_Ghost.gameObject.activeSelf)
            {
                m_Ghost.gameObject.SetActive(true);
                KitchenGame.Tutorial("grid_snap", pos + Vector3.up * 1.2f);
            }
            m_Ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, m_RotationSteps * 90f, 0f));
            var info = Info;
            var header = info != null ? info.displayName + "  |  " + KitchenGame.Db.Describe(info.cost) : buildingId.ToString();
            m_Ghost.Show(m_Check.ok, header + "\n" + m_Check.reason);
            if (m_Check.foundationProblem)
                KitchenGame.Tutorial("foundation", pos + Vector3.up * 1.2f);
        }

        void TryBuild()
        {
            var grid = KitchenGame.Grid;
            if (grid == null)
                return;
            var cell = grid.WorldToCell(transform.position);
            var rot = YawToSteps(transform.eulerAngles.y);
            if (!grid.InBounds(cell))
                return;
            var check = grid.CheckPlacement(buildingId, cell, true);
            var pos = grid.CellCenter(cell) + Vector3.up * (grid.SurfaceHeight(cell) + 0.6f);
            if (!check.ok)
            {
                KitchenGame.Float(pos, check.reason, new Color(1f, 0.35f, 0.3f));
                if (check.foundationProblem)
                    KitchenGame.Tutorial("foundation", pos);
                return;
            }
            var built = grid.Build(buildingId, cell, rot, true);
            if (built != null)
            {
                var info = Info;
                KitchenGame.Float(pos, "Installed " + (info != null ? info.displayName : buildingId.ToString()), new Color(1f, 0.8f, 0.3f));
            }
        }

        void BeginReturn()
        {
            if (homeAnchor == null)
                return;
            m_Returning = true;
            m_ReturnT = 0f;
            m_ReturnFromPos = transform.position;
            m_ReturnFromRot = transform.rotation;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        void DestroyGhost()
        {
            if (m_Ghost != null)
                Destroy(m_Ghost.gameObject);
            m_Ghost = null;
        }

        void OnDisable()
        {
            DestroyGhost();
        }

        static int YawToSteps(float yaw)
        {
            var steps = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f) / 90f) % 4;
            return steps < 0 ? steps + 4 : steps;
        }
    }
}
