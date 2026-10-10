#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform int frame;
uniform vec2 mouse;

uniform sampler2D inp;

const vec4 WHITE = vec4(1.);
const vec4 BLACK = vec4(vec3(0.), 1.);
const vec2 RES = vec2(1920., 1080.);
const int MINIMAP = 128;

const float DEFAULT = 128./255.;
const ivec2 STATE_MOVE = ivec2(0, 127);
const ivec2 STATE_POS = ivec2(1, 127);
const ivec2 STATE_ROT = ivec2(2, 127);

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

float fullrand(in vec2 co, in int fr, in float seed) {
    return rand(co * rand(mod(float(fr), 30.2376) * seed));
}

// both straight from my ass
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

bool map(in ivec2 ipos) { return state(ipos) == WHITE; }
bool map(in int x, in int y) { return state(ivec2(x, y)) == WHITE; }

/* MOVEMENT */
const float MOVESPEED = 0.08 / 128.;
const vec2 DEFAULT_POS = vec2(6., 122.);

const float H_SENS = 1.5;
const float Y_SENS = 2.;

vec4 handlemovement(in vec4 back, in vec2 mousepos) {
    if (back == vec4(0.)) { return vec4(DEFAULT_POS.x/128., 0., DEFAULT_POS.y/128., 1.); }
    vec2 pos = back.xz;
    float movestrength = MOVESPEED*(state(STATE_MOVE).r - DEFAULT);
    float theta = state(STATE_ROT).r + 3.14 * H_SENS*mousepos.x + 0.5;
    vec2 move = movestrength*vec2(cos(theta), sin(theta));
    // pos += move;
    if (!map(ivec2(floor(pos + move)))) { pos += move; }
    return vec4(pos.x, back.y, pos.y, back.w);
}

const float ROTSPEED = 0.025;

vec4 handlerotation(in vec4 back) {
    if (back == vec4(0.)) { return BLACK; }
    float rotation = state(STATE_MOVE).g;
    back.x += ROTSPEED*(rotation - DEFAULT);
    back.x = mod(back.x, 6.28);
    return back;
}

/* RENDERING */
const float RAY_STEP = 0.05;
const float DOT_CHANCE = 0.0015;
const float WALL_HEIGHT = 2.0;
const float MAX_DIST = 20.;


// figured this out all on my own, without desmos :3
vec4 draw(in vec2 mousepos) {
    if (fullrand(fragTexCoord, frame, 1.) < (1. - DOT_CHANCE)) { return vec4(0.); }
    vec3 rduv = vec3(0.);
    float theta = state(STATE_ROT).r;
    theta += 3.14 * (H_SENS*mousepos.x + 0.3*fragTexCoord.x);
    rduv.x = cos(theta);
    rduv.x *= RES.x/RES.y;
    rduv.z = sin(theta);
    rduv.y += Y_SENS*mousepos.y + fragTexCoord.y;
    vec3 rd = normalize(rduv);
    vec3 ro = state(STATE_POS).xyz * 128.;
    vec3 r;
    for (r = ro; r.y < WALL_HEIGHT && r.y > -WALL_HEIGHT; r += RAY_STEP*rd) {
        if (map(ivec2(floor(r.xz)))) {
            float darkness = distance(r, ro)/(MAX_DIST);
            if (fullrand(fragTexCoord, frame, 1.83) < sqrt(darkness)) { return vec4(0.); }
            return vec4(1. - 0.5*darkness, 0.5 - 10.*(darkness - 0.15), 0., 1.);
        }
        if (distance(r, ro) > MAX_DIST) { return vec4(0.); }
    }
    return vec4(0.5, 0.7 - 10.*(distance(r, ro)/MAX_DIST - 0.15), 0.7, 1.);
}

void main() {
    vec2 pos = fragTexCoord;
    vec4 back = texture(inp, pos);
    ivec2 ipos = ivec2(pos * RES);
    vec2 mousepos = mouse == vec2(0.) ? vec2(-1., 0.) : mouse/RES;
    mousepos.y = clamp(mousepos.y, -0.5, 0.);
    if (ipos.x < MINIMAP && ipos.y < MINIMAP) {
        if (ipos == STATE_POS) { color = handlemovement(back, mousepos);
        } else if (ipos == STATE_ROT) { color = handlerotation(back);
        } else if (distance(ivec2(128.*state(STATE_POS).xy), ipos) < 5) { color == vec4(0., 1., 0., 1.);
        } else { color = back; }
        return;
    }

    vec4 drawcolor = draw(mousepos);
    if (drawcolor.a == 1.) {
        color = drawcolor;
    } else if (back.a > 0.15) {
        color = back - 0.05;
    } else {
        color = BLACK;
    }
}
