using UnityEngine.UIElements;

/// <summary>
/// Builds decorative strips that USS cannot express (it has no gradients or
/// repeating backgrounds), using real elements clipped by the parent.
/// </summary>
public static class HazardTape
{
    /// <summary>Slanted yellow bars on black, like caution tape.</summary>
    public static void Fill(VisualElement strip, int segments = 80)
    {
        if (strip == null) return;
        strip.Clear();
        strip.AddToClassList("hazard-tape");
        strip.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < segments; i++)
        {
            var segment = new VisualElement();
            segment.AddToClassList("hazard-seg");
            segment.pickingMode = PickingMode.Ignore;
            strip.Add(segment);
        }
    }
}

public static class GaugeTicks
{
    /// <summary>Evenly spaced tick marks with a heavier mark every majorEvery ticks.</summary>
    public static void Fill(VisualElement container, int ticks = 21, int majorEvery = 5)
    {
        if (container == null) return;
        container.Clear();
        container.AddToClassList("gauge-ticks");
        container.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < ticks; i++)
        {
            var tick = new VisualElement();
            tick.AddToClassList("gauge-tick");
            if (i % majorEvery == 0) tick.AddToClassList("major");
            tick.pickingMode = PickingMode.Ignore;
            container.Add(tick);
        }
    }
}
