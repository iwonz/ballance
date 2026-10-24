// Runs against the shipped dylib, without Unity or an external IVP source checkout.
#include "../BallancePhysics/ballance_physics.h"
#include <dlfcn.h>
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
  require(argc == 2, "Usage: physics-smoke /absolute/path/libbphysics.dylib");
  void* library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
  if (!library) {
    std::fprintf(stderr, "%s\n", dlerror());
    return 1;
  }
  auto entry = reinterpret_cast<void* (*)(int, sInitStruct*)>(dlsym(library, "ballance_physics_entry"));
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
  void* world = createWorld(point, 1.0f / 66.0f, -1, collisionMasks, nullptr);
  require(world != nullptr, "Physics world creation failed");
  for (int frame = 0; frame < 120; ++frame)
    simulate(world, 1.0f / 60.0f);
  destroyWorld(world);
  destroyPoint(point);
  entry(0, nullptr);
  require(entry(4, nullptr) == nullptr, "Physics shutdown failed");
  dlclose(library);
  std::puts("ARM64 physics smoke test passed (ABI 2302)");
  return 0;
}
