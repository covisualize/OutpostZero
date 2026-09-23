using System;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class VolumeParameter<T> { public T value; public bool overrideState; public virtual void Override(T x) { value = x; } }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float v, bool o = false) { } }
    public class MinFloatParameter : FloatParameter { public MinFloatParameter(float v, float min, bool o = false) : base(v) { } }
    public class ClampedFloatParameter : FloatParameter { public ClampedFloatParameter(float v, float a, float b, bool o = false) : base(v) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color v, bool a = false, bool b = true, bool c = true, bool o = false) { } }
    public class VolumeComponent : ScriptableObject { public bool active = true; }
    public class VolumeProfile : ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent { return null; } }
    public class Volume : MonoBehaviour { public bool isGlobal { get; set; } public float priority; public VolumeProfile sharedProfile; public VolumeProfile profile { get; set; } public float weight = 1f; }
}

namespace UnityEngine.Rendering.Universal
{
    public enum TonemappingMode { None, Neutral, ACES }
    public enum FilmGrainLookup { Thin1, Thin2, Medium1, Medium2, Medium3, Medium4, Medium5, Medium6, Large01, Large02, Custom }
    public enum DepthOfFieldMode { Off, Gaussian, Bokeh }
    public enum MotionBlurMode { CameraOnly, CameraAndObjects }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode v, bool o = false) { } }
    public class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup> { public FilmGrainLookupParameter(FilmGrainLookup v, bool o = false) { } }
    public class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode v, bool o = false) { } }
    public class MotionBlurModeParameter : VolumeParameter<MotionBlurMode> { public MotionBlurModeParameter(MotionBlurMode v, bool o = false) { } }
    public sealed class Bloom : VolumeComponent { public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f); public MinFloatParameter intensity = new MinFloatParameter(0f, 0f); }
    public sealed class Vignette : VolumeComponent { public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f); public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f); }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None); }
    public sealed class ColorAdjustments : VolumeComponent { public FloatParameter postExposure = new FloatParameter(0f); public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f); public ColorParameter colorFilter = new ColorParameter(Color.white); public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f); }
    public sealed class FilmGrain : VolumeComponent { public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1); public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f); }
    public sealed class MotionBlur : VolumeComponent { public MotionBlurModeParameter mode = new MotionBlurModeParameter(MotionBlurMode.CameraOnly); public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f); }
    public sealed class DepthOfField : VolumeComponent { public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off); public MinFloatParameter gaussianStart = new MinFloatParameter(10f, 0f); public MinFloatParameter gaussianEnd = new MinFloatParameter(30f, 0f); }
    public class UniversalAdditionalLightData : MonoBehaviour { }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing { get; set; } public bool renderShadows { get; set; } }
    public abstract class ScriptableRendererData : ScriptableObject { }
    public class UniversalRendererData : ScriptableRendererData { }
    public abstract class ScriptableRendererFeature : ScriptableObject { }
    public class DecalRendererFeature : ScriptableRendererFeature { }
    public enum DecalScaleMode { ScaleInvariant, InheritFromHierarchy }
    public class DecalProjector : MonoBehaviour { public Material material { get; set; } public float drawDistance { get; set; } public float fadeScale { get; set; } public float startAngleFade { get; set; } public float endAngleFade { get; set; } public Vector2 uvScale { get; set; } public Vector2 uvBias { get; set; } public uint renderingLayerMask { get; set; } public DecalScaleMode scaleMode { get; set; } public Vector3 pivot { get; set; } public Vector3 size { get; set; } public float fadeFactor { get; set; } public bool IsValid() { return false; } }
    public class UniversalRenderPipelineAsset : RenderPipelineAsset { public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null) { return null; } protected override RenderPipeline CreatePipeline() { return null; } public int msaaSampleCount { get; set; } public float renderScale { get; set; } }
}

