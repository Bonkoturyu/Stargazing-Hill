using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Flat single-colour section icons for the local settings board, built in code rather than
    /// imported. The board already draws its gear this way: a generated mesh needs no third-party
    /// licence entry, keeps every icon on one line weight, and carries no texture to Quest or iOS.
    /// </summary>
    /// <remarks>
    /// Shapes are authored in a roughly -0.7..0.7 square on the XY plane and face local -Z, the
    /// reader side of every panel in this world. Winding therefore matches the gear: points are
    /// given counter-clockwise as seen from +Z and emitted reversed.
    /// </remarks>
    internal static class SettingsIconMeshes
    {
        private const string MeshRoot = "Assets/StargazingHill/Generated/Meshes/";

        internal static Mesh EnsureMoon()
        {
            Builder builder = new Builder();
            // Crescent: the region between an outer disc and a smaller disc pushed to the right.
            // Both arcs run between the two intersection points, so walking them together fills it.
            builder.AddCrescent(0.62f, 0.56f, 0.17f, 48);
            return builder.Save("SettingsIconMoon");
        }

        internal static Mesh EnsureVolume()
        {
            Builder builder = new Builder();
            builder.AddRect(-0.58f, -0.20f, -0.26f, 0.20f);
            // Cone, opening away from the waves.
            builder.AddPolygon(new Vector2(-0.26f, -0.20f), new Vector2(0.04f, -0.58f),
                new Vector2(0.04f, 0.58f), new Vector2(-0.26f, 0.20f));
            builder.AddRingSector(new Vector2(0.04f, 0f), 0.28f, 0.38f, -52f, 52f, 20);
            builder.AddRingSector(new Vector2(0.04f, 0f), 0.50f, 0.60f, -52f, 52f, 20);
            return builder.Save("SettingsIconVolume");
        }

        /// <summary>Bed seen from above, so it reads as the mat the mirrors stand around.</summary>
        internal static Mesh EnsureBed()
        {
            Builder builder = new Builder();
            builder.AddOutlineRect(-0.52f, -0.72f, 0.52f, 0.72f, 0.10f);
            builder.AddRect(-0.34f, 0.30f, 0.34f, 0.58f);   // pillow
            builder.AddRect(-0.42f, 0.06f, 0.42f, 0.16f);   // turned-down blanket edge
            return builder.Save("SettingsIconBed");
        }

        internal static Mesh EnsureBell()
        {
            Builder builder = new Builder();
            builder.AddRect(-0.07f, 0.52f, 0.07f, 0.66f);                    // handle
            builder.AddRingSector(new Vector2(0f, 0.06f), 0f, 0.42f, 0f, 180f, 24);
            builder.AddRect(-0.42f, -0.22f, 0.42f, 0.06f);                   // skirt
            builder.AddRect(-0.56f, -0.34f, 0.56f, -0.22f);                  // rim
            builder.AddDisc(new Vector2(0f, -0.48f), 0.13f, 16);             // clapper
            return builder.Save("SettingsIconBell");
        }

        /// <summary>Sheet of paper with ruled lines: the join/leave report.</summary>
        internal static Mesh EnsureReport()
        {
            Builder builder = new Builder();
            builder.AddOutlineRect(-0.46f, -0.68f, 0.46f, 0.68f, 0.09f);
            builder.AddRect(-0.28f, 0.30f, 0.28f, 0.40f);
            builder.AddRect(-0.28f, 0.06f, 0.28f, 0.16f);
            builder.AddRect(-0.28f, -0.18f, 0.28f, -0.08f);
            builder.AddRect(-0.28f, -0.42f, 0.06f, -0.32f);
            return builder.Save("SettingsIconReport");
        }

        /// <summary>Diagonal bar laid over an icon to read as "off".</summary>
        internal static Mesh EnsureSlash()
        {
            Builder builder = new Builder();
            builder.AddPolygon(new Vector2(-0.70f, -0.58f), new Vector2(-0.58f, -0.70f),
                new Vector2(0.70f, 0.58f), new Vector2(0.58f, 0.70f));
            return builder.Save("SettingsIconSlash");
        }

        internal static Mesh EnsureAlarm()
        {
            Builder builder = new Builder();
            builder.AddRingSector(Vector2.zero, 0.40f, 0.50f, 0f, 360f, 40);
            builder.AddDisc(new Vector2(-0.44f, 0.44f), 0.15f, 16);   // bells
            builder.AddDisc(new Vector2(0.44f, 0.44f), 0.15f, 16);
            builder.AddPolygon(new Vector2(-0.52f, -0.42f), new Vector2(-0.28f, -0.60f),
                new Vector2(-0.16f, -0.46f));                          // feet
            builder.AddPolygon(new Vector2(0.16f, -0.46f), new Vector2(0.28f, -0.60f),
                new Vector2(0.52f, -0.42f));
            builder.AddRect(-0.04f, -0.02f, 0.04f, 0.28f);             // hands
            builder.AddRect(-0.02f, -0.06f, 0.24f, 0.04f);
            return builder.Save("SettingsIconAlarm");
        }

        internal static Mesh EnsurePresence()
        {
            Builder builder = new Builder();
            // Doorway drawn as an outline, with an arrow passing through it.
            builder.AddRect(-0.62f, -0.62f, -0.48f, 0.62f);
            builder.AddRect(-0.62f, 0.48f, 0.04f, 0.62f);
            builder.AddRect(-0.62f, -0.62f, 0.04f, -0.48f);
            builder.AddRect(-0.10f, -0.62f, 0.04f, -0.20f);
            builder.AddRect(-0.10f, 0.20f, 0.04f, 0.62f);
            builder.AddRect(0.16f, -0.07f, 0.48f, 0.07f);
            builder.AddPolygon(new Vector2(0.44f, -0.24f), new Vector2(0.70f, 0f),
                new Vector2(0.44f, 0.24f));
            return builder.Save("SettingsIconPresence");
        }

        private sealed class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            internal void AddRect(float minX, float minY, float maxX, float maxY)
            {
                AddPolygon(new Vector2(minX, minY), new Vector2(maxX, minY),
                    new Vector2(maxX, maxY), new Vector2(minX, maxY));
            }

            /// <summary>Convex fan. Points are counter-clockwise seen from +Z.</summary>
            internal void AddPolygon(params Vector2[] points)
            {
                if (points.Length < 3) throw new InvalidOperationException("Icon polygon needs 3 points.");
                int first = _vertices.Count;
                for (int index = 0; index < points.Length; index++) AddVertex(points[index]);
                for (int index = 1; index < points.Length - 1; index++)
                {
                    // Reversed on purpose: the icon faces local -Z like every other panel element.
                    _triangles.Add(first);
                    _triangles.Add(first + index + 1);
                    _triangles.Add(first + index);
                }
            }

            /// <summary>Four bars forming a hollow rectangle.</summary>
            internal void AddOutlineRect(float minX, float minY, float maxX, float maxY, float thickness)
            {
                AddRect(minX, maxY - thickness, maxX, maxY);
                AddRect(minX, minY, maxX, minY + thickness);
                AddRect(minX, minY + thickness, minX + thickness, maxY - thickness);
                AddRect(maxX - thickness, minY + thickness, maxX, maxY - thickness);
            }

            internal void AddDisc(Vector2 center, float radius, int segments)
            {
                AddRingSector(center, 0f, radius, 0f, 360f, segments);
            }

            internal void AddRingSector(Vector2 center, float inner, float outer,
                float startDegrees, float endDegrees, int segments)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    float a0 = Mathf.Deg2Rad * Mathf.Lerp(startDegrees, endDegrees, segment / (float)segments);
                    float a1 = Mathf.Deg2Rad * Mathf.Lerp(startDegrees, endDegrees, (segment + 1) / (float)segments);
                    Vector2 outer0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outer;
                    Vector2 outer1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outer;
                    if (inner <= 0f)
                    {
                        AddPolygon(center, outer0, outer1);
                        continue;
                    }
                    Vector2 inner0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * inner;
                    Vector2 inner1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * inner;
                    AddPolygon(inner0, outer0, outer1, inner1);
                }
            }

            /// <summary>
            /// Region between a disc of <paramref name="outerRadius"/> at the origin and a disc of
            /// <paramref name="innerRadius"/> offset by <paramref name="offset"/> along +X.
            /// </summary>
            internal void AddCrescent(float outerRadius, float innerRadius, float offset, int segments)
            {
                float crossX = (offset * offset + outerRadius * outerRadius - innerRadius * innerRadius) /
                    (2f * offset);
                float crossY = Mathf.Sqrt(Mathf.Max(0f, outerRadius * outerRadius - crossX * crossX));
                if (crossY <= 0f) throw new InvalidOperationException("Crescent discs do not intersect.");
                float outerStart = Mathf.Atan2(crossY, crossX);
                float outerEnd = Mathf.PI * 2f - outerStart;
                float innerStart = Mathf.Atan2(crossY, crossX - offset);
                float innerEnd = Mathf.PI * 2f - innerStart;

                for (int segment = 0; segment < segments; segment++)
                {
                    float t0 = segment / (float)segments;
                    float t1 = (segment + 1) / (float)segments;
                    Vector2 outer0 = ArcPoint(Vector2.zero, outerRadius, Mathf.Lerp(outerStart, outerEnd, t0));
                    Vector2 outer1 = ArcPoint(Vector2.zero, outerRadius, Mathf.Lerp(outerStart, outerEnd, t1));
                    Vector2 inner0 = ArcPoint(new Vector2(offset, 0f), innerRadius,
                        Mathf.Lerp(innerStart, innerEnd, t0));
                    Vector2 inner1 = ArcPoint(new Vector2(offset, 0f), innerRadius,
                        Mathf.Lerp(innerStart, innerEnd, t1));
                    AddPolygon(inner0, outer0, outer1, inner1);
                }
            }

            private static Vector2 ArcPoint(Vector2 center, float radius, float radians)
            {
                return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            }

            private void AddVertex(Vector2 point)
            {
                _vertices.Add(new Vector3(point.x, point.y, 0f));
                _normals.Add(Vector3.back);
                // The icons are flat colour, but the dynamic batcher merges these small meshes and
                // reads UV0 off every one of them. A mesh without the channel crashes it, so keep
                // the channel present and map the unit square across the icon.
                _uv.Add(new Vector2(point.x + 0.5f, point.y + 0.5f));
            }

            internal Mesh Save(string name)
            {
                string path = MeshRoot + name + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                bool create = mesh == null;
                if (mesh == null) mesh = new Mesh();
                mesh.name = name;
                mesh.Clear();
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uv);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                if (create) AssetDatabase.CreateAsset(mesh, path);
                EditorUtility.SetDirty(mesh);
                return mesh;
            }
        }
    }
}
