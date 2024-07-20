shader_type spatial;
render_mode wireframe, unshaded; //, cull_disabled;

uniform vec3 color: source_color = vec3(1);

void fragment() {
	ALPHA = ALPHA;
	ALBEDO = color;
}
