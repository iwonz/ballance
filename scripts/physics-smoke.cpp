// Runs against the shipped dylib, without Unity or an external IVP source checkout.
#include "../BallancePhysics/ballance_physics.h"
#if defined(_WIN32)
#include <windows.h>
#elif !defined(__EMSCRIPTEN__)
#include <dlfcn.h>
#endif
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cmath>
#include <limits>

static_assert(sizeof(sSurfaceState) == 20, "Surface state layout");
static_assert(sizeof(sSurfaceRegion) == 40, "Surface region layout");
static_assert(sizeof(sSurfaceContact) == 56, "Surface contact layout");

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
#elif defined(_WIN32)
  require(argc == 2, "Usage: physics-smoke path/to/bphysics.dll");
  auto library = LoadLibraryA(argv[1]);
  require(library != nullptr, "Cannot load physics DLL");
  auto entry = reinterpret_cast<void* (*)(int, sInitStruct*)>(GetProcAddress(library, "ballance_physics_entry"));
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
  require(version && version() == 2304, "Physics ABI version does not match the C# wrapper");
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
  auto setState = reinterpret_cast<int (*)(void*, const sSurfaceState*, float, float, float)>(api[90]);
  auto setRegions = reinterpret_cast<int (*)(void*, int, int, const sSurfaceRegion*)>(api[91]);
  auto getContact = reinterpret_cast<int (*)(void*, sSurfaceContact*)>(api[92]);
  auto getId = reinterpret_cast<int (*)(void*)>(api[69]);
  sSurfaceRegion region = { {0, 100, 0}, {30, 3, 30}, 1, 1, 1, 1 };
  require(setRegions(floor, 6, 1, &region) && setRegions(ball, 1, 0, nullptr), "Surface registration failed");
  sSurfaceState state = {1, 0, 0, 0, 0};
  require(setState(world, &state, 0, 0, 0), "Wet state rejected");
  simulate(world, 1.0f / 66);
  sSurfaceContact contact = {};
  require(getContact(ball, &contact) && std::fabs(contact.friction - .16f) < .001f,
    "Wet material pair did not update the existing contact");
  require(contact.supportId == getId(floor), "Contact support identity does not match the physical body");
  state = {0, 0, 1, 0, 0};
  require(setState(world, &state, 0, 0, 0), "Ice state rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction - .055f) < .001f,
    "Local ice contact did not reduce grip");
  region.center[0] = region.center[2] = 28;
  require(setRegions(floor, 6, 1, &region), "Rounded region rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction - .25f) < .001f,
    "A rectangular corner outside the rounded ice footprint changed grip");
  region.center[0] = 24; region.center[2] = 0;
  require(setRegions(floor, 6, 1, &region), "Feathered region rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && contact.friction > .06f && contact.friction < .245f,
    "Organic ice boundary did not blend contact grip");
  float featheredFriction = contact.friction;
  region.extent[1] = 100;
  require(setRegions(floor, 6, 1, &region), "Thick region rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction-featheredFriction) < .001f,
    "Covering projection changed when its support thickness exceeded its radius");
  region.extent[1] = 3;
  region.center[0] = 100;
  require(setRegions(floor, 6, 1, &region), "Moved region rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction - .25f) < .001f,
    "Ice outside its physical region changed grip");
  region.center[0] = 0;
  require(setRegions(floor, 6, 1, &region), "Region restore rejected");
  state = {0, 1, 0, 0, 0};
  require(setState(world, &state, 0, 0, 0), "Snow state rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && contact.resistance > .6f, "Snow resistance missing");
  state = {};
  require(setState(world, &state, 0, 0, 0), "Dry state rejected");
  simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction - .25f) < .001f && contact.resistance == 0,
    "Returning to dry did not restore the original contact");
  // Exercise every ball/surface pair on an existing settled contact.
  float wetMetal[3] = {}, snowDrag[3] = {};
  for (int kind = 1; kind <= 3; ++kind) {
    require(setRegions(ball, kind, 0, nullptr), "Ball material registration failed");
    for (int surface = 4; surface <= 7; ++surface) {
      require(setRegions(floor, surface, 1, &region), "Floor material registration failed");
      state = {1, 0, 0, 0, 0};
      require(setState(world, &state, 0, 0, 0), "Wet material state rejected");
      simulate(world, 1.0f / 66);
      require(getContact(ball, &contact) && contact.friction > .1f && contact.friction < .25f && contact.resistance == 0,
        "Wet pair lost contact, became frictionless, or added loose-cover drag");
      if (surface == 6) wetMetal[kind - 1] = contact.friction;
    }
    state = {0, 1, 0, 0, 0};
    require(setState(world, &state, 0, 0, 0), "Snow pair state rejected");
    simulate(world, 1.0f / 66);
    require(getContact(ball, &contact), "Snow pair lost contact");
    snowDrag[kind - 1] = contact.resistance;
    state = {0, 0, 0, 1, 0};
    require(setState(world, &state, 0, 0, 0), "Sand pair state rejected");
    simulate(world, 1.0f / 66);
    require(getContact(ball, &contact) && contact.resistance > snowDrag[kind - 1],
      "Sand did not add distinct rolling resistance");
  }
  require(wetMetal[0] < wetMetal[1] && wetMetal[1] < wetMetal[2], "Wet ball materials are indistinguishable");
  require(snowDrag[1] > 0 && snowDrag[1] < snowDrag[0] && snowDrag[0] < snowDrag[2], "Loose cover ignores ball material");
  require(setRegions(ball, 1, 0, nullptr) && setRegions(floor, 6, 1, &region), "Original test materials could not be restored");
  state = {};
  state.wetness = std::numeric_limits<float>::quiet_NaN();
  require(!setState(world, &state, 0, 0, 0), "NaN surface state accepted");
  state = {};
  require(!setState(world, &state, 3, 0, 0), "Unbounded wind accepted");
  require(!setRegions(floor, 6, 9, &region), "Oversized region array accepted");
  auto beam = reinterpret_cast<void (*)(void*, void*, void*, int)>(api[26]);
  void* turned = createQuat(0, 0, .70710678f, .70710678f);
  void* shiftedFloor = createPoint(12, -100, 0);
  void* shiftedBall = createPoint(12, 3, 0);
  beam(floor, turned, shiftedFloor, 0);
  beam(ball, rotation, shiftedBall, 0);
  region = { {100, 0, 0}, {3, 30, 30}, 1, 1, 1, 1 };
  require(setRegions(floor, 6, 1, &region), "Rotated support region rejected");
  state = {0, 0, 1, 0, 0};
  require(setState(world, &state, 0, 0, 0), "Rotated support ice rejected");
  for (int frame = 0; frame < 120; ++frame) simulate(world, 1.0f / 66);
  require(getContact(ball, &contact) && std::fabs(contact.friction - .055f) < .001f,
    "Local covering did not follow the translated and rotated support");
  require(contact.supportId == getId(floor), "Moving support identity changed");
  destroyPoint(shiftedFloor);
  destroyPoint(shiftedBall);
  destroyQuat(turned);
  auto fixedConstraint = reinterpret_cast<void* (*)(void*, void*)>(api[59]);
  auto destroyConstraint = reinterpret_cast<void (*)(void*)>(api[62]);
  void* joint = fixedConstraint(ball, floor);
  require(joint != nullptr, "Constraint creation failed");
  destroyConstraint(joint);
  auto getSpeedVector = reinterpret_cast<void (*)(void*, void*)>(api[28]);
  void* windSpeed = createPoint(0, 0, 0);
  float airSpeed[3] = {};
  state = {};
  require(setState(world, &state, 1, 0, 0), "Prepared bounded wind rejected");
  for (int kind = 1; kind <= 3; ++kind) {
    void* airbornePosition = createPoint(300 + kind * 50, 100, 0);
    void* airborne = physicalize(world, "wind-ball", 0, 0, 0, 0, 1, .5f, .1f, 0, 0, 1,
      1, 0, 1, 1, 0, 0, airbornePosition, nullptr, rotation, 0, "", 0, nullptr, 0, nullptr, 0, 0);
    require(airborne && setRegions(airborne, kind, 0, nullptr), "Airborne material creation failed");
    for (int frame = 0; frame < 66; ++frame) simulate(world, 1.0f / 66);
    getSpeedVector(airborne, windSpeed); getPoint(windSpeed, values);
    airSpeed[kind - 1] = values[0];
    require(std::isfinite(values[0]) && values[0] > 0 && values[0] <= 1.05f, "Native wind impulse is absent or unbounded");
    unphysicalize(world, airborne, 1); destroyPoint(airbornePosition);
  }
  require(airSpeed[1] < airSpeed[0] && airSpeed[0] < airSpeed[2], "Wind ignores ball material sensitivity");
  require(setState(world, &state, 0, 0, 0), "Wind reset rejected");
  destroyPoint(windSpeed);
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
#if defined(_WIN32)
  FreeLibrary(library);
#elif !defined(__EMSCRIPTEN__)
  dlclose(library);
#endif
  std::puts("Physics smoke test passed (ABI 2304)");
  return 0;
}
