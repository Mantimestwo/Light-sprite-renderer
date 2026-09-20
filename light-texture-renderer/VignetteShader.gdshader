shader_type canvas_item;

uniform float openAngle : hint_range(0.0, 1.0) = 1.0;
uniform float rotate : hint_range(-3.14, 3.14);

uniform float state: hint_range(0,1) = 0.0f;
uniform float testfloat: hint_range(0,1) = 0.0f;
uniform float usePixelShift : hint_range(0.0, 1.0, 1.0) = 1.0;
uniform float alpha : hint_range(0,1) = 1.0f;
uniform float feathering : hint_range(1.01, 6.0, 0.01) = 1.01;
uniform float size : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform sampler2D noiseTex : filter_nearest, repeat_enable;
uniform float spriteResolution : hint_range(8, 512, 4) = 16;
uniform int noiseResolution = 16;

uniform int quantCount = 2;

uniform float powerFactor = 3.0;
uniform float colorSqueezer: hint_range(0.01,1.0) = 1.0f;

float calculateColor(float currentColor, float wantedCount)
{
	return floor(currentColor*(wantedCount))/wantedCount;
}

float SmoothStepMethod(float percentage)
{
	if (percentage == 0.0) { return 0.0; }
	float v1 = percentage * percentage;
	float v2 = 1.0 - (1.0 - percentage) * (1.0 - percentage);
	return mix(v1, v2, percentage);
}

vec2 pivotRotate(vec2 uv, float radian, vec2 rotatePivot){
	float x = ((uv.x-rotatePivot.x) * cos(radian)) + ((rotatePivot.y-uv.y) * -sin(radian)) + rotatePivot.x;
	float y = ((uv.y-rotatePivot.y) * cos(radian)) + ((rotatePivot.x-uv.x) *  sin(radian)) + rotatePivot.y;
	return vec2(x,y);
}

float in_cooldown(vec2 uv, float direction, float shifter) {
	vec2 centered_uv = uv - vec2(0.0 - shifter, 0.5 - shifter);
	vec2 rotated = pivotRotate(vec2(centered_uv.x, centered_uv.y),rotate, vec2(.5,.0));
	float angle = (atan(rotated.x * -direction + 0.5 * direction, -rotated.y) / (2.0 * PI) + 0.5);
	return clamp(1.0 - round((angle)/(openAngle)), 0.0, 1.0);
}

void fragment() {
	vec2 pixelated_uv = vec2(calculateColor(UV.x, spriteResolution),calculateColor(UV.y, spriteResolution));

	float noiseScale =  spriteResolution / float(noiseResolution);

	vec4 noise = texture(noiseTex,noiseScale * vec2(calculateColor(UV.x, spriteResolution),calculateColor(UV.y, spriteResolution)));
	float pixelShifter =  1.0 / float(spriteResolution) / 2.0 * usePixelShift;
	vec2 rotated_uv = pivotRotate(vec2(pixelated_uv.x , pixelated_uv.y),rotate, vec2(0.5 - pixelShifter));
	float x = abs(rotated_uv.x - 0.5 + pixelShifter);
	float y = abs(rotated_uv.y - 0.5 + pixelShifter);
	float trueSize = size / powerFactor;
	float openArea = in_cooldown(pixelated_uv, 1.0, pixelShifter) + in_cooldown(pixelated_uv, -1.0, pixelShifter);
	float q = 1.0-(1.0-sqrt(pow(x,powerFactor)+pow(y,powerFactor))/trueSize)/(1.0-feathering);

	float dither = clamp(noise.r,colorSqueezer,1.0);
	float alp = q*dither;
	COLOR.a = calculateColor(alp*state, float(quantCount)) * openArea;
	COLOR.a = clamp(COLOR.a, .0, 1.0);
	COLOR = mix(COLOR,vec4(noise.r, noise.g,noise.b,1.0),testfloat);
	COLOR.a = clamp(COLOR.a * alpha, .0, 1.0);
}
