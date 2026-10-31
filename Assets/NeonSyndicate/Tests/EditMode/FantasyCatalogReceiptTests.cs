#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class FantasyCatalogReceiptTests
    {
        private static Type EditorType(string name)
        {
            Type value = Type.GetType("NeonSyndicate.Editor." + name + ", NeonSyndicate.Editor");
            Assert.That(value, Is.Not.Null); return value;
        }

        [Test]
        public void CachedCatalogMustContainEveryReceiptModuleAndItsLods()
        {
            Type setup = EditorType("HeartSetupFR01");
            Type row = setup.GetNestedType("Row", BindingFlags.NonPublic);
            MethodInfo check = setup.GetMethod("PreparedReceiptCompleteFR01", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(row, Is.Not.Null); Assert.That(check, Is.Not.Null);
            Array receipt = Array.CreateInstance(row, 2);
            for (int i = 0; i < 2; i++)
            {
                object entry = Activator.CreateInstance(row, true);
                row.GetField("name").SetValue(entry, i == 0 ? "HeartGate" : "HeartStreet");
                row.GetField("lod").SetValue(entry, i);
                receipt.SetValue(entry, i);
            }
            HeartCatalogFR01 catalog = UnityEngine.ScriptableObject.CreateInstance<HeartCatalogFR01>();
            UnityEngine.GameObject model = new UnityEngine.GameObject("receipt test");
            try
            {
                catalog.Modules = new[] { new HeartModuleFR01 { Id = "HeartGate", Lods = new[] { model, model, model } } };
                Assert.That(check.Invoke(null, new object[] { catalog, receipt }), Is.EqualTo(false));
                catalog.Modules = new[] { catalog.Modules[0], new HeartModuleFR01 { Id = "HeartStreet", Lods = new[] { model, null, model } } };
                Assert.That(check.Invoke(null, new object[] { catalog, receipt }), Is.EqualTo(false));
                catalog.Modules[1].Lods[1] = model;
                Assert.That(check.Invoke(null, new object[] { catalog, receipt }), Is.EqualTo(true));
            }
            finally { UnityEngine.Object.DestroyImmediate(model); UnityEngine.Object.DestroyImmediate(catalog); }
        }

    }
}
#endif
