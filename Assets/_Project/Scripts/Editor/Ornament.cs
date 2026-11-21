using UnityEngine;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>Reusable wall ornament from reference A: teal enamel inset panels in brass frames with a rondel boss.</summary>
    public static class Ornament
    {
        /// <summary>
        /// A row of framed panels on a wall face. <paramref name="from"/>/<paramref name="to"/> are the first/last panel
        /// centres on the wall surface (local to <paramref name="parent"/>), <paramref name="inward"/> points into the room.
        /// Decorative only: no colliders.
        /// </summary>
        public static void PanelRow(Transform parent, string name, Vector3 from, Vector3 to, Vector3 inward, int count,
            float width = 1.4f, float height = 1.3f, Material panel = null)
        {
            panel ??= StationMaterials.TealPanel;
            var rot = Quaternion.LookRotation(-inward, Vector3.up); // local -Z faces the room
            for (int i = 0; i < count; i++)
            {
                var c = count == 1 ? from : Vector3.Lerp(from, to, i / (float)(count - 1));
                var g = Group($"{name}_{i}", parent, c + inward * 0.012f).transform;
                g.localRotation = rot;
                Box("Panel", g, Vector3.zero, new Vector3(width, height, 0.02f), panel, false);
                Box("FrameT", g, new Vector3(0, height * 0.5f, -0.02f), new Vector3(width + 0.05f, 0.05f, 0.04f), StationMaterials.Brass, false);
                Box("FrameB", g, new Vector3(0, -height * 0.5f, -0.02f), new Vector3(width + 0.05f, 0.05f, 0.04f), StationMaterials.Brass, false);
                Box("FrameL", g, new Vector3(-width * 0.5f, 0, -0.02f), new Vector3(0.05f, height, 0.04f), StationMaterials.Brass, false);
                Box("FrameR", g, new Vector3(width * 0.5f, 0, -0.02f), new Vector3(0.05f, height, 0.04f), StationMaterials.Brass, false);
                Cylinder("Boss", g, new Vector3(0, 0, -0.02f), new Vector3(0.34f, 0.02f, 0.34f), StationMaterials.Brass, false, Quaternion.Euler(90, 0, 0));
                Cylinder("BossInner", g, new Vector3(0, 0, -0.035f), new Vector3(0.2f, 0.02f, 0.2f), StationMaterials.Terracotta, false, Quaternion.Euler(90, 0, 0));
            }
        }
    }
}
