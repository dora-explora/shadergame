#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform sampler2D inp;

const vec4 WHITE = vec4(1.);
const vec4 BLACK = vec4(vec3(0.), 1.);
const vec2 RES = vec2(1920., 1080.);
const int MINIMAP = 128;

const float DEFAULT = 128./255.;
const ivec2 STATE_FRAME = ivec2(0, 127);
const ivec2 STATE_MOVE = ivec2(1, 127);
const ivec2 STATE_POS = ivec2(2, 127);
const ivec2 STATE_ROT = ivec2(3, 127);

/* UTIL */
// both from LYGIA by Patricio Gonzalez Vivo with David Hoskins and (ofc) Inigo Quilez
float rand(in float seed) {
    seed = fract(seed * 443.897);
    seed *= seed + 33.33;
    seed *= seed + seed;
    return fract(seed);
}

float sinrand(in float seed) {
    return fract(sin(seed) * 43758.5453);
}

// from the Book of Shaders also by Patricio Gonzalez Vivo
float rand(vec2 co){
  return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

bool brand(in float seed) { return (rand(seed) < 0.5); }

float fmod(in float n, in float m) {
    float x = n;
    while (x > m) { x -= m; }
    return x;
}

/* STATE */
vec4 state(in ivec2 ipos) {
    return texture(inp, vec2(ipos) / RES);
}

bool map(in ivec2 ipos) { return state(ipos).a == 1; }
bool map(in int x, in int y) { return state(ivec2(x, y)).a == 1; }

uint frame() {
    uvec4 framevec = uvec4(255.*state(STATE_FRAME));
    return (framevec.r << 24) + (framevec.g << 16) + (framevec.b << 8) + framevec.a;
}

float fullrand(in float seed) {
    return rand(fragTexCoord * sinrand(mod(float(frame()), 30.2376) * seed));
}

/* MOVEMENT */
const float MOVESPEED = 0.4;
const vec4 DEFAULT_POS = vec4(6./128., 0., 122./128., 1.);

vec4 handlemovement(in vec4 back) {
    if (back == vec4(0.)) { return DEFAULT_POS; }
    vec2 pos = back.xz * 128.;
    float movestrength = MOVESPEED*(state(STATE_MOVE).r - DEFAULT);
    float theta = state(STATE_ROT).r + 0.5;
    vec2 move = movestrength*vec2(cos(theta), RES.y/RES.x*sin(theta)); // the weird scaling is because of the same thing in draw() i think
    pos += move;
    if (map(ivec2(pos))) { pos -= move; }
    return vec4(pos.x / 128., back.y, pos.y / 128., back.w);
}

const vec4 DEFAULT_ROT = vec4(4.65, 0.9, 0., 1.);
const float ROTSPEED = 0.05;
const float H_SENS = 0.4;
const float Y_SENS = 0.8;

vec4 handlerotation(in vec4 back) {
    if (back == vec4(0.)) { return DEFAULT_ROT; }
    vec4 move = state(STATE_MOVE);
    back.r += ROTSPEED*(move.g - DEFAULT) + H_SENS*(move.b - DEFAULT);
    back.r = mod(back.r, 6.28);
    back.g += Y_SENS*(move.a - DEFAULT);
    back.g = clamp(back.g, -0.5, 0.5);
    return back;
}

/* RENDERING */
const float RAY_STEP = 0.05;
 const float DOT_CHANCE = 0.003;
 // const float DOT_CHANCE = 1.;
const float WALL_HEIGHT = 2.0;
const float MAX_DIST = 20.;


// figured this out all on my own, without desmos :3
vec4 draw() {
    if (fullrand(0.3) < (1. - DOT_CHANCE)) { return vec4(0.); }
    vec4 rot = state(STATE_ROT);
    float theta = rot.r + fragTexCoord.x;
    vec3 rduv = vec3(
        cos(theta) * RES.x/RES.y,
        fragTexCoord.y - 0.5 + rot.g,
        sin(theta)
    );
    vec3 rd = normalize(rduv);
    vec3 ro = state(STATE_POS).xyz * 128.;
    float dist = 0.;
    float step_dist = length(RAY_STEP*rd)/MAX_DIST;
    for (vec3 r = ro; r.y < WALL_HEIGHT && r.y > -WALL_HEIGHT; r += RAY_STEP*rd) {
	dist += step_dist;
        if (map(ivec2(floor(r.xz)))) {
            if (fullrand(0.41) < sqrt(dist)) { return vec4(0.); }
            return vec4(1. - 0.2*dist, 0.5 - 10.*(dist - 0.15), 0., 1.);
        }
        if (distance(r, ro) > MAX_DIST) { return vec4(0.); }
    }
    return vec4(0.5, 0.55 - 10.*(dist -  0.15), 0.7, 1.);
}

void main() {
    vec2 pos = fragTexCoord;
    vec4 back = texture(inp, pos);
    ivec2 ipos = ivec2(pos * RES);
    if (ipos.x < MINIMAP && ipos.y < MINIMAP) {
        if (ipos == STATE_POS) {
            color = handlemovement(back);
        } else if (ipos == STATE_ROT) {
            color = handlerotation(back);
        // } else if (distance(ivec2(128.*state(STATE_POS).xz), ipos) < 3 && back != WHITE) {
            // color = vec4(0., 1., 1., 0.3);
        } else {
            color = back;
        }
        return;
    }

    vec4 drawcolor = draw();
    if (drawcolor.a == 1.) {
        color = drawcolor;
    } else if (back.a > 0.15) {
        color = back - 0.1;
    } else {
        color = BLACK;
    }
}
