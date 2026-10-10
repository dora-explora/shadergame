all: shaderdemo

shadergame: main.c
	gcc main.c -o build/shadergame -lraylib

run: shadergame
	build/shadergame

static:
		gcc main.c -o build/shadergame -Ibuild/raylib lib/libraylib.a -lm -ldl -lpthread -lrt -lX11 -lXrandr -lXinerama -lXxf86vm -lXcursor -lXfixes -lfreetype -lGL
