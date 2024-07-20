shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE: hint_screen_texture;

void fragment() {
	COLOR = vec4(vec3(1, 1, 1) - texture(SCREEN_TEXTURE, SCREEN_UV).rgb, 1);
}
