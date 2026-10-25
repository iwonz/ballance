// pch.h: precompiled header.
// The files listed below compile once to speed up subsequent builds.
// This also affects IntelliSense, including completion and code navigation.
// If any included file changes between builds, all of them are recompiled.
// Avoid frequently changing files here, as they defeat the performance benefit.

#ifndef PCH_H
#define PCH_H

// Add headers to precompile here.
#include "framework.h"

#ifdef VIRTOOLSNMOLOADER_EXPORTS
#include "CKAll.h"
#endif

#define API_EXPORT __declspec(dllexport)

#endif //PCH_H
