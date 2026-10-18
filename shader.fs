#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform sampler2D inp;

const vec4 WHITE = vec4(1.);
const vec4 BLACK = vec4(0., 0., 0., 1.);
const vec4 BLUE  = vec4(0., 0., 1., 1.);
const vec2 RES = vec2(1920., 1080.);
const int MINIMAP = 128;

const float DEFAULT = 128./255.;
const vec2 STATE_FRAME = vec2(0, 127);
const vec2 STATE_MOVE = vec2(1, 127);
const vec2 STATE_POS = vec2(2, 127);
const vec2 STATE_ROT = vec2(3, 127);

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
vec4 state(in vec2 pos) { return texture(inp, pos / RES); }
bool map(in vec2 pos) { return state(pos) == WHITE; }

uint frame() {
    uvec4 framevec = uvec4(255.*state(STATE_FRAME));
    return (framevec.r << 24) + (framevec.g << 16) + (framevec.b << 8) + framevec.a;
}

float fullrand(in float seed) {
    return rand(fragTexCoord * sinrand(mod(float(frame()), 30.2376) * seed));
}

/* MOVEMENT */
const float MOVESPEED = 0.3;
const vec4 DEFAULT_POS = vec4(5./128., 0., 122./128., 1.);

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

const vec4 DEFAULT_ROT = vec4(3.65, -0.9, 0., 1.);
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

/* COINS */
float COIN_SEARCH = 4.;

vec3 drawcoin(in vec3 r) {
    for (float i = -COIN_SEARCH; i <= COIN_SEARCH; i += 1.) {
        for (float j = -COIN_SEARCH; j <= COIN_SEARCH; j += 1.) {
	    vec2 pos = floor(r.xz) + vec2(i, j);
	    if (state(pos) == BLUE) {
		float coindist = sqrt(distance(vec3(pos.x, 0., pos.y) + vec3(0.5), r));
		return vec3(
		    max(1.8 - coindist, 0.),
		    max(2.2 - coindist, 0.),
		    max(2.1 - coindist, 0.)
		);
	    }
	}
    }
    return vec3(0.);
}

vec4 handlecoin() {
    if (distance(state(STATE_POS).xz * 128., fragTexCoord * RES) < 2.) {
        return BLACK;
    } else {
        return BLUE;
    }
}

/* RENDERING */
const float RAY_STEP = 0.05;
const float DOT_CHANCE = 0.004;
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
    vec3 r;
    for (r = ro; r.y < WALL_HEIGHT && r.y > -WALL_HEIGHT; r += RAY_STEP*rd) {
	dist += step_dist;
        if (map(ivec2(floor(r.xz)))) {
            if (fullrand(0.41) < sqrt(dist)) { return vec4(0.); }
	    vec3 coin = drawcoin(r);
            return vec4(vec3(1. - 0.2*dist, 0.5 - 10.*(dist - 0.15), 0.) + 1.5*coin, 1.);
        }
        if (distance(r, ro) > MAX_DIST) { return vec4(0.); }
    }
    vec3 coin = drawcoin(r);
    return vec4(vec3(0.5, 0.55 - 10.*(dist -  0.15), 0.7) + coin, 1.);
}

void main() {
    vec4 back = texture(inp, fragTexCoord);
    ivec2 ipos = ivec2(fragTexCoord * RES);
    if (ipos.x < MINIMAP && ipos.y < MINIMAP) {
        if (ipos == STATE_POS) {
            color = handlemovement(back);
        } else if (ipos == STATE_ROT) {
            color = handlerotation(back);
        } else if (back == BLUE) {
            color = handlecoin();
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
