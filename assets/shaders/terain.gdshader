shader_type spatial;
render_mode unshaded;

uniform float CURVATURE;
uniform bool CURVATURE_ACTIVE = false;
uniform float CURVATURE_DISTANCE;
uniform sampler2D tex: filter_nearest;
varying float light;

void vertex() {
	light = CUSTOM0.r;
	//VERTEX += CUSTOM1;

	if (CURVATURE_ACTIVE == true) {
		NORMAL = (MODEL_MATRIX * vec4(VERTEX, 0.0)).xyz;
		float dist = length(MAIN_CAM_INV_VIEW_MATRIX	[3].xyz - VERTEX) / CURVATURE_DISTANCE;
		VERTEX.y -= pow(dist, CURVATURE);
	}

	//VERTEX -= vec3(0, distance(CAMERA_POSITION_WORLD, VERTEX) / 4., 0);
}

void fragment() {
	vec4 texx = texture(tex, UV);
	ALBEDO = texx.rgb * light;

	//ALBEDO = vec3(UV, 0);

	//ALBEDO = vec3(light);
}
