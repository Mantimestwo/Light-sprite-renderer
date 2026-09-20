using Godot;
using System;
using System.IO;
using Dictionary = Godot.Collections.Dictionary;

public partial class MainController : Node2D
{
	string userPath;
	bool settingsHaveChanged;
	float cacheTimer;
	float cacheTime = 15f;
	[Export] Button saveButton;
	[Export] Button settingsResetButton;
	[Export] HSlider resSlider;
	[Export] HSlider ditherSlider;
	[Export] HSlider colorCountSlider;
	[Export] HSlider sizeSlider;
	[Export] HSlider featherSlider;
	[Export] HSlider alphaSlider;
	[Export] HSlider powerSlider;
	[Export] HSlider openAngleSlider;
	[Export] HSlider rotateSlider;
	[Export] RichTextLabel debugLabel;
	[Export] SubViewport subview;
	[Export] Sprite2D lightSprite;
	[Export] Sprite2D renderer;
	[Export] LineEdit savePath;
	[Export] LineEdit saveName;

	[Export] Button presetSaveButton;
	[Export] OptionButton presetSelectionButton;
	ShaderMaterial lightSpriteMat;

	[Export] ConfirmationScreen overwriteConfirmation;
	[Export] Label overwriteConfirmationLabel;
	[Export] ConfirmationScreen presetSaveConfirmation;
	[Export] LineEdit presetNameField;
	[Export] Button presetAcceptSaveButton;

	[Export] HSlider currentFrameSlider;
	[Export] HSlider frameCountSlider;

	[Export] FrameData[] frames = [new(), new(), new(), new(), new(), new(), new(), new()];
	FrameData defaultReference = new();
	
	float scaler;
	string cachedPath;

	public override void _Ready()
	{
		userPath = ProjectSettings.GlobalizePath("user://");
		lightSpriteMat = lightSprite.Material as ShaderMaterial;
		resSlider.ValueChanged += (v) => SetResolution(Mathf.RoundToInt(v));
		ditherSlider.ValueChanged += (v) => SetDither(v);
		colorCountSlider.ValueChanged += (v) => SetQuantization(Mathf.RoundToInt(v));
		sizeSlider.ValueChanged += (v) => SetSize(v);
		featherSlider.ValueChanged += (v) => SetFeathering(v);
		alphaSlider.ValueChanged += (v) => SetAlpha(v);
		powerSlider.ValueChanged += (v) => SetPower(v);
		openAngleSlider.ValueChanged += (v) => SetOpenAngle(v);
		rotateSlider.ValueChanged += (v) => SetRotation(v);
		saveButton.Pressed += CheckSave;
		overwriteConfirmation.AcceptSignal += AcceptOverwriteCheck;
		overwriteConfirmation.CancelSignal += CancelOverwriteCheck;
		settingsResetButton.Pressed += ResetToDefaults;

		presetSelectionButton.ItemSelected += (selected) => DoPresetLoad((int)selected);
		presetSaveButton.Pressed += OpenPresetSave;
		presetSaveConfirmation.AcceptSignal += DoPresetSave;
		presetSaveConfirmation.CancelSignal += ClosePresetSave;
		presetNameField.TextChanged += CheckIfPresetNameValid;

		currentFrameSlider.Visible = false;
		currentFrameSlider.ValueChanged += (v) => CheckCurrentFrame();
		frameCountSlider.ValueChanged += (v) =>  CheckFrames();

		savePath.TextChanged += (v) => SetPath(v);
		saveName.TextChanged += (v) => SetTextureName(v);

		LoadCache();
		FetchPresetListNames();

		// this is here to just force the viewport to match size, as it starts off as 512x512
		// if the on startup loaded resolution value is the same as default, it doesnt trigger the value changed call, and may thus remain 512x even if the light is 32x
		SetResolution(Mathf.RoundToInt(resSlider.Value));
	}
	
	public override void _Process(double delta)
	{
		cacheTimer -= (float)delta;
		if (cacheTimer <= 0f)
		{
			cacheTimer = cacheTime;
			if (settingsHaveChanged)
			{
				settingsHaveChanged = false;
				GD.Print("Cached settings");
				CacheCurrent();
			}
		}
	}

	void SetResolution(int resolutionSize)
	{
		lightSprite.Position = new(resolutionSize  / 2f, resolutionSize / 2f);
		lightSprite.Scale = new(resolutionSize, resolutionSize);
		subview.Size = new(resolutionSize, resolutionSize);
		float ratio = 512f / resolutionSize;
		renderer.Scale = new(ratio, ratio);
		lightSpriteMat.SetShaderParameter("spriteResolution", resolutionSize);
	}

