using Godot;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace Components;

[GlobalClass]
public partial class ScreenFlashModule : ColorRect
{
    [Export(PropertyHint.Range, secondsHint)] public float DefaultFadeIn { get; set; } = 0.1f;
    [Export(PropertyHint.Range, secondsHint)] public float DefaultDuration { get; set; } = 0.1f;
    [Export(PropertyHint.Range, secondsHint)] public float DefaultFadeOut { get; set; } = 0.1f;
    [Export(PropertyHint.Range, positiveHint)] public float DefaultAlpha { get; set; } = 0.5f;
    public Color DefaultColor { get; set; }

    const string secondsHint = "0,0, or_greater, hide_slider, suffix:s", positiveHint = "0,0, or_greater, hide_slider";
    Tween tween;

    public override void _Ready()
    {
        Visible = false;
        DefaultColor = new(Color, 1f);
        Modulate = new(Color, 0f);
    }

    public async Task PlayFlash(float? fadeIn = null, float? duration = null, float? fadeOut = null, float? alpha = null, Color? color = null)
    {
        tween?.Kill();
        
        Visible = true;
        float fadeInVal = fadeIn ?? DefaultFadeIn;
        float durationVal = duration ?? DefaultDuration;
        float fadeOutVal = fadeOut ?? DefaultFadeOut;
        float alphaVal = alpha ?? DefaultAlpha;
        Color flashColor = color ?? DefaultColor;

        void setter(float value) => Modulate = new(flashColor, value);
        
        tween = CreateTween();
        tween.TweenDelegate(setter, 0f, alphaVal, fadeInVal);
        tween.TweenInterval(durationVal);
        tween.TweenDelegate(setter, alphaVal, 0f, fadeOutVal);

        await ToSignal(tween, Tween.SignalName.Finished);
        Visible = false;
    }

    public void SetDefaultParameters(float? fadeIn = null, float? duration = null, float? fadeOut = null, float? alpha = null, Color? color = null)
    {
        if (fadeIn.HasValue) DefaultFadeIn = fadeIn.Value;
        if (duration.HasValue) DefaultDuration = duration.Value;
        if (fadeOut.HasValue) DefaultFadeOut = fadeOut.Value;
        if (alpha.HasValue) DefaultAlpha = alpha.Value;
        if (color.HasValue) DefaultColor = color.Value;
    }
}
