shader_type spatial;
render_mode unshaded;

float gradientNoise(in vec2 uv)
{
	return fract(52.9829189 * fract(dot(uv, vec2(0.06711056, 0.00583715))));
}

void fragment() {
	ALBEDO = (mix(vec3(0.125, 0.215, 0.431), vec3(0.227, 0.494, 0.760), .7 - UV.y) * 1.) + (gradientNoise(FRAGCOORD.xy) / 128.);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