	void SetDither(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("colorSqueezer", 1.0f - (float)value);
		frames[CurrentFrame()].dither = (float)value;
	}

	void SetQuantization(int value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("quantCount", value);
		frames[CurrentFrame()].valuesCount = value;
	}

	void SetSize(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("size", value);
		frames[CurrentFrame()].centerSize = (float)value;
	}

	void SetAlpha(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("alpha", value);
		frames[CurrentFrame()].alpha = (float)value;
	}

	void SetFeathering(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("feathering", value + 1.01f);
		frames[CurrentFrame()].feathering = (float)value;
	}

	void SetPower(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("powerFactor", value);
		frames[CurrentFrame()].power = (float)value;
	}

	void SetOpenAngle(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("openAngle", value / 360.0f);
		frames[CurrentFrame()].openAngle = (float)value;
	}

	void SetRotation(double value)
	{
		settingsHaveChanged = true;
		lightSpriteMat.SetShaderParameter("rotate", value / 180.0f * 3.14f);
		frames[CurrentFrame()].rotation = (float)value;
	}

	void SetPath(string input)
	{
		settingsHaveChanged = true;
	}

	void SetTextureName(string input)
	{
		settingsHaveChanged = true;
	}

	int CurrentFrame()
	{
		return Mathf.RoundToInt(currentFrameSlider.Value);
	}

	void CheckCurrentFrame()
	{
		SetVisibleParametersOfFrame(CurrentFrame());	
	}

	void SetVisibleParametersOfFrame(int frame)
	{
		// resolution isnt frame bound
		ditherSlider.Value = frames[frame].dither;
		colorCountSlider.Value = frames[frame].valuesCount;
		alphaSlider.Value = frames[frame].alpha;
		sizeSlider.Value = frames[frame].centerSize;
		featherSlider.Value = frames[frame].feathering;
		powerSlider.Value = frames[frame].power;
		openAngleSlider.Value = frames[frame].openAngle;
		rotateSlider.Value = frames[frame].rotation;
	}

	void CheckFrames()
	{
		currentFrameSlider.MaxValue = Mathf.RoundToInt(frameCountSlider.Value) - 1;
		currentFrameSlider.TickCount = Mathf.RoundToInt(currentFrameSlider.MaxValue) + 1;
		currentFrameSlider.Visible = currentFrameSlider.MaxValue > 0;
		GD.Print("new max frame count: " + currentFrameSlider.MaxValue);
	}
	

	void CheckSave()
	{
		int maxFrames = Mathf.RoundToInt(frameCountSlider.Value);
		for (int i = 0; i < maxFrames; i++)
		{
			SetVisibleParametersOfFrame(i);
			string mainName = saveName.Text == "" || saveName.Text == string.Empty ? $"lightmask_x{Mathf.RoundToInt(resSlider.Value)}" : saveName.Text;
			string suffixFull = maxFrames == 1 ? "" : $"_{i}";
			string fullFileName = mainName + suffixFull + ".png";
			string fullPath = Path.Join(savePath.Text == "" || savePath.Text == string.Empty ? ProjectSettings.GlobalizePath("user://") : savePath.Text, fullFileName);
			if (File.Exists(fullPath) && i == 0)
			{
				GD.PrintErr("file already exists, throwing overwrite");
				overwriteConfirmation.Visible = true;
				overwriteConfirmationLabel.Text = $"\"{fullFileName}\" already exists.";
				cachedPath = fullPath;
			}
			else DoTextureSave(fullPath);
		}	
	}

	void CancelOverwriteCheck()
	{
		overwriteConfirmation.Visible = false;
	}

	void AcceptOverwriteCheck()
	{
		DoTextureSave(cachedPath);
		overwriteConfirmation.Visible = false;
	}

	void DoTextureSave(string fullPath)
	{
		Image subviewImage = subview.GetTexture().GetImage();
		ExportImageAsTexture(subviewImage, fullPath);
	}

	void FetchPresetListNames()
	{
		string[] fileNames = Directory.GetFiles(CreateGetPresetPath());
		Godot.Collections.Array<string> strippedNames = [];
		foreach (string name in fileNames)
		{
			string[] separated = name.Split('\\', '.');
			strippedNames.Add(separated[^2]);
			GD.Print("Added preset " + separated[^2] + " to selectable");
		}
		presetSelectionButton.Clear();
		foreach (string item in strippedNames)
			presetSelectionButton.AddItem(item);

		presetSelectionButton.Select(-1);
		currentFrameSlider.Value = 0;
	}

