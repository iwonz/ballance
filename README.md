# <center>Ballance Unity Rebuild</center>

<p align="center">
  <a href="README.CN.md">简体中文</a> | English
</p>
<p align="center">
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/unity-6000.6.7f1-blue?style=flat-square" alt="Unity Version" />
  </a>
  <a style="text-decoration:none" href="https://github.com/iwonz/ballance/releases">
    <img src="https://img.shields.io/github/v/release/iwonz/ballance.svg?label=alpha&style=flat-square&color=yellow" alt="Releases" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/platform-Win%20%7C%20Linux%20%7C%20Mac%20%7C%20Android%20%7C%20Web-orange?style=flat-square" alt="Platform" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/license-GPL--3.0-green?style=flat-square" alt="License" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/github/repo-size/imengyu/ballance?style=flat-square" alt="Size" />
  </a>
</p>

---

![image](/Assets/Game/Common/Textures/UI/LogoPicture.jpg)

## Introduction

This is an open source unity remake of ballance game

**Note: Ballance belongs to Cyparade's work, and the copyright belongs to the original developer Cyparade. The development of this project is for learning purposes only and cannot be used for any commercial purposes. This project follows the GPL-3.0 protocol, but is limited to the code of this project. Any 3D model data, physical engine, images, audio and video, and game data related to Ballance are not within this scope. Any use of these game data for commercial activities without the permission of the copyright owner is illegal.**

The project has completed the features of the original version:

* Original game content
* Level 1-13 game content
* Physical effect similarity 85%

Compared with the original version, this project has added the following features:

* **Load NMO files directly** (Windows version only)
* Android version, Mac version (You can also try to compile other platforms)
* Self made map interface
* Modul interface (Use C# to develop mod or custom modules)
* Level previewer
* Mod manager

## Play and download

[Play in your browser](https://iwonz.github.io/ballance/) or download a build from
[this fork's Releases](https://github.com/iwonz/ballance/releases).
Every build includes the original 13 levels and Russian, English and Chinese UI.

| Platform | Release file | Installation / requirements |
| --- | --- | --- |
| Web | `Ballance-<version>-WebGL.tar.gz` | Play on GitHub Pages; current browser with WebGL 2 and WebAssembly |
| Windows | `Ballance-<version>-Windows.zip` | Extract and run `Ballance.exe`; x86-64, Windows 10 21H1+ |
| Linux | `Ballance-<version>-Linux.tar.gz` | Extract and run `./Ballance.x86_64`; x86-64, Ubuntu 22.04/24.04 |
| macOS | `Ballance-<version>-macOS.zip` | Extract `Ballance.app`; Apple Silicon, macOS 13.1+ |
| Android | `Ballance-<version>-Android.apk` | Install APK; ARM64, Android 8.0+; test signing, not a Play Store package |

See [Unity 6.6 player requirements](https://docs.unity3d.com/6000.6/Documentation/Manual/system-requirements.html#player)
for graphics and driver requirements, and [macOS signing and first-launch notes](docs/macos-apple-silicon.md).
The macOS build is ad-hoc signed, without Apple notarization. iOS publication is deferred;
the build script retains an Xcode export target.
Historical Intel macOS and Unity 2021 builds are available in the
[upstream releases](https://github.com/imengyu/Ballance/releases).

## Keys and gameplay

* The PC version operation remains unchanged from the original game. You can modify the key mapping in the Settings>Control menu.
* The mobile version has added a touch keyboard, and you can modify the keyboard style in the Settings>Control menu.

## Turn on debugging mode

In debugging mode, the ball can fly and you can use it to cheat or test levels.

1. Go to the about menu, click the version number several times until the prompt pops up.
2. Then restart the game, you enter the debugging mode.
3. You can press F12 to open the console.

In the debugging mode, you can press the Q key to raise the ball and the e key to lower the ball.

Enter the `quit dev` command on the console to turn off the debugging mode.

### Open all original levels

After entering the debugging mode, you can enter `highscore open-all` command in the console to open all levels.

## Directly load NMO file [new]

Ballance Unity Rebuild Version 0.9.8 supports the function of loading the original level file of ballance.

You can load a standard original ballance NMO level by clicking start > NMO level.

The core uses the Virtools SDK 5.0 to process NMO files, so only the windows 32-bit version is supported.

Most levels can be loaded successfully and played, but there are a few restrictions:

* Cannot load level with Virtools script.
* Point and line mesh of Virtools are not supported.
* The material does not support the special effect of Virtools. The default material will be used instead.

### How to run project source code

You need:

* Install Unity 6.6 (6000.6.7f1) and the build modules for your target platforms
* Install a code editor (VScode or Visual Studio)
* Clone or download this project `https://github.com/iwonz/ballance` to your computer.

Steps:

1. Open the project with unity.
2. Open `Assets/Scenes/Main.unity` scene.
3. Select the `GameEntry` object, set `Debug Type` to `NoDebug` in the inspector.
4. Click Run and you can see the game.

## Generate game programs from project source code

Please refer [production document](wiki/production.md)。

For Unity 6.6 builds for Windows, Linux, macOS, Android, iOS and Web, and
publication of Web releases to GitHub Pages, see [build and release instructions](docs/build-and-release.md).

## Physics Engine

The C++source code for the physics engine can be found [here](https://github.com/nillerusr/source-physics) (this is not the author's own repo).

The wrapper DLL code for the physics engine is located in the BallancePhysics directory below the project, and you need to compile it using Visual Studio 2919 or higher.

## Project progress and roadmap

The original gameplay and level reproduction have all been completed, and you can fully experience the game from start to finish. You can also refer to the development interface to create custom levels. The author will continue to develop based on everyone's feedback in the next step, adding more features, such as fun moduls, level editors, and so on.

## Contribute

If you have any good modifications or problem solvers, welcome PR. If you have any good ideas or comments, or if you find a bug, please feel free to submit an issue.

## Why did i do this?

Ballance Unity is a small dream of the me, I hoping to enable Ballance to run on mobile phones and easier make the levels and mods (the latter has been [BallanceModLoader](https://github.com/Gamepiaynmo/BallanceModLoader) Implemented). Also, Ballance Unity was my first project to learn about Unity game development, which greatly helped me improve my skills.

## Game album

Original levels
![Demo](./DemoImages/11.jpg)
![Demo](./DemoImages/12.jpg)
![Demo](./DemoImages/13.jpg)
![Demo](./DemoImages/14.jpg)
![Demo](./DemoImages/18.jpg)
![Demo](./DemoImages/9.jpg)
![Demo](./DemoImages/6.jpg)
![Demo](./DemoImages/7.jpg)
![Demo](./DemoImages/15.jpg)
![Demo](./DemoImages/16.jpg)
![Demo](./DemoImages/17.jpg)

Level 13

![Demo](./DemoImages/9.gif)
![Demo](./DemoImages/10.png)

Self made level (魔脓空间站)

![Demo](./DemoImages/3.jpg)
![Demo](./DemoImages/4.jpg)
![Demo](./DemoImages/5.jpg)

Level previewer

![Demo](./DemoImages/1.jpg)
![Demo](./DemoImages/2.jpg)

## Open source projects used in the project

* https://github.com/chrisnolet/QuickOutline
* https://github.com/seedov/AsyncAwaitUtil/
* https://github.com/Tayx94/graphy
* https://github.com/yasirkula/UnityRuntimeSceneGizmo
* https://github.com/yasirkula/UnityRuntimeInspector
* https://github.com/yasirkula/UnitySimpleFileBrowser
