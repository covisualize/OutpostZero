using System.IO;
using NUnit.Framework;

namespace OutpostZero.Tests.EditMode
{
    public class VertexAoTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(string rel)
        {
            return File.ReadAllText(Path.Combine(Root, rel)).Replace("\r\n", "\n");
        }

        [Test]
        public void TheBakedShaderReadsTheVertexAoLayerBehindAWeight()
        {
            string shader = Read("Assets/Shaders/OutpostTriplanarRim.shader");
            StringAssert.Contains("_VertexAO (\"Vertex Colour AO\", Range(0, 1)) = 0", shader);
            int start = shader.IndexOf("CBUFFER_START(UnityPerMaterial)");
            int end = shader.IndexOf("CBUFFER_END", start);
            StringAssert.Contains("half _VertexAO;", shader.Substring(start, end - start));
            StringAssert.Contains("half4 color : COLOR;", shader);
            StringAssert.Contains("output.vertexAO = input.color.r;", shader);
            StringAssert.Contains("lerp(1.0, input.vertexAO, _VertexAO)", shader);
        }

        [Test]
        public void EveryBakedMaterialAndTheImporterTurnItOn()
        {
            var mats = Directory.GetFiles(Path.Combine(Root, "Assets/Materials/Baked"), "*.mat");
            Assert.GreaterOrEqual(mats.Length, 114);
            foreach (var mat in mats)
                StringAssert.Contains("- _VertexAO: 1", File.ReadAllText(mat), Path.GetFileName(mat));
            StringAssert.Contains("material.SetFloat(\"_VertexAO\", 1f);", Read("Assets/Scripts/Editor/FbxPrefabPostprocessor.cs"));
        }

        [Test]
        public void TheExportBakesAndChecksTheLayer()
        {
            StringAssert.Contains("vertex_ao.bake(obj)", Read("BlenderScripts/blender_utils.py"));
            StringAssert.Contains("colors_type='SRGB'", Read("BlenderScripts/blender_utils.py"));
            StringAssert.Contains("fbx_uv.colour_problems(output", Read("BlenderScripts/pipeline.py"));
            StringAssert.Contains("fbx_uv.colour_problems(path, relative)", Read("BlenderScripts/asset_audit.py"));
            StringAssert.Contains("\"vertex_ao.py\"", Read("BlenderScripts/pipeline_plan.py"));
        }
    }
}