	string CreateGetPresetPath()
	{
		string usedSavePath = PresetFolderPathString();
		if (!Directory.Exists(usedSavePath))
		{
			GD.PrintErr("Preset folder not found, creating " + usedSavePath);
			Directory.CreateDirectory(usedSavePath);
		}
		return usedSavePath;
	}

	string PresetFolderPathString()
	{
		return Path.Join(ProjectSettings.GlobalizePath("user://"), "presets");
	}

	void OpenPresetSave()
	{
		presetSaveConfirmation.Visible = true;
	}

	void CheckIfPresetNameValid(string input)
	{
		presetAcceptSaveButton.Disabled = input == "" || input == string.Empty || input == "." || input == ",";
	}

	void DoPresetSave()
	{
		string usedSavePath = CreateGetPresetPath();

		string jsonData = Json.Stringify(CompileDataDictionary());
		string finalPath = Path.Join(usedSavePath, presetNameField.Text + ".preset");

		try
		{
			File.WriteAllText(finalPath, jsonData);
		}
		catch (System.Exception e)
		{
			GD.PrintErr("Error writing and saving settings " + e);
		}

		GD.Print("Saved settings: " + finalPath);
		SetDebugText("Saved settings preset to " + finalPath);

		FetchPresetListNames();
		ClosePresetSave();
	}

	void ClosePresetSave()
	{
		presetSaveConfirmation.Visible = false;
	}

	void DoPresetLoad(int id)
	{
		if (id == -1) return;

		string presetName = presetSelectionButton.GetItemText(id);
		TryLoadSettings(Path.Join(CreateGetPresetPath(), presetName + ".preset"));
	}

	void CacheCurrent()
	{
		if (!Directory.Exists(userPath))
		{
			GD.PrintErr("Cache path not found, creating");
			Directory.CreateDirectory(userPath);
		}

		string jsonData = Json.Stringify(CompileDataDictionary());
		string finalPath = Path.Join(userPath, "SettingsCache.json");

		try
		{
			File.WriteAllText(finalPath, jsonData);
		}
		catch (System.Exception e)
		{
			GD.PrintErr("Error writing and saving settings " + e);
		}
	}

	Dictionary CompileDataDictionary()
	{
		Godot.Collections.Array<Dictionary> frameSaveDataArray = [];
		for (int i = 0; i < frames.Length; i++)
		{
			Dictionary frameSaveData = new()
			{
				{$"Dither{i}", frames[i].dither},
				{$"Values{i}", frames[i].valuesCount},
				{$"Alpha{i}", frames[i].alpha},
				{$"Center{i}", frames[i].centerSize},
				{$"Feathering{i}", frames[i].feathering},
				{$"Power{i}", frames[i].power},
				{$"Angle{i}", frames[i].openAngle},
				{$"Rotation{i}", frames[i].rotation},
			};
			frameSaveDataArray.Add(frameSaveData);
		}

		Dictionary SettingDictionaryData = new()
		{
			{"Resolution", Mathf.RoundToInt(resSlider.Value)},
			{"Frames", Mathf.RoundToInt(frameCountSlider.Value)},
			{"Frame_data_0", frameSaveDataArray[0]},
			{"Frame_data_1", frameSaveDataArray[1]},
			{"Frame_data_2", frameSaveDataArray[2]},
			{"Frame_data_3", frameSaveDataArray[3]},
			{"Frame_data_4", frameSaveDataArray[4]},
			{"Frame_data_5", frameSaveDataArray[5]},
			{"Frame_data_6", frameSaveDataArray[6]},
			{"Frame_data_7", frameSaveDataArray[7]},
			{"Path", savePath.Text},
			{"Name", saveName.Text},
		};

		return SettingDictionaryData;
	}

	void LoadCache()
	{
		TryLoadSettings(Path.Join(userPath, "SettingsCache.json"));
		settingsHaveChanged = false;
	}

