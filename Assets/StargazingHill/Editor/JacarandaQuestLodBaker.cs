using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Bakes the Quest-grade Jacaranda mesh from the high-density component-selected source.
    ///
    /// The source keeps real leaf geometry, which cannot survive the reduction a mobile GPU needs: at the
    /// 6% component selection it shipped with, the canopy became scattered speckles instead of foliage while
    /// still costing 465,580 triangles. Poly Haven's leaf texture is an atlas of three complete compound
    /// fronds with an alpha mask, so the canopy is rebuilt here as alpha-tested frond cards that take their
    /// position from the branches and their direction from the discarded leaves. Trunk and branches keep
    /// their scanned silhouette through vertex clustering.
    /// </summary>
    public static class JacarandaQuestLodBaker
    {
        private const string Root = "Assets/StargazingHill";
        private const string TreeRoot = Root + "/ThirdParty/PolyHaven/JacarandaTree";
        // The component-selected intermediate is a bake input only, so it sits with the original FBX under
        // the ignored SourceDownloads tree rather than shipping its 38MB inside the repository.
        private const string SourceMeshPath =
            Root + "/SourceDownloads/JacarandaTree/Jacaranda_LOD0.asset";
        private const string OutputMeshPath = TreeRoot + "/Jacaranda_Quest.asset";

        // Submesh order is fixed by the source bake and by the builder's material array.
        private const int BranchSubMesh = 0;
        private const int TrunkSubMesh = 1;
        private const int LeafSubMesh = 2;

        // Branches carry the foliage, so every twig that survives decimation is somewhere a frond can
        // credibly grow from. The trunk needs its own generous share for a different reason: it is the one
        // surface players stand next to, and clustering a scanned trunk too hard folds the bark into flat
        // shards and twisted ribbons that are obvious at arm's length.
        private const int TargetBranchTriangles = 3400;
        private const int TargetTrunkTriangles = 7000;
        // Many small cards rather than few large ones. A card is a flat sheet, and the larger it is the more
        // it reads as one: at frond sizes above life size the crown turns into stacked slabs with straight
        // edges, and a single card caught edge-on draws a long streak across the view.
        private const int TargetLeafCards = 4800;

        /// <summary>
        /// Frond sub-rectangles inside the 1K leaf atlas, in UV space, with their pixel aspect and the side
        /// the petiole sits on. The petiole side matters: a card is anchored by its stem to a branch, so the
        /// stem edge of the rectangle has to be the edge that touches the wood.
        /// </summary>
        private static readonly FrondRect[] Fronds =
        {
            new FrondRect(0.1465f, 0.5557f, 0.9912f, 0.9756f, 2.012f, true),
            new FrondRect(0.0049f, 0.3164f, 0.4297f, 0.6289f, 1.359f, true),
            new FrondRect(0.2637f, 0.0137f, 0.9961f, 0.4092f, 1.852f, false)
        };

        private readonly struct FrondRect
        {
            public readonly float UMin;
            public readonly float VMin;
            public readonly float UMax;
            public readonly float VMax;
            public readonly float Aspect;
            public readonly bool StemAtUMin;

            public FrondRect(float uMin, float vMin, float uMax, float vMax, float aspect, bool stemAtUMin)
            {
                UMin = uMin;
                VMin = vMin;
                UMax = uMax;
                VMax = vMax;
                Aspect = aspect;
                StemAtUMin = stemAtUMin;
            }
        }

        public static void InspectSourceForBatchMode()
        {
            Mesh source = LoadRequiredMesh(SourceMeshPath);
            Vector3[] vertices = source.vertices;
            Debug.Log("[JacarandaLod] source=" + source.name + " vertices=" + source.vertexCount +
                      " submeshes=" + source.subMeshCount + " bounds=" + source.bounds.ToString("F4"));
            for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                int[] triangles = source.GetTriangles(subMesh);
                Bounds bounds = ComputeBounds(vertices, triangles);
                Debug.Log("[JacarandaLod] submesh=" + subMesh + " triangles=" + triangles.Length / 3 +
                          " bounds=" + bounds.ToString("F4"));
            }
        }

        public static void BakeForBatchMode()
        {
            Mesh source = LoadRequiredMesh(SourceMeshPath);
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;
            if (normals == null || normals.Length != vertices.Length)
                throw new InvalidOperationException("Source mesh is missing normals.");
            if (uvs == null || uvs.Length != vertices.Length)
                throw new InvalidOperationException("Source mesh is missing UVs.");

            Part branches = Decimate(vertices, normals, uvs, source.GetTriangles(BranchSubMesh),
                TargetBranchTriangles, "branches");
            Part trunk = Decimate(vertices, normals, uvs, source.GetTriangles(TrunkSubMesh),
                TargetTrunkTriangles, "trunk");
            Bounds trunkBounds = ComputeBounds(vertices, source.GetTriangles(TrunkSubMesh));
            Part leaves = BuildCanopy(branches, vertices, source.GetTriangles(LeafSubMesh), trunkBounds,
                TargetLeafCards);

            Mesh baked = Combine(branches, trunk, leaves);
            Mesh saved = SaveMesh(OutputMeshPath, baked);

            long total = 0;
            for (int subMesh = 0; subMesh < saved.subMeshCount; subMesh++)
                total += saved.GetIndexCount(subMesh) / 3L;
            Debug.Log("[JacarandaLod] baked=" + OutputMeshPath + " vertices=" + saved.vertexCount +
                      " triangles=" + total + " submeshes=" + saved.subMeshCount +
                      " bounds=" + saved.bounds.ToString("F4"));
            Debug.Log("[JacarandaLod] sha256=" + ComputeFileHash(OutputMeshPath) +
                      " bytes=" + new FileInfo(OutputMeshPath).Length);
        }

        private sealed class Part
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[] Uvs;
            public Vector4[] Tangents;
            public int[] Triangles;
        }

        /// <summary>
        /// Grid vertex clustering: collapses every vertex inside a cubic cell onto their average, then drops
        /// triangles whose corners land in one cell. Cell size is searched so the result lands just under the
        /// triangle target. Averaging (rather than snapping to cell centres) keeps the bark silhouette smooth.
        /// </summary>
        private static Part Decimate(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles,
            int targetTriangles, string label)
        {
            int low = 4;
            int high = 512;
            Part best = null;
            while (low <= high)
            {
                int resolution = (low + high) / 2;
                Part candidate = ClusterAtResolution(vertices, normals, uvs, triangles, resolution);
                int count = candidate.Triangles.Length / 3;
                if (count > targetTriangles)
                {
                    high = resolution - 1;
                }
                else
                {
                    best = candidate;
                    low = resolution + 1;
                }
            }

            if (best == null)
                throw new InvalidOperationException("Unable to reach the triangle target for " + label + ".");
            Debug.Log("[JacarandaLod] " + label + " sourceTriangles=" + triangles.Length / 3 +
                      " bakedTriangles=" + best.Triangles.Length / 3 + " vertices=" + best.Vertices.Length);
            return best;
        }

        private static Part ClusterAtResolution(Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
            int[] triangles, int resolution)
        {
            Bounds bounds = ComputeBounds(vertices, triangles);
            float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float cellSize = Mathf.Max(extent / resolution, 1e-6f);

            var cellToCluster = new Dictionary<long, int>(triangles.Length);
            var sumPosition = new List<Vector3>();
            var sumNormal = new List<Vector3>();
            var sumUv = new List<Vector2>();
            var sumCount = new List<int>();
            var vertexToCluster = new Dictionary<int, int>(triangles.Length);

            for (int index = 0; index < triangles.Length; index++)
            {
                int vertexIndex = triangles[index];
                if (vertexToCluster.ContainsKey(vertexIndex)) continue;

                Vector3 position = vertices[vertexIndex];
                long cellX = (long)Mathf.Floor((position.x - bounds.min.x) / cellSize);
                long cellY = (long)Mathf.Floor((position.y - bounds.min.y) / cellSize);
                long cellZ = (long)Mathf.Floor((position.z - bounds.min.z) / cellSize);
                long key = (cellX * 73856093L) ^ (cellY * 19349663L) ^ (cellZ * 83492791L);

                int cluster;
                if (!cellToCluster.TryGetValue(key, out cluster))
                {
                    cluster = sumPosition.Count;
                    cellToCluster.Add(key, cluster);
                    sumPosition.Add(Vector3.zero);
                    sumNormal.Add(Vector3.zero);
                    sumUv.Add(Vector2.zero);
                    sumCount.Add(0);
                }

                sumPosition[cluster] += position;
                sumNormal[cluster] += normals[vertexIndex];
                sumUv[cluster] += uvs[vertexIndex];
                sumCount[cluster] += 1;
                vertexToCluster.Add(vertexIndex, cluster);
            }

            int clusterCount = sumPosition.Count;
            var outVertices = new Vector3[clusterCount];
            var outNormals = new Vector3[clusterCount];
            var outUvs = new Vector2[clusterCount];
            for (int cluster = 0; cluster < clusterCount; cluster++)
            {
                float inverse = 1f / sumCount[cluster];
                outVertices[cluster] = sumPosition[cluster] * inverse;
                Vector3 normal = sumNormal[cluster];
                outNormals[cluster] = normal.sqrMagnitude > 1e-12f ? normal.normalized : Vector3.up;
                outUvs[cluster] = sumUv[cluster] * inverse;
            }

            var outTriangles = new List<int>(triangles.Length);
            for (int index = 0; index + 2 < triangles.Length; index += 3)
            {
                int a = vertexToCluster[triangles[index]];
                int b = vertexToCluster[triangles[index + 1]];
                int c = vertexToCluster[triangles[index + 2]];
                if (a == b || b == c || a == c) continue;
                outTriangles.Add(a);
                outTriangles.Add(b);
                outTriangles.Add(c);
            }

            return Compact(outVertices, outNormals, outUvs, outTriangles.ToArray());
        }

        /// <summary>Drops clusters that no surviving triangle references, so the baked mesh carries no dead vertices.</summary>
        private static Part Compact(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            var remap = new int[vertices.Length];
            for (int index = 0; index < remap.Length; index++) remap[index] = -1;

            var outVertices = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outUvs = new List<Vector2>();
            var outTriangles = new int[triangles.Length];
            for (int index = 0; index < triangles.Length; index++)
            {
                int source = triangles[index];
                if (remap[source] < 0)
                {
                    remap[source] = outVertices.Count;
                    outVertices.Add(vertices[source]);
                    outNormals.Add(normals[source]);
                    outUvs.Add(uvs[source]);
                }
                outTriangles[index] = remap[source];
            }

            return new Part
            {
                Vertices = outVertices.ToArray(),
                Normals = outNormals.ToArray(),
                Uvs = outUvs.ToArray(),
                Tangents = null,
                Triangles = outTriangles
            };
        }

        /// <summary>
        /// Rebuilds the canopy as alpha-tested frond cards that bridge the shipped branches and the foliage
        /// positions of the scanned tree.
        ///
        /// Two earlier attempts each failed on one half of the problem. Anchoring cards at the old leaf
        /// positions left them floating, because the twigs that carried those leaves do not survive
        /// decimation. Deriving both position and direction from the branch geometry alone fixed the
        /// attachment but invented the distribution: area-weighted sampling piles foliage onto whatever
        /// branch has the most surface, and a principal axis carries no sign, so near-vertical branches
        /// resolved to straight down and the crown hung in curtains.
        ///
        /// So each card takes its position from the branches and its direction from the leaves: the target
        /// is where the scan actually had foliage, the stem sits on the nearest drawn branch, and the frond
        /// runs from one to the other.
        /// </summary>
        private static Part BuildCanopy(Part branches, Vector3[] sourceVertices, int[] leafTriangles,
            Bounds trunkBounds, int targetCards)
        {
            Bounds branchBounds = ComputeBounds(branches.Vertices, branches.Triangles);
            float canopyRadius = Mathf.Max(branchBounds.size.x, branchBounds.size.z) * 0.5f;
            // A real jacaranda frond is 30-45cm. At the scene's 0.40 world scale that is close to one model
            // unit, and oversized cards are the fastest way to make the tree read as fake up close.
            float baseLength = canopyRadius * 0.105f;

            Vector3[] targets = SelectLeafTargets(sourceVertices, leafTriangles, targetCards);
            BranchSample[] branchPoints = SampleBranchSurface(branches, Mathf.Max(targetCards * 8, 8000));
            PointGrid grid = BuildPointGrid(branchPoints, Mathf.Max(canopyRadius * 0.05f, 1e-4f));

            int cardCount = targets.Length;
            var outVertices = new Vector3[cardCount * 4];
            var outNormals = new Vector3[cardCount * 4];
            var outUvs = new Vector2[cardCount * 4];
            var outTangents = new Vector4[cardCount * 4];
            var outTriangles = new int[cardCount * 6];
            float reachTotal = 0f;
            float reachMax = 0f;

            for (int card = 0; card < cardCount; card++)
            {
                Vector3 target = targets[card];
                uint seed = Hash((uint)card * 2654435761u + 17u);
                FrondRect frond = Fronds[seed % (uint)Fronds.Length];

                int nearest = NearestPoint(grid, target);
                BranchSample anchor = branchPoints[nearest];
                Vector3 reach = target - anchor.Position;
                float reachLength = reach.magnitude;
                reachTotal += reachLength;
                if (reachLength > reachMax) reachMax = reachLength;

                Vector3 outward = new Vector3(
                    anchor.Position.x - trunkBounds.center.x, 0f, anchor.Position.z - trunkBounds.center.z);
                outward = outward.sqrMagnitude < 1e-8f ? Vector3.forward : outward.normalized;

                // Direction is the leaf data, not a guess from the branch: it already points outward into
                // the crown, so no sign disambiguation is needed and vertical branches cannot invert.
                Vector3 growth = reachLength > 1e-5f ? reach / reachLength : (outward + Vector3.up * 0.3f).normalized;

                // Jacaranda fronds are carried outward and dip at the tip. They do not hang vertically.
                Vector3 longAxis = (growth +
                                    Vector3.down * Mathf.Lerp(0.10f, 0.30f, NextUnit(ref seed)) +
                                    new Vector3(NextSigned(ref seed), NextSigned(ref seed), NextSigned(ref seed)) * 0.18f)
                    .normalized;

                // Roll each blade around its own stem. Leaves do favour facing the sky, but only a light bias
                // survives here: a strong one lays the cards into horizontal sheets, and the crown then shows
                // flat shelves with straight edges instead of a volume.
                Vector3 face = (Vector3.up * 0.30f +
                                new Vector3(NextSigned(ref seed), NextSigned(ref seed), NextSigned(ref seed)))
                    .normalized;
                Vector3 widthAxis = Vector3.Cross(face, longAxis);
                if (widthAxis.sqrMagnitude < 1e-6f) widthAxis = Vector3.Cross(Vector3.forward, longAxis);
                if (widthAxis.sqrMagnitude < 1e-6f) widthAxis = Vector3.Cross(Vector3.right, longAxis);
                widthAxis = widthAxis.normalized;
                Vector3 cardNormal = Vector3.Cross(longAxis, widthAxis).normalized;

                // A frond may stretch a little to span the gap left by a removed twig, but only a little:
                // letting it reach the whole way produces single cards several times life size.
                float length = baseLength * Mathf.Lerp(0.75f, 1.35f, NextUnit(ref seed));
                length = Mathf.Max(length, Mathf.Min(reachLength * 1.15f, baseLength * 1.6f));
                float halfWidth = length / frond.Aspect * 0.5f;

                // Lift the stem off the bark by a fraction of a frond. Cards pinned exactly onto the branch
                // surface collapse into one shell around the wood; a small spread gives the crown depth
                // without breaking the read that the foliage is attached.
                Vector3 stem = anchor.Position + anchor.SurfaceNormal * (halfWidth * 0.06f) +
                               new Vector3(NextSigned(ref seed), NextSigned(ref seed), NextSigned(ref seed)) *
                               (baseLength * 0.14f);
                Vector3 tip = stem + longAxis * length;

                int vertexBase = card * 4;
                outVertices[vertexBase + 0] = stem - widthAxis * halfWidth;
                outVertices[vertexBase + 1] = stem + widthAxis * halfWidth;
                outVertices[vertexBase + 2] = tip + widthAxis * halfWidth;
                outVertices[vertexBase + 3] = tip - widthAxis * halfWidth;

                float stemU = frond.StemAtUMin ? frond.UMin : frond.UMax;
                float tipU = frond.StemAtUMin ? frond.UMax : frond.UMin;
                outUvs[vertexBase + 0] = new Vector2(stemU, frond.VMin);
                outUvs[vertexBase + 1] = new Vector2(stemU, frond.VMax);
                outUvs[vertexBase + 2] = new Vector2(tipU, frond.VMax);
                outUvs[vertexBase + 3] = new Vector2(tipU, frond.VMin);

                // Shading leans on the crown's outward bulge so the foliage reads as one soft mass rather
                // than a pile of independently lit quads.
                Vector3 shading = (cardNormal * 0.45f + outward * 0.35f + Vector3.up * 0.62f).normalized;
                var tangent = new Vector4(longAxis.x, longAxis.y, longAxis.z, -1f);
                for (int corner = 0; corner < 4; corner++)
                {
                    outNormals[vertexBase + corner] = shading;
                    outTangents[vertexBase + corner] = tangent;
                }

                int triangleBase = card * 6;
                outTriangles[triangleBase + 0] = vertexBase + 0;
                outTriangles[triangleBase + 1] = vertexBase + 2;
                outTriangles[triangleBase + 2] = vertexBase + 1;
                outTriangles[triangleBase + 3] = vertexBase + 0;
                outTriangles[triangleBase + 4] = vertexBase + 3;
                outTriangles[triangleBase + 5] = vertexBase + 2;
            }

            Debug.Log("[JacarandaLod] leaves cards=" + cardCount + " bakedTriangles=" + cardCount * 2 +
                      " frondLength=" + baseLength.ToString("F4") +
                      " meanBranchGap=" + (reachTotal / cardCount).ToString("F4") +
                      " maxBranchGap=" + reachMax.ToString("F4"));

            return new Part
            {
                Vertices = outVertices,
                Normals = outNormals,
                Uvs = outUvs,
                Tangents = outTangents,
                Triangles = outTriangles
            };
        }

        /// <summary>
        /// Voxel-bins the centroids of the discarded leaf triangles and returns one jittered point per
        /// occupied cell, so the card crown inherits the scan's foliage distribution rather than an
        /// invented one. Cell size is bisected until the occupied count lands just under the card target.
        /// </summary>
        private static Vector3[] SelectLeafTargets(Vector3[] vertices, int[] leafTriangles, int targetCards)
        {
            int triangleCount = leafTriangles.Length / 3;
            if (triangleCount == 0)
                throw new InvalidOperationException("Source mesh has no leaf geometry to place foliage from.");

            var centroids = new Vector3[triangleCount];
            for (int triangle = 0; triangle < triangleCount; triangle++)
            {
                int index = triangle * 3;
                centroids[triangle] = (vertices[leafTriangles[index]] +
                                       vertices[leafTriangles[index + 1]] +
                                       vertices[leafTriangles[index + 2]]) / 3f;
            }

            var bounds = new Bounds(centroids[0], Vector3.zero);
            for (int index = 1; index < centroids.Length; index++) bounds.Encapsulate(centroids[index]);
            float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

            List<Vector3> best = null;
            int low = 2;
            int high = 256;
            while (low <= high)
            {
                int resolution = (low + high) / 2;
                List<Vector3> candidate = BinPoints(centroids, bounds.min, extent / resolution);
                if (candidate.Count > targetCards)
                {
                    high = resolution - 1;
                }
                else
                {
                    best = candidate;
                    low = resolution + 1;
                }
            }

            if (best == null) throw new InvalidOperationException("Unable to reach the leaf card target.");
            return best.ToArray();
        }

        private static List<Vector3> BinPoints(Vector3[] points, Vector3 origin, float cellSize)
        {
            cellSize = Mathf.Max(cellSize, 1e-6f);
            var cells = new Dictionary<long, int>(points.Length);
            var sums = new List<Vector3>();
            var counts = new List<int>();
            for (int index = 0; index < points.Length; index++)
            {
                long key = CellKey(points[index], origin, cellSize);
                int slot;
                if (!cells.TryGetValue(key, out slot))
                {
                    slot = sums.Count;
                    cells.Add(key, slot);
                    sums.Add(Vector3.zero);
                    counts.Add(0);
                }
                sums[slot] += points[index];
                counts[slot] += 1;
            }

            var binned = new List<Vector3>(sums.Count);
            for (int slot = 0; slot < sums.Count; slot++)
            {
                Vector3 mean = sums[slot] / counts[slot];
                uint seed = Hash((uint)slot * 2246822519u);
                // Jitter inside the cell so the crown does not betray the binning grid.
                mean += new Vector3(NextSigned(ref seed), NextSigned(ref seed), NextSigned(ref seed)) *
                        (cellSize * 0.34f);
                binned.Add(mean);
            }
            return binned;
        }

        private readonly struct BranchSample
        {
            public readonly Vector3 Position;
            public readonly Vector3 SurfaceNormal;

            public BranchSample(Vector3 position, Vector3 surfaceNormal)
            {
                Position = position;
                SurfaceNormal = surfaceNormal;
            }
        }

        /// <summary>Points on the shipped branch surface, weighted by triangle area.</summary>
        private static BranchSample[] SampleBranchSurface(Part branches, int count)
        {
            int triangleCount = branches.Triangles.Length / 3;
            var cumulative = new float[triangleCount];
            float total = 0f;
            for (int triangle = 0; triangle < triangleCount; triangle++)
            {
                Vector3 a = branches.Vertices[branches.Triangles[triangle * 3]];
                Vector3 b = branches.Vertices[branches.Triangles[triangle * 3 + 1]];
                Vector3 c = branches.Vertices[branches.Triangles[triangle * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                cumulative[triangle] = total;
            }
            if (total <= 0f) throw new InvalidOperationException("Branch geometry has no surface area.");

            var samples = new BranchSample[count];
            uint seed = Hash(0x5F356495u);
            for (int index = 0; index < count; index++)
            {
                float pick = NextUnit(ref seed) * total;
                int low = 0;
                int high = triangleCount - 1;
                while (low < high)
                {
                    int middle = (low + high) / 2;
                    if (cumulative[middle] < pick) low = middle + 1;
                    else high = middle;
                }

                Vector3 a = branches.Vertices[branches.Triangles[low * 3]];
                Vector3 b = branches.Vertices[branches.Triangles[low * 3 + 1]];
                Vector3 c = branches.Vertices[branches.Triangles[low * 3 + 2]];
                float u = NextUnit(ref seed);
                float v = NextUnit(ref seed);
                if (u + v > 1f)
                {
                    u = 1f - u;
                    v = 1f - v;
                }

                Vector3 normal = Vector3.Cross(b - a, c - a);
                samples[index] = new BranchSample(
                    a + (b - a) * u + (c - a) * v,
                    normal.sqrMagnitude > 1e-12f ? normal.normalized : Vector3.up);
            }
            return samples;
        }

        private sealed class PointGrid
        {
            public Dictionary<long, List<int>> Cells;
            public BranchSample[] Points;
            public Vector3 Origin;
            public float CellSize;
            public int MaxRing;
        }

        private static PointGrid BuildPointGrid(BranchSample[] points, float cellSize)
        {
            var bounds = new Bounds(points[0].Position, Vector3.zero);
            for (int index = 1; index < points.Length; index++) bounds.Encapsulate(points[index].Position);

            var grid = new PointGrid
            {
                Cells = new Dictionary<long, List<int>>(points.Length),
                Points = points,
                Origin = bounds.min,
                CellSize = cellSize,
                MaxRing = Mathf.CeilToInt(
                    Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) / cellSize) + 2
            };

            for (int index = 0; index < points.Length; index++)
            {
                long key = CellKey(points[index].Position, grid.Origin, cellSize);
                List<int> bucket;
                if (!grid.Cells.TryGetValue(key, out bucket))
                {
                    bucket = new List<int>();
                    grid.Cells.Add(key, bucket);
                }
                bucket.Add(index);
            }
            return grid;
        }

        /// <summary>
        /// Nearest stored point, by scanning cell shells outward. The scan only stops once the best distance
        /// found fits inside the shells already visited, so the answer is exact rather than merely close.
        /// </summary>
        private static int NearestPoint(PointGrid grid, Vector3 query)
        {
            long baseX = (long)Mathf.Floor((query.x - grid.Origin.x) / grid.CellSize);
            long baseY = (long)Mathf.Floor((query.y - grid.Origin.y) / grid.CellSize);
            long baseZ = (long)Mathf.Floor((query.z - grid.Origin.z) / grid.CellSize);

            int best = -1;
            float bestSqr = float.MaxValue;
            for (int ring = 0; ring <= grid.MaxRing; ring++)
            {
                for (long offsetX = -ring; offsetX <= ring; offsetX++)
                {
                    for (long offsetY = -ring; offsetY <= ring; offsetY++)
                    {
                        for (long offsetZ = -ring; offsetZ <= ring; offsetZ++)
                        {
                            long chebyshev = Math.Max(Math.Abs(offsetX), Math.Max(Math.Abs(offsetY), Math.Abs(offsetZ)));
                            if (chebyshev != ring) continue;

                            long key = ((baseX + offsetX) * 73856093L) ^
                                       ((baseY + offsetY) * 19349663L) ^
                                       ((baseZ + offsetZ) * 83492791L);
                            List<int> bucket;
                            if (!grid.Cells.TryGetValue(key, out bucket)) continue;
                            for (int index = 0; index < bucket.Count; index++)
                            {
                                float distance = (grid.Points[bucket[index]].Position - query).sqrMagnitude;
                                if (distance >= bestSqr) continue;
                                bestSqr = distance;
                                best = bucket[index];
                            }
                        }
                    }
                }

                if (best >= 0 && Mathf.Sqrt(bestSqr) <= ring * grid.CellSize) break;
            }

            if (best < 0) throw new InvalidOperationException("No branch surface point found for a leaf target.");
            return best;
        }

        private static long CellKey(Vector3 point, Vector3 origin, float cellSize)
        {
            long cellX = (long)Mathf.Floor((point.x - origin.x) / cellSize);
            long cellY = (long)Mathf.Floor((point.y - origin.y) / cellSize);
            long cellZ = (long)Mathf.Floor((point.z - origin.z) / cellSize);
            return (cellX * 73856093L) ^ (cellY * 19349663L) ^ (cellZ * 83492791L);
        }



        private static Mesh Combine(Part branches, Part trunk, Part leaves)
        {
            Part[] parts = { branches, trunk, leaves };
            int vertexTotal = 0;
            for (int index = 0; index < parts.Length; index++) vertexTotal += parts[index].Vertices.Length;

            var vertices = new Vector3[vertexTotal];
            var normals = new Vector3[vertexTotal];
            var uvs = new Vector2[vertexTotal];
            var tangents = new Vector4[vertexTotal];
            var submeshTriangles = new int[parts.Length][];

            int vertexOffset = 0;
            for (int index = 0; index < parts.Length; index++)
            {
                Part part = parts[index];
                Array.Copy(part.Vertices, 0, vertices, vertexOffset, part.Vertices.Length);
                Array.Copy(part.Normals, 0, normals, vertexOffset, part.Normals.Length);
                Array.Copy(part.Uvs, 0, uvs, vertexOffset, part.Uvs.Length);
                if (part.Tangents != null)
                    Array.Copy(part.Tangents, 0, tangents, vertexOffset, part.Tangents.Length);

                var shifted = new int[part.Triangles.Length];
                for (int corner = 0; corner < shifted.Length; corner++)
                    shifted[corner] = part.Triangles[corner] + vertexOffset;
                submeshTriangles[index] = shifted;
                vertexOffset += part.Vertices.Length;
            }

            var mesh = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(OutputMeshPath),
                indexFormat = vertexTotal > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                subMeshCount = parts.Length
            };
            for (int index = 0; index < parts.Length; index++)
                mesh.SetTriangles(submeshTriangles[index], index, true);

            // Trunk and branches keep their clustered UVs, so their tangents are derived; the leaf cards
            // already carry an exact tangent along the frond's long axis.
            mesh.RecalculateTangents();
            int leafVertexStart = branches.Vertices.Length + trunk.Vertices.Length;
            Vector4[] recalculated = mesh.tangents;
            for (int index = leafVertexStart; index < vertexTotal; index++)
                recalculated[index] = tangents[index];
            mesh.tangents = recalculated;

            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }

        private static Bounds ComputeBounds(Vector3[] vertices, int[] triangles)
        {
            if (triangles.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var bounds = new Bounds(vertices[triangles[0]], Vector3.zero);
            for (int index = 1; index < triangles.Length; index++)
                bounds.Encapsulate(vertices[triangles[index]]);
            return bounds;
        }

        private static uint Hash(uint value)
        {
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            value *= 3266489917u;
            value ^= value >> 16;
            return value;
        }

        private static float NextUnit(ref uint seed)
        {
            seed = Hash(seed + 0x9E3779B9u);
            return (seed & 0xFFFFFFu) / (float)0x1000000;
        }

        private static float NextSigned(ref uint seed)
        {
            return NextUnit(ref seed) * 2f - 1f;
        }

        private static Mesh LoadRequiredMesh(string path)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) throw new InvalidOperationException("Missing required mesh at " + path);
            return mesh;
        }

        private static Mesh SaveMesh(string path, Mesh generated)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                AssetDatabase.SaveAssets();
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static string ComputeFileHash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new System.Text.StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++) builder.Append(hash[index].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
