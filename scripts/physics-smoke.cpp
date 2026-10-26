// Runs against the shipped dylib, without Unity or an external IVP source checkout.
#include "../BallancePhysics/ballance_physics.h"
#ifndef __EMSCRIPTEN__
#include <dlfcn.h>
#endif
#include <cstdio>
#include <cstdlib>
#include <cstring>

static void require(bool condition, const char* message) {
  if (!condition) {
    std::fprintf(stderr, "%s\n", message);
    std::exit(1);
  }
}

int main(int argc, char** argv) {
#ifdef __EMSCRIPTEN__
  (void)argc;
  (void)argv;
  auto entry = &ballance_physics_entry;
#else
  require(argc == 2, "Usage: physics-smoke /absolute/path/libbphysics.dylib");
  void* library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
  if (!library) {
    std::fprintf(stderr, "%s\n", dlerror());
    return 1;
  }
  auto entry = reinterpret_cast<void* (*)(int, sInitStruct*)>(dlsym(library, "ballance_physics_entry"));
#endif
  require(entry != nullptr, "Missing physics entry point");
  require(entry(4, nullptr) == nullptr, "Physics unexpectedly initialized");
  sInitStruct options = {};
  options.smallPoolSize = 16;
  std::strcpy(options.key, "666dccad4ae697b45aac145f18f49c5b");
  auto api = static_cast<void**>(entry(1, &options));
  require(api != nullptr && entry(4, nullptr) != nullptr, "Physics initialization failed");
  // Indices and signatures match ApiStruct.cs and init_functions in ballance_physics.cpp.
  auto version = reinterpret_cast<int (*)()>(api[0]);
  require(version && version() == 2302, "Physics ABI version does not match the C# wrapper");
  auto createPoint = reinterpret_cast<void* (*)(float, float, float)>(api[9]);
  auto destroyPoint = reinterpret_cast<void (*)(void*)>(api[11]);
  auto getPoint = reinterpret_cast<void (*)(void*, float*)>(api[13]);
  void* point = createPoint(1.0f, -9.81f, 3.0f);
  require(point != nullptr, "Point allocation failed");
  float values[3] = {};
  getPoint(point, values);
  require(values[0] == 1.0f && values[1] == -9.81f && values[2] == 3.0f, "Physics vector ABI mismatch");
  auto createWorld = reinterpret_cast<void* (*)(void*, float, int, int*, EventCallback)>(api[1]);
  auto destroyWorld = reinterpret_cast<void (*)(void*)>(api[2]);
  auto simulate = reinterpret_cast<void (*)(void*, float)>(api[3]);
  int collisionMasks[32];
  for (int& mask : collisionMasks)
    mask = -1;
  void* gravity = createPoint(0, -9.81f, 0);
  void* world = createWorld(gravity, 1.0f / 66.0f, -1, collisionMasks, nullptr);
  require(world != nullptr, "Physics world creation failed");
  auto createQuat = reinterpret_cast<void* (*)(float, float, float, float)>(api[10]);
  auto destroyQuat = reinterpret_cast<void (*)(void*)>(api[12]);
  auto physicalize = reinterpret_cast<void* (*)(void*, const char*, int, int, int, int,
    float, float, float, float, float, float, int, int, int, int, int, int,
    void*, void*, void*, int, const char*, int, void**, int, void**, float, int)>(api[18]);
  auto unphysicalize = reinterpret_cast<void (*)(void*, void*, int)>(api[19]);
  void* rotation = createQuat(0, 0, 0, 1);
  void* floorPosition = createPoint(0, -100, 0);
  void* ballPosition = createPoint(0, 3, 0);
  void* floor = physicalize(world, "floor", 0, 0, 0, 0, 1, 0.5f, 0.1f, 0, 0, 100,
    1, 0, 1, 1, 0, 1, floorPosition, nullptr, rotation, 0, "", 0, nullptr, 0, nullptr, 0, 0);
  void* ball = physicalize(world, "ball", 0, 0, 0, 0, 1, 0.5f, 0.1f, 0, 0, 1,
    1, 0, 1, 1, 0, 0, ballPosition, nullptr, rotation, 0, "", 0, nullptr, 0, nullptr, 0, 0);
  require(floor && ball, "Physics body creation failed");
  for (int frame = 0; frame < 120; ++frame)
    simulate(world, 1.0f / 60.0f);
  auto speed = reinterpret_cast<float (*)(void*)>(api[27]);
  require(speed(ball) < 0.1f, "Ball did not settle on the static collider");
  auto fixedConstraint = reinterpret_cast<void* (*)(void*, void*)>(api[59]);
  auto destroyConstraint = reinterpret_cast<void (*)(void*)>(api[62]);
  void* joint = fixedConstraint(ball, floor);
  require(joint != nullptr, "Constraint creation failed");
  destroyConstraint(joint);
  unphysicalize(world, ball, 1);
  unphysicalize(world, floor, 1);
  destroyPoint(ballPosition);
  destroyPoint(floorPosition);
  destroyQuat(rotation);
  destroyWorld(world);
  destroyPoint(gravity);
  destroyPoint(point);
  entry(0, nullptr);
  require(entry(4, nullptr) == nullptr, "Physics shutdown failed");
#ifndef __EMSCRIPTEN__
  dlclose(library);
#endif
  std::puts("Physics smoke test passed (ABI 2302)");
  return 0;
}
