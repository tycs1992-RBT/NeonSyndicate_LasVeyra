using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NeonSyndicate;
using NeonSyndicate.Tests;
using NUnit.Framework;
using UnityEngine;

public static class SourceProgram
{
    private static float[] Point(Vector3 value) { return new[] { value.x, value.y, value.z }; }
    private static object WalkRoute(CityLayout layout, Vector3 start, Vector3 end)
    {
        int first = layout.NearestWalkNode(start, 120f), last = layout.NearestWalkNode(end, 120f);
        if (first < 0 || last < 0) return new { status = "NO NEARBY WALK GRAPH NODE" };
        float[] distances = Enumerable.Repeat(float.PositiveInfinity, layout.WalkNodes.Count).ToArray();
        int[] previous = Enumerable.Repeat(-1, layout.WalkNodes.Count).ToArray();
        PriorityQueue<int, float> queue = new PriorityQueue<int, float>(); distances[first] = 0f; queue.Enqueue(first, 0f);
        while (queue.Count > 0)
        {
            int node; float cost; queue.TryDequeue(out node, out cost);
            if (cost != distances[node]) continue;
            if (node == last) break;
            foreach (int index in layout.WalkNodes[node].Links)
            {
                WalkLink link = layout.WalkLinks[index]; int next = link.A == node ? link.B : link.A;
                float candidate = cost + MathUtil.FlatDistance(layout.WalkNodes[node].Pos, layout.WalkNodes[next].Pos);
                if (candidate >= distances[next]) continue;
                distances[next] = candidate; previous[next] = node; queue.Enqueue(next, candidate);
            }
        }
        if (float.IsInfinity(distances[last])) return new { status = "WALK GRAPH DISCONNECTED" };
        List<int> nodes = new List<int>(); int current = last;
        while (current >= 0) { nodes.Add(current); if (current == first) break; current = previous[current]; }
        nodes.Reverse();
        float startConnector = MathUtil.FlatDistance(start, layout.WalkNodes[first].Pos), endConnector = MathUtil.FlatDistance(end, layout.WalkNodes[last].Pos);
        return new { status = "SOURCE WALK GRAPH ONLY; END CONNECTORS AND COLLISION NOT VERIFIED", graphMetres = distances[last],
            startConnectorMetres = startConnector, endConnectorMetres = endConnector, totalMetres = distances[last] + startConnector + endConnector,
            nodes = nodes.Select(n => new { id = n, position = Point(layout.WalkNodes[n].Pos) }).ToArray() };
    }
    public static int Main()
    {
        List<object> tests = new List<object>(); int failures = 0;
        Type[] types = { typeof(InhabitedCityBook04CTests), typeof(InhabitedCityBindings04CTests), typeof(ServiceUseGate04CTests), typeof(UnityMetadata04CTests) };
        foreach (Type type in types) foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            List<object[]> cases = new List<object[]>();
            foreach (TestCaseAttribute attribute in method.GetCustomAttributes(typeof(TestCaseAttribute), true)) cases.Add(attribute.Arguments);
            if (method.GetCustomAttributes(typeof(TestAttribute), true).Length > 0) cases.Add(new object[0]);
            foreach (object[] arguments in cases)
            {
                string error = null;
                try { method.Invoke(Activator.CreateInstance(type), arguments); }
                catch (TargetInvocationException exception) { error = exception.InnerException.ToString(); failures++; }
                tests.Add(new { name = type.Name + "." + method.Name, arguments, passed = error == null, error });
            }
        }
        List<object> seeds = new List<object>();
        foreach (int seed in new[] { 61724, 1, 2016, 77231, 92017 })
        {
            CityLayout layout = new CityLayout(seed); layout.Generate();
            List<VenueBinding04C> bindings = InhabitedCityBindings04C.Select(layout, InhabitedCityBindings04CTests.ReadCatalog());
            Poi home = layout.FindPoi("Veyra Lofts");
            // Exact planar portion of GameRoot.SpawnOutside; its native FindGround call is deliberately not simulated.
            Vector3 start = home.Position + (home.Facing.sqrMagnitude > .01f ? home.Facing.normalized : Vector3.forward) * 3.5f;
            List<object> venues = new List<object>();
            foreach (VenueBinding04C binding in bindings)
            {
                bool selected = binding.Building != null;
                venues.Add(new { id = binding.Definition.Id, binding.Definition.Title, binding.Definition.PoiName,
                    sourceSelected = selected, nativeAvailable = "NOT VERIFIED", status = binding.Status,
                    buildingSeed = selected ? binding.Building.Seed : (int?)null,
                    footprint = selected ? new[] { binding.Building.Footprint.X0, binding.Building.Footprint.Z0, binding.Building.Footprint.X1, binding.Building.Footprint.Z1 } : null,
                    origin = Point(binding.Origin), exterior = Point(binding.Outside), outward = Point(binding.Outward),
                    width = binding.Width, depth = binding.Depth, yaw = binding.Yaw,
                    directFromArrivalMetres = selected ? MathUtil.FlatDistance(start, binding.Outside) : (float?)null,
                    arrivalRoute = selected && seed == 61724 ? WalkRoute(layout, start, binding.Outside) : null });
            }
            List<object> legs = new List<object>();
            if (seed == 61724)
            {
                string[] circuit = { "lofts", "clinic", "forge", "inn", "archive", "observatory", "garage", "depot", "lofts" };
                for (int i = 1; i < circuit.Length; i++)
                {
                    VenueBinding04C from = bindings.Find(v => v.Definition.Id == circuit[i - 1]), to = bindings.Find(v => v.Definition.Id == circuit[i]);
                    if (from.Building != null && to.Building != null) legs.Add(new { from = circuit[i - 1], to = circuit[i], route = WalkRoute(layout, from.Outside, to.Outside) });
                }
            }
            seeds.Add(new { seed, sourceSelected = bindings.Count(v => v.Building != null), arrivalBeforeNativeGround = Point(start), venues, circuitLegs = legs });
        }
        Console.Write(JsonSerializer.Serialize(new { scope = "Actual unchanged production book, selection, city generator, service gate and NUnit assertions. Managed math adapter disclosed separately; the native view host throws and is never instantiated. Walk routes are shortest paths over the actual generated walk graph plus unverified endpoint connectors, not measured player traversal.",
            tests, failures, seeds, nativeUnity = "NOT RUN", nativeCollision = "NOT RUN", nativeSaveJsonUtility = "NOT RUN" }, new JsonSerializerOptions { WriteIndented = true }));
        return failures == 0 ? 0 : 1;
    }
}
