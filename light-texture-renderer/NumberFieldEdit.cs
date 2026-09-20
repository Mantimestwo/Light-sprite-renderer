using Godot;
using System;

public partial class NumberFieldEdit : LineEdit
{
	string previousValid;
	[Export] HSlider targetSlider;
	bool textCheckWait;

	public override void _Ready()
	{
		FocusEntered += () => SetCheck();
	}

	public override void _Process(double delta)
	{
		if (textCheckWait)
		{
			if (Input.IsActionJustPressed("ui_accept"))
				CheckText(Text);
			else if (Input.IsActionJustPressed("ui_cancel"))
				CancelCheck();
		}
	}

	void SetCheck()
	{
		if(!textCheckWait)
		{
			previousValid = Text;
			textCheckWait = true;
		}
	}

	void CancelCheck()
	{
		Text = previousValid;
		textCheckWait = false;
		ReleaseFocus();
	}

	void CheckText(string newText)
	{
		if (newText == string.Empty || newText == "") return;

		int caretCol = CaretColumn;
		CancelCheck();
		if (!newText.IsValidFloat())
		{
			GD.Print("invalid input");
		}
		else
		{
			Text = newText;
			float parsed = (float)Mathf.Clamp(float.Parse(Text, System.Globalization.CultureInfo.GetCultureInfo("es-ES")), targetSlider.MinValue, targetSlider.MaxValue);
			parsed = Mathf.Round(parsed / (float)targetSlider.Step) * (float)targetSlider.Step;
			GD.Print(Text + " to: " + parsed + ", min: " + targetSlider.MinValue + ", max: " + targetSlider.MaxValue);
			targetSlider.Value = parsed;
			Text = targetSlider.Value.ToString();
			textCheckWait = false;
		}

		
	}
}