namespace Unity.Cinemachine
{
    public struct LensSettings { public float FieldOfView; public float OrthographicSize; public static LensSettings Default => new LensSettings { FieldOfView = 40f }; }
    public struct PrioritySettings { public bool Enabled; public int Value { get; set; } public static implicit operator int(PrioritySettings p) => p.Value; public static implicit operator PrioritySettings(int v) => new PrioritySettings { Value = v, Enabled = true }; }
    public static class CinemachineCore { public enum Stage { Body, Aim, Noise, Finalize } }
    [Serializable] public struct CinemachineBlendDefinition { public enum Styles { Cut, EaseInOut, EaseIn, EaseOut, HardIn, HardOut, Linear, Custom } public Styles Style; public float Time; public CinemachineBlendDefinition(Styles style, float time) { Style = style; Time = time; } }
    public class CinemachineBrain : MonoBehaviour { public enum UpdateMethods { FixedUpdate, LateUpdate, SmartUpdate, ManualUpdate } public UpdateMethods UpdateMethod = UpdateMethods.SmartUpdate; public bool IgnoreTimeScale; public CinemachineBlendDefinition DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 2f); }
    public abstract class CinemachineVirtualCameraBase : MonoBehaviour { public PrioritySettings Priority = new PrioritySettings(); public abstract Transform LookAt { get; set; } public abstract Transform Follow { get; set; } }
    public class CinemachineCamera : CinemachineVirtualCameraBase { public LensSettings Lens = LensSettings.Default; public override Transform LookAt { get; set; } public override Transform Follow { get; set; } }
    public abstract class CinemachineCameraManagerBase : CinemachineVirtualCameraBase { public override Transform LookAt { get; set; } public override Transform Follow { get; set; } }
    public class CinemachineSequencerCamera : CinemachineCameraManagerBase { [Serializable] public struct Instruction { public CinemachineVirtualCameraBase Camera; public CinemachineBlendDefinition Blend; public float Hold; } public bool Loop; public System.Collections.Generic.List<Instruction> Instructions = new System.Collections.Generic.List<Instruction>(); }
    public abstract class CinemachineComponentBase : MonoBehaviour { }
    public abstract class CinemachineExtension : MonoBehaviour { }
    public class CinemachineFollow : CinemachineComponentBase { public Vector3 FollowOffset = Vector3.back * 10f; }
    public class CinemachineHardLookAt : CinemachineComponentBase { }
    public class CinemachinePositionComposer : CinemachineComponentBase { public float CameraDistance = 10f; public float DeadZoneDepth; public bool CenterOnActivate = true; public Vector3 TargetOffset; public Vector3 Damping; }
    public class CinemachineConfiner3D : CinemachineExtension { public Collider BoundingVolume; public float SlowingDistance; }
    public class CinemachineImpulseListener : CinemachineExtension { [Serializable] public struct ImpulseReaction { public float AmplitudeGain; public float FrequencyGain; public float Duration; } public CinemachineCore.Stage ApplyAfter = CinemachineCore.Stage.Aim; public int ChannelMask; public float Gain; public bool Use2DDistance; public bool UseCameraSpace; public ImpulseReaction ReactionSettings; }
    [Serializable] public class CinemachineImpulseDefinition { public enum ImpulseShapes { Custom, Recoil, Bump, Explosion, Rumble } public enum ImpulseTypes { Uniform, Dissipating, Propagating, Legacy } public int ImpulseChannel = 1; public ImpulseShapes ImpulseShape; public float ImpulseDuration = 0.2f; public ImpulseTypes ImpulseType = ImpulseTypes.Legacy; public float DissipationRate; public float AmplitudeGain = 1f; public float FrequencyGain = 1f; public bool Randomize = true; public float ImpactRadius = 100f; public float DissipationDistance = 100f; public float PropagationSpeed = 343f; }
    public class CinemachineImpulseSource : MonoBehaviour { public CinemachineImpulseDefinition ImpulseDefinition = new CinemachineImpulseDefinition(); public Vector3 DefaultVelocity = Vector3.down; public void GenerateImpulseAt(Vector3 position, Vector3 velocity) { } public void GenerateImpulseWithForce(float force) { } }
}
