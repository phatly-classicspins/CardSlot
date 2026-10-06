using System.Collections.Generic;
using UnityEngine;

namespace Game.Views
{
    /// <summary>
    /// Procedural meshes for the 3D board (CR-003: models are built in code). Units are world units on the
    /// rig ruler (1 px = 1 unit). Local frame: the shape lies in the XY plane, centred on the origin; its
    /// thickness grows toward the camera along −Z (z ∈ [−depth, 0]). Submesh 0 is the top face, UV-mapped
    /// 0..1 across width/height so a card face texture fits it; submesh 1 is the sides and the bottom.
    /// </summary>
    public static class MeshKit
    {
        public static Mesh RoundedSlab(float w, float h, float depth, float radius, int cornerSegments = 6, float bevel = 0f)
        {
            radius = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            var outline = RoundedRect(w, h, radius, cornerSegments);
            var inset = bevel > 0f ? RoundedRect(w - 2f * bevel, h - 2f * bevel, Mathf.Max(0f, radius - bevel), cornerSegments) : outline;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var top = new List<int>();
            var rest = new List<int>();
            float zTop = -depth, zBevel = bevel > 0f ? -depth + bevel : -depth;

            // top face: fan from the centre over the (inset) outline
            int centre = verts.Count;
            verts.Add(new Vector3(0f, 0f, zTop)); normals.Add(Vector3.back); uvs.Add(new Vector2(0.5f, 0.5f));
            int topStart = verts.Count;
            foreach (var p in inset) { verts.Add(new Vector3(p.x, p.y, zTop)); normals.Add(Vector3.back); uvs.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f)); }
            int n = inset.Count;
            for (int i = 0; i < n; i++) { top.Add(centre); top.Add(topStart + (i + 1) % n); top.Add(topStart + i); }

            // bevel ring (top inset → outer edge), then the vertical wall, then the bottom
            if (bevel > 0f) Ring(verts, normals, uvs, rest, inset, zTop, outline, zBevel);
            Ring(verts, normals, uvs, rest, outline, zBevel, outline, 0f);
            int bottomCentre = verts.Count;
            verts.Add(Vector3.zero); normals.Add(Vector3.forward); uvs.Add(new Vector2(0.5f, 0.5f));
            int bottomStart = verts.Count;
            foreach (var p in outline) { verts.Add(new Vector3(p.x, p.y, 0f)); normals.Add(Vector3.forward); uvs.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f)); }
            for (int i = 0; i < outline.Count; i++) { rest.Add(bottomCentre); rest.Add(bottomStart + i); rest.Add(bottomStart + (i + 1) % outline.Count); }

            var mesh = new Mesh { name = $"RoundedSlab {w}x{h}x{depth}" };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0); mesh.SetTriangles(rest, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Cylinder(float radius, float depth, int segments = 40) =>
            RoundedSlab(radius * 2f, radius * 2f, depth, radius, segments / 4);

        // a side strip between two outlines at two depths, with outward normals
        private static void Ring(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> tri, List<Vector2> a, float za, List<Vector2> b, float zb)
        {
            int n = a.Count, start = v.Count;
            for (int i = 0; i < n; i++)
            {
                var prev = b[(i - 1 + n) % n]; var next = b[(i + 1) % n];
                var t = (next - prev).normalized;
                var outward = new Vector3(t.y, -t.x, 0f);
                var slope = new Vector3(b[i].x - a[i].x, b[i].y - a[i].y, zb - za);
                var normal = (outward + (slope.sqrMagnitude > 0.01f ? Vector3.back * 0.5f : Vector3.zero)).normalized;
                v.Add(new Vector3(a[i].x, a[i].y, za)); nrm.Add(normal); uv.Add(new Vector2((float)i / n, 1f));
                v.Add(new Vector3(b[i].x, b[i].y, zb)); nrm.Add(normal); uv.Add(new Vector2((float)i / n, 0f));
            }
            for (int i = 0; i < n; i++)
            {
                int i0 = start + 2 * i, i1 = start + 2 * ((i + 1) % n);
                tri.Add(i0); tri.Add(i1); tri.Add(i0 + 1);
                tri.Add(i1); tri.Add(i1 + 1); tri.Add(i0 + 1);
            }
        }

        // counter-clockwise outline of a rounded rectangle centred on the origin
        private static List<Vector2> RoundedRect(float w, float h, float r, int seg)
        {
            var pts = new List<Vector2>();
            float hx = w * 0.5f - r, hy = h * 0.5f - r;
            var centres = new[] { new Vector2(hx, hy), new Vector2(-hx, hy), new Vector2(-hx, -hy), new Vector2(hx, -hy) };
            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= seg; s++)
                {
                    float a = (c * 90f + 90f * s / seg) * Mathf.Deg2Rad;
                    pts.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            return pts;
        }
    }
}
