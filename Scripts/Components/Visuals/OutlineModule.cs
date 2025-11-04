using Godot;

namespace Components;

[GlobalClass]
public partial class OutlineModule : Node
{
    [Export] public Sprite2D TargetNode { get; set; }
    [Export]
    public Color OutlineColor
    {
        get => outlineColor;
        set
        {
            outlineColor = value;
            ApplySettings();
        }
    }

    [Export(PropertyHint.Range, "0.0,10.0,0.1")] public float OutlineThickness { get; set; } = 1.0f;
    public bool Enabled { get; set; } = true;

    ShaderMaterial material;
    Color outlineColor = Colors.White;

    private const string ShaderParamOutlineColor = "outline_color";
    private const string ShaderParamOutlineThickness = "outline_thickness";
    private const string ShaderParamEnabled = "enabled";

    public override void _Ready()
    {
        if (TargetNode == null)
        {
            GD.PushWarning($"{nameof(OutlineModule)}: TargetNode not assigned.");
            return;
        }

        material = GetOutlineShader(TargetNode);
        ApplySettings();
    }

    public override void _Process(double delta)
    {
        if (material == null) return;

        material.SetShaderParameter(ShaderParamEnabled, Enabled);
    }

    void ApplySettings()
    {
        if (material == null) return;

        material.SetShaderParameter(ShaderParamOutlineColor, OutlineColor);
        material.SetShaderParameter(ShaderParamOutlineThickness, OutlineThickness);
        material.SetShaderParameter(ShaderParamEnabled, Enabled);
    }

    static ShaderMaterial GetOutlineShader(Sprite2D sprite)
    {
        if (sprite?.Material is ShaderMaterial shaderMat && shaderMat.Shader != null)
            return shaderMat;

        string shaderCode = @"
        shader_type canvas_item;

        uniform bool enabled = true;
        uniform vec4 outline_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
        uniform float outline_thickness : hint_range(0.0, 10.0, 0.1) = 1.0;

        void fragment() {
            vec4 tex = texture(TEXTURE, UV);
            float alpha = tex.a;

            if (enabled) {
                for (float x = -1.0; x <= 1.0; x++) {
                    for (float y = -1.0; y <= 1.0; y++) {
                        vec2 offset = vec2(x, y) * outline_thickness * TEXTURE_PIXEL_SIZE;
                        vec4 sample_tex = texture(TEXTURE, UV + offset);
                        alpha = max(alpha, sample_tex.a);
                    }
                }

                if (tex.a < 0.1 && alpha > 0.1)
                    COLOR = outline_color;
                else
                    COLOR = tex;
            } else {
                COLOR = tex;
            }
        }
        ";

        Shader shader = new() { Code = shaderCode };
        ShaderMaterial material = new() { Shader = shader };
        if (sprite != null)
            sprite.Material = material;

        return material;
    }
}
