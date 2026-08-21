using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Rounded replacements for the unit cube every panel button is built from.
    ///
    /// The mesh still spans -0.5 to 0.5 on all three axes, so a button keeps the
    /// <see cref="Transform.localScale"/> it already had and its label, beam target and collider
    /// keep their offsets. Only the four vertical edges change. Because that scale is non-uniform,
    /// the corner radius is divided by the button's width and height before it is baked, which is
    /// why a mesh belongs to one button size rather than to all of them.
    /// </summary>
    internal static class PanelButtonMeshes
    {
        private const string MeshRoot = "Assets/StargazingHill/Generated/Meshes/";
        private const int CornerSegments = 5;
        /// <summary>Corner radius as a share of the shorter side. Enough to read as rounded at arm's length.</summary>
        private const float CornerShare = 0.28f;

        internal static Mesh EnsureRoundedPlate(float width, float height)
        {
            width = Mathf.Abs(width);
            height = Mathf.Abs(height);
            if (width <= 0.0001f || height <= 0.0001f) return null;
            string name = "PanelButton_" + Mathf.RoundToInt(width * 1000f) + "x" +
                Mathf.RoundToInt(height * 1000f);
            string path = MeshRoot + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            float radius = Mathf.Min(width, height) * CornerShare;
            // Unit-space radii: the button's own scale stretches them back into a circle.
            float radiusX = Mathf.Min(radius / width, 0.5f);
            float radiusY = Mathf.Min(radius / height, 0.5f);
            Vector2[] outline = BuildOutline(radiusX, radiusY);

            Mesh mesh = existing != null ? existing : new Mesh();
            mesh.name = name;
            mesh.Clear();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            // Buttons read from local -Z, the same side every panel label faces.
            AddCap(vertices, normals, uv, triangles, outline, -0.5f, Vector3.back, true);
            AddCap(vertices, normals, uv, triangles, outline, 0.5f, Vector3.forward, false);
            AddRim(vertices, normals, uv, triangles, outline);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        /// <summary>Counter-clockwise seen from +Z, starting at the lower right corner.</summary>
        private static Vector2[] BuildOutline(float radiusX, float radiusY)
        {
            Vector2[] centres =
            {
                new Vector2(0.5f - radiusX, -(0.5f - radiusY)),
                new Vector2(0.5f - radiusX, 0.5f - radiusY),
                new Vector2(-(0.5f - radiusX), 0.5f - radiusY),
                new Vector2(-(0.5f - radiusX), -(0.5f - radiusY))
            };
            var points = new Vector2[4 * (CornerSegments + 1)];
            int next = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                float start = (corner - 1) * 90f;
                for (int step = 0; step <= CornerSegments; step++)
                {
                    float radians = (start + 90f * step / CornerSegments) * Mathf.Deg2Rad;
                    points[next++] = centres[corner] + new Vector2(
                        Mathf.Cos(radians) * radiusX, Mathf.Sin(radians) * radiusY);
                }
            }
            return points;
        }

        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
            List<int> triangles, Vector2[] outline, float z, Vector3 normal, bool reverseWinding)
        {
            int first = vertices.Count;
            for (int index = 0; index < outline.Length; index++)
            {
                vertices.Add(new Vector3(outline[index].x, outline[index].y, z));
                normals.Add(normal);
                uv.Add(new Vector2(outline[index].x + 0.5f, outline[index].y + 0.5f));
            }
            for (int index = 1; index < outline.Length - 1; index++)
            {
                triangles.Add(first);
                triangles.Add(first + (reverseWinding ? index + 1 : index));
                triangles.Add(first + (reverseWinding ? index : index + 1));
            }
        }

        private static void AddRim(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
            List<int> triangles, Vector2[] outline)
        {
            for (int index = 0; index < outline.Length; index++)
            {
                Vector2 here = outline[index];
                Vector2 ahead = outline[(index + 1) % outline.Length];
                Vector2 edge = ahead - here;
                // Outward in the XY plane: the outline runs counter-clockwise seen from +Z.
                Vector3 normal = new Vector3(edge.y, -edge.x, 0f).normalized;
                int first = vertices.Count;
                vertices.Add(new Vector3(here.x, here.y, -0.5f));
                vertices.Add(new Vector3(ahead.x, ahead.y, -0.5f));
                vertices.Add(new Vector3(ahead.x, ahead.y, 0.5f));
                vertices.Add(new Vector3(here.x, here.y, 0.5f));
                for (int corner = 0; corner < 4; corner++) normals.Add(normal);
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(1f, 0f));
                uv.Add(new Vector2(1f, 1f));
                uv.Add(new Vector2(0f, 1f));
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
                triangles.Add(first + 3);
                triangles.Add(first + 2);
            }
        }
    }
}
