using Godot;
using System;

public partial class ConfirmationScreen : ColorRect
{
	[Export] Button confirmButton;
	[Export] Button cancelButton;

	[Signal] public delegate void AcceptSignalEventHandler();
	[Signal] public delegate void CancelSignalEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		confirmButton.Pressed += Accept;
		cancelButton.Pressed += Cancel;
	}

	void Accept()
	{
		EmitSignal(SignalName.AcceptSignal);
	}

	void Cancel()
	{
		EmitSignal(SignalName.CancelSignal);
	}
}
