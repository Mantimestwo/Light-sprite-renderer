These are the project files of a simple quickly thrown together tool made using Godot C# for procedurally creating and creating png files for use as light-sprites for Godot's light nodes. 
This was created as an optimal solution to the need of having different sizes of sprites, without the need of manually drawing sprites individually.

Supports saveable 'Presets', as well as automatically exported multiple frames (limited by default to 8 maximum for personal preference, but more could be implemented via editing the savedata controller code, frame-resource array count and frame slider maximums in MainController.cs).

The dither texture used is a rough manually drawn estimation of a 16x16 bayer dither texture.
There is unfortunately no in-app implementation for changing this as of now, but it can be changed to another texture via the project files by changing the editing the material of subviewport -> sprite, then changing the material's parameters "noiseTex" as well as "noiseResolution" to match the texture resolution so that it scales correctly.

<img width="666" height="360" alt="light_renderer" src="https://github.com/user-attachments/assets/7498437f-8fd0-490d-b12e-0e781023f77e" />

If you simply want the plain executable, it can be downloaded via itch.io [here](https://mantimestwo.itch.io/godot-light-sprite-renderer)




