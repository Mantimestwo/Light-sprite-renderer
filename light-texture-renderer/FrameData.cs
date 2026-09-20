using Godot;
using System;

[GlobalClass]
public partial class FrameData : Resource
{
    public float dither = 0.2f;
    public int valuesCount = 4;
    public float alpha = 1.0f;
    public float centerSize = 0.5f;
    public float feathering = 1f;
    public float power = 2f;
    public float openAngle = 360f;
    public float rotation = 0f;
}