	void TryLoadSettings(string path)
	{
		string attemptedSettingsData = null;
		if (!File.Exists(path)) { GD.PrintErr("Settings save file not found"); return; }

		try { attemptedSettingsData = File.ReadAllText(path); }
		catch (System.Exception e) { GD.Print(e); }

		Json jsonLoader = new();
		Error loadingError = jsonLoader.Parse(attemptedSettingsData);

		if (loadingError != Error.Ok) { GD.Print(loadingError); return; }

		Dictionary loadedData = (Dictionary)jsonLoader.Data;

		int res = 32;
		int frameCount = 1;

		if (loadedData.ContainsKey("Resolution")) res = loadedData["Resolution"].AsInt32();
		resSlider.Value = res;
		if (loadedData.ContainsKey("Frames")) frameCount = loadedData["Frames"].AsInt32();
		frameCountSlider.Value = frameCount;

		for (int i = 0; i < 8; i++)
		{
			if(!loadedData.ContainsKey($"Frame_data_{i}")) continue;

			Dictionary loadedDictionary = (Dictionary)loadedData[$"Frame_data_{i}"];
			float dith = 0.2f;
			int values = 4;
			float alpha = 1.0f;
			float center = 0.5f;
			float feather = 1f;
			float pow = 2f;
			float angle = 360f;
			float rotation = 0f;

			string dName = $"Dither{i}";
			string vName = $"Values{i}";
			string aName = $"Alpha{i}";
			string sizeName = $"Center{i}";
			string feaName = $"Feathering{i}";
			string powName = $"Power{i}";
			string angleName = $"Angle{i}";
			string rotName = $"Rotation{i}";

			if (loadedDictionary.ContainsKey(dName)) dith = (float)loadedDictionary[dName];
			if (loadedDictionary.ContainsKey(vName)) values = loadedDictionary[vName].AsInt32();
			if (loadedDictionary.ContainsKey(aName)) alpha = (float)loadedDictionary[aName];
			if (loadedDictionary.ContainsKey(sizeName)) center = (float)loadedDictionary[sizeName];
			if (loadedDictionary.ContainsKey(feaName)) feather = (float)loadedDictionary[feaName];
			if (loadedDictionary.ContainsKey(powName)) pow = (float)loadedDictionary[powName];
			if (loadedDictionary.ContainsKey(angleName)) angle = loadedDictionary[angleName].AsInt32();
			if (loadedDictionary.ContainsKey(rotName)) rotation = loadedDictionary[rotName].AsInt32();
			
			frames[i].dither = dith;
			frames[i].valuesCount = values;
			frames[i].alpha = alpha;
			frames[i].centerSize = center;
			frames[i].feathering = feather;
			frames[i].power = pow;
			frames[i].openAngle = angle;
			frames[i].rotation = rotation;
		}

		string pth = "";
		string nm = "";
		if (loadedData.ContainsKey("Path")) pth = loadedData["Path"].AsString();
		if (loadedData.ContainsKey("Name")) nm = loadedData["Name"].AsString();
		savePath.Text = pth;
		saveName.Text = nm;

		presetSelectionButton.Select(-1);
		currentFrameSlider.Value = 0.0;
		CheckCurrentFrame();

		SetDebugText("Loaded settings: " + path);
	}

	void ExportImageAsTexture(Image img, string path)
	{
		GD.Print("trying to export to: " + path);
		Vector2I res = img.GetSize();
		// Need to create a new image and copy value as alpha, as the image format of the subview.image does not have alpha (rgb8) -> godot's light mode "mix" doesn't work correctly with the black border...
		// In general I'm sure there is a more elegant and performant method for doing this, but it works well enough with low res images
		Image newImage = Image.CreateEmpty(res.X, res.Y, false, Image.Format.Rgba8);
		for (int y = 0; y < res.Y; y++)
		{
			for (int x = 0; x < res.X; x++)
			{
				Color color = img.GetPixel(x, y);
				newImage.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, color.R));
			}
		}
		newImage.SavePng(path);
		SetDebugText("Exported texture to " + path);
	}

	void SetDebugText(string text)
	{
		debugLabel.Text = text;
	}

	void ResetToDefaults()
	{
		// if frame is 0, reset all params - otherwise, match frame 0
		if (Mathf.RoundToInt(currentFrameSlider.Value) == 0)
		{
			frameCountSlider.Value = 1;
			currentFrameSlider.Value = 0;
			resSlider.Value = 32;
			ditherSlider.Value = defaultReference.dither;
			colorCountSlider.Value = defaultReference.valuesCount;
			alphaSlider.Value = defaultReference.alpha;
			sizeSlider.Value = defaultReference.centerSize;
			featherSlider.Value = defaultReference.feathering;
			powerSlider.Value = defaultReference.power;
			openAngleSlider.Value = defaultReference.openAngle;
			rotateSlider.Value = defaultReference.rotation;
		}
		else
		{
			// resolution isnt frame bound
			ditherSlider.Value = frames[0].dither;
			colorCountSlider.Value = frames[0].valuesCount;
			alphaSlider.Value = frames[0].alpha;
			sizeSlider.Value = frames[0].centerSize;
			featherSlider.Value = frames[0].feathering;
			powerSlider.Value = frames[0].power;
			openAngleSlider.Value = frames[0].openAngle;
			rotateSlider.Value = frames[0].rotation;
		}
		presetSelectionButton.Select(-1);
		settingsHaveChanged = true;
	}
}
