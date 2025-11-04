using Godot;
using System.Threading.Tasks;
using Utility;

namespace Components;

[GlobalClass]
public partial class ColorOverlayModule : Node
{
    [Export] public Sprite2D TargetNode { get; set; }

    Tween tween;
    ShaderMaterial material;
    bool isActive = false;

    const string ShaderParamFadeValue = "fade_value", ShaderParamOverlayColor = "fade_color";

    public override void _Ready()
    {
        material = GetColorShader(TargetNode);
    }

    /// <summary>Play a fade effect using a Fade struct and an optional color.</summary>
    public async Task PlayEffect(Fade fade, Color? color = null)
    {
        tween?.Kill();
        isActive = true;
        material.SetShaderParameter(ShaderParamOverlayColor, color ?? Colors.White);
        void setter(float value) => material?.SetShaderParameter(ShaderParamFadeValue, value);
        tween = CreateTween();

        tween.TweenDelegate(setter, 0f, fade.Intensity, fade.FadeInTime);
        tween.TweenInterval(fade.PeakTime);
        tween.TweenDelegate(setter, fade.Intensity, 0f, fade.FadeOutTime);

        await ToSignal(tween, Tween.SignalName.Finished);
        isActive = false;
    }

    static ShaderMaterial GetColorShader(Sprite2D sprite)
    {
        if (sprite?.Material is ShaderMaterial sm && sm.Shader != null)
            return sm;

        string shaderCode = @"
        shader_type canvas_item;

        uniform vec4 fade_color : source_color;
        uniform float fade_value : hint_range(0.0, 1.0, 0.1);

        void fragment() {
            vec4 texture_color = texture(TEXTURE, UV);
            COLOR.rgb = mix(texture_color.rgb, fade_color.rgb, fade_value);
            COLOR.a = texture_color.a;
        }";

        Shader shader = new() { Code = shaderCode };
        ShaderMaterial mat = new() { Shader = shader };
        if (sprite != null) sprite.Material = mat;
        return mat;
    }
    
    public struct Fade(float intensity, float fadeInTime, float peakTime, float fadeOutTime)
    {
        public float Intensity { get; set; } = intensity;
        public float FadeInTime { get; set; } = fadeInTime;
        public float PeakTime { get; set; } = peakTime;
        public float FadeOutTime { get; set; } = fadeOutTime;
    }
}

