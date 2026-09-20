using Godot;
using System;

[Tool]
public partial class ValueSlider : HSlider
{
	[Export] string SliderName
	{
		get { return sliderName; }
		set
		{
			sliderName = value;
			if(nameLabel == null) return;
			nameLabel.Text = sliderName;
		}
	}
	string sliderName;
	[Export] Label valuePrint;
	[Export] LineEdit valueField;
	[Export] Label nameLabel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		nameLabel.Text = sliderName;
		valueField.Text = Value.ToString();
		if (Engine.IsEditorHint())
		{
			
		}
		else
		{
			ValueChanged += (v) => SetText(v);
		}
	}

	void SetText(double v)
	{
		valueField.Text = v.ToString();
	}

    public override void _PhysicsProcess(double delta)
    {
		if (Engine.IsEditorHint())
		{
			if(valueField == null) return;
			if (valueField.Text != Value.ToString())
				valueField.Text = Value.ToString();
		}
    }

}
