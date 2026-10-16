using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Editor-only convenience: if you press Play without a headset (no XR display running), this switches on the
    /// XR Interaction Simulator child so you can test with mouse and keyboard. The parent object is tagged
    /// EditorOnly, so none of this ships in the Quest build.
    /// </summary>
    public class EditorSimulatorActivator : MonoBehaviour
    {
        public GameObject simulator;
        [Tooltip("Turn on the simulator even when a headset is connected.")]
        public bool forceSimulator;

        void Start()
        {
#if UNITY_EDITOR
            if (simulator == null)
                return;
            var headsetActive = UnityEngine.XR.XRSettings.isDeviceActive;
            if (forceSimulator || !headsetActive)
            {
                simulator.SetActive(true);
                Debug.Log("[MP2A] No XR headset detected - XR Interaction Simulator enabled (Editor only).");
            }
#endif
        }
    }
}
