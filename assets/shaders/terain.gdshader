shader_type spatial;
render_mode unshaded;

uniform sampler2D tex: filter_nearest;
varying float light;

void vertex() {
	light = CUSTOM0.r;
	//VERTEX += CUSTOM1;
}

void fragment() {
	vec4 texx = texture(tex, UV);
	ALBEDO = texx.rgb * light;
	
	//ALBEDO = vec3(UV, 0);

	//ALBEDO = vec3(light);
}
