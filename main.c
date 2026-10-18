#include <raylib.h>
#include <rlgl.h>
#include <stdint.h>
// #include <stdio.h> // for debug

const int WIDTH = 1920;
const int HEIGHT = 1080;
const int MINIMAP = 128; // side length of state texture

// this function was entirely claude
RenderTexture2D LoadRenderTextureF32(int w, int h)
{
    RenderTexture2D rt = { 0 };
    rt.id = rlLoadFramebuffer();   // older raylib (<5.0): rlLoadFramebuffer(w, h)
    if (rt.id == 0) return rt;

    rlEnableFramebuffer(rt.id);
    rt.texture.id      = rlLoadTexture(0, w, h, PIXELFORMAT_UNCOMPRESSED_R32G32B32A32, 1);
    rt.texture.width   = w;
    rt.texture.height  = h;
    rt.texture.format  = PIXELFORMAT_UNCOMPRESSED_R32G32B32A32;
    rt.texture.mipmaps = 1;

    rlFramebufferAttach(rt.id, rt.texture.id, RL_ATTACHMENT_COLOR_CHANNEL0,
                        RL_ATTACHMENT_TEXTURE2D, 0);
    if (!rlFramebufferComplete(rt.id)) TraceLog(LOG_ERROR, "FBO incomplete");
    rlDisableFramebuffer();

    SetTextureFilter(rt.texture, TEXTURE_FILTER_POINT);  // no interpolation of state!
    SetTextureWrap(rt.texture, TEXTURE_WRAP_CLAMP);
    return rt;
}

void DrawState(RenderTexture2D *target, Image *bg, uint32_t frame) {
    Color framecol = (Color) {
	(frame & 0xFF000000) >> 24,
	(frame & 0x00FF0000) >> 16,
	(frame & 0x0000FF00) >> 8,
	(frame & 0x000000FF)
    };
    ImageDrawPixel(bg, 0, MINIMAP-1, framecol);

    unsigned char r = 128;
    unsigned char g = 128;
    if (IsKeyDown(KEY_W) || IsKeyDown(KEY_UP)   ) { r += 127; }
    if (IsKeyDown(KEY_A) || IsKeyDown(KEY_LEFT) ) { g -= 127; }
    if (IsKeyDown(KEY_S) || IsKeyDown(KEY_DOWN) ) { r -= 127; }
    if (IsKeyDown(KEY_D) || IsKeyDown(KEY_RIGHT)) { g += 127; }
    Vector2 mousedelta = GetMouseDelta();
    int dx = (int) (mousedelta.x + 128.);
    if (dx < 0) dx = 0; if (dx > 255) dx = 255;
    int dy = (int) (mousedelta.y + 128.);
    if (dy < 0) dy = 0; if (dy > 255) dy = 255;
    ImageDrawPixel(bg, 1, MINIMAP-1, (Color) {r, g, dx, dy});

    Texture2D state = LoadTextureFromImage(*bg);
    BeginTextureMode(*target); {
        DrawTexturePro(
            state,
            (Rectangle) {0, 0, MINIMAP, -MINIMAP},
            (Rectangle) {0, HEIGHT - MINIMAP, MINIMAP, MINIMAP},
            (Vector2) {0, 0}, 0., WHITE
        );
    } EndTextureMode();
    UnloadTexture(state);
}

int main(void) {
    SetTraceLogLevel(LOG_WARNING);

    InitWindow(WIDTH, HEIGHT, "Shader Demo");
    HideCursor();
    DisableCursor();

    Shader shader = LoadShader(0, "./shader.fs");
    RenderTexture2D targetA = LoadRenderTextureF32(WIDTH, HEIGHT);
    RenderTexture2D targetB = LoadRenderTextureF32(WIDTH, HEIGHT);
    RenderTexture2D *inTarget = &targetA;
    RenderTexture2D *outTarget = &targetB;
    Image map = LoadImage("assets/map.png");
    Image blank = GenImageColor(MINIMAP, MINIMAP, BLANK);

    uint32_t frame = 0;
    int inputLoc = GetShaderLocation(shader, "inp");

    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        inTarget = outTarget;

        frame++;
        // SetShaderValue(shader, frameLoc, &frame, SHADER_UNIFORM_UINT);

        DrawState(inTarget, frame == 1 ? &map : &blank, frame);
        BeginTextureMode(*outTarget); {
            BeginShaderMode(shader); {
                SetShaderValueTexture(shader, inputLoc, inTarget->texture);
                DrawTextureRec(inTarget->texture, (Rectangle) {0, 0, WIDTH, -HEIGHT}, (Vector2) {0, 0}, WHITE);
                // ^this^ is reversed to put (0, 0) in the top left
            } EndShaderMode();
        } EndTextureMode();

        BeginDrawing(); {
            DrawTexture(outTarget->texture, 0, 0, WHITE);
        } EndDrawing();
    }

    UnloadShader(shader);
    UnloadRenderTexture(targetA);
    UnloadRenderTexture(targetB);

    CloseWindow();
    return 0;
}
