# Unity 6.6: сборка и публикация

Проект закреплён на **6000.6.7f1**. Открывайте его этой версией Unity через Hub.
Старые сборки и пакеты Unity 2021 не смешивайте с новыми: каждый выпуск заново
собирает игрока, ядро, шесть встроенных пакетов и уровни 01–13.

## Локальная сборка

В Unity Hub установите модули нужных платформ: Android (включая SDK, NDK, OpenJDK),
Web, Linux Mono, Windows Mono. Модуль iOS нужен только для экспорта Xcode. На Mac используется редактор Apple Silicon.
Для упаковки нужен Python 3.12+, для macOS — Xcode Command Line Tools.
Закройте редактор с этим проектом перед запуском команды.

```sh
bash scripts/build.sh WebGL
python3 scripts/package-build.py WebGL
```

| Аргумент | Результат в `Builds/` | Файл для релиза |
| --- | --- | --- |
| `WebGL` | `WebGL/index.html`, Build, StreamingAssets | `Ballance-<version>-WebGL.tar.gz` |
| `macOS` | `macOS/Ballance.app`, ARM64, Mono | `Ballance-<version>-macOS.zip`, ad-hoc подпись |
| `Windows` | `Windows/Ballance.exe`, x86-64, Mono | `Ballance-<version>-Windows.zip` |
| `Linux` | `Linux/Ballance.x86_64`, x86-64, Mono | `Ballance-<version>-Linux.tar.gz` |
| `Android` | `Android/Ballance.apk`, ARM64, IL2CPP | `Ballance-<version>-Android.apk` |
| `iOS` | `iOS/Unity-iPhone.xcodeproj`, ARM64, IL2CPP | `Ballance-<version>-iOS.tar.gz` — проект Xcode |

Подставьте нужную платформу в обе команды. Архивы и SHA-256 находятся в
`Builds/releases/`. Версия берётся из Player Settings → Version. Предыдущие
выходные файлы нужно переместить перед повторной сборкой: скрипты их не затирают.
Журнал Unity: `Logs/build-<platform>.log`. Публикация iOS пока отложена;
этот target сохранён для будущего экспорта и в текущий релиз не входит.

Путь к редактору можно указать через `UNITY_EDITOR`. Пример для macOS:

```sh
UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity bash scripts/build.sh Linux
```

Из открытого редактора переключите платформу в Build Profiles, затем используйте
**Ballance → Build → Build current platform with game content**. Обычная кнопка
Build не упаковывает внешние игровые ресурсы. Для командной строки точка входа —
`Ballance2.Editor.PlayerBuild.Build`, обязательно с соответствующим `-buildTarget`.
Настройки выбранной платформы сохраняются в проекте.

Android APK предназначен для установки и проверки. Для Google Play отдельно
настройте собственный bundle identifier, version code и keystore. Для AAB вызовите
`Ballance2.Editor.PlayerBuild.Build` с `-buildTarget Android`
и `-customBuildPath Builds/Android/Ballance.aab`: расширение выбирает App Bundle,
все игровые ресурсы включаются тем же способом.
Не публикуйте debug-подпись как магазинный релиз. iOS экспортирует проект:
подпись, provisioning, Archive и загрузка в TestFlight/App Store выполняются в
Xcode с вашим Apple Developer account. macOS ZIP подписан ad-hoc; для
notarization нужен ваш Developer ID. Автоматического отправления в магазины нет.

Android собирается с Input System, поскольку режим Both там не поддерживается.
`AndroidInput` сохраняет существующие KeyCode-настройки, чувствительность мыши
и сенсорные контролы; после сборки настройка редактора восстанавливается.

## GitHub Pages

Web использует gzip с **Decompression Fallback**, однопоточную WebAssembly
физику и относительные URL. Специальные заголовки Content-Encoding, COOP/COEP
не нужны. Сайт работает в подпапке репозитория, например `/ballance/`.
Исходники не содержат сгенерированную игру: Web-архив хранится в GitHub Release.

1. Один раз включите **Settings → Pages → Source → GitHub Actions**.
   В **Settings → Environments → github-pages → Deployment branches and tags**
   разрешите ветку `main` и теги `v*`: публикация релиза запускается от тега.
   В `iwonz/ballance` оба правила уже настроены.
2. Закоммитьте исходники и workflows, соберите и проверьте Web из этого коммита.
3. Создайте draft release на его теге; приложите
   `Ballance-<version>-WebGL.tar.gz` и соответствующий `.sha256`.
4. Опубликуйте релиз. Workflow **Publish Web to GitHub Pages** проверит сумму,
   структуру архива, наличие уровней, затем развернёт игру.

Для уже опубликованного релиза запустите этот workflow вручную и укажите тег.
Это также позволяет вернуть предыдущую Web-версию. Релиз без Web-архива не
меняет сайт. После первого успешного развёртывания адрес этого репозитория:
`https://iwonz.github.io/ballance/` (если в Pages не задан собственный домен).
Лицензия Unity и Unity-секреты GitHub для этого workflow не нужны.

Перед публикацией проверьте игру через HTTP, включая загрузку уровня и движение
шара; открытие `index.html` через `file://` не подходит:

```sh
python3 -m http.server 8000 --directory Builds
# http://localhost:8000/WebGL/
```

Архив для Pages ограничен 1 GB распакованных файлов. Все игровые пакеты
включаются в `StreamingAssets/BuiltInPackages`; HTTP-запросы идут к тому же
хосту и сохраняют подпапку сайта. Встроенные моды скомпилированы в игрок.
Загрузка новых C# DLL работает только в Mono desktop-сборках; Web/iOS/Android
IL2CPP требуют включить код мода в исходники и пересобрать игру.

## Сборки в GitHub Actions

Workflow **Unity 6 platform builds** запускается вручную для выбранной платформы
и выдаёт готовый release asset с checksum. Нужен ваш **self-hosted Apple Silicon
runner** с метками `macOS`, `ARM64`, `unity-6000.6.7f1`, активированной Unity Hub
и установленными модулями. Скрипт использует стандартный путь Hub.
Используйте актуальный Actions runner с поддержкой Node 24 (не ниже 2.327.1).
Runner не настраивается автоматически. Не используйте его для недоверенных PR.

Публикация готовых файлов не требует self-hosted runner: достаточно локальной
сборки. Workflow **Build checks** на hosted runner проверяет локализацию,
упаковку и нативную macOS-физику на push/PR. Он не выдаёт эти проверки за
тестирование всей игры или всех устройств.

## Физика Web и проверки

Web-плагин включён в репозиторий. Для воспроизводимой пересборки:

```sh
python3 scripts/build-physics.py WebGL
python3 scripts/build-physics.py Android
python3 scripts/test-release-tools.py
python3 scripts/check-physics-abi.py
python3 scripts/check-localization.py
```

Скрипт физики скачивает закреплённый коммит IVP, применяет
`BallancePhysics/source-physics.patch`, компилирует библиотеку Emscripten из
Unity 6000.6.7f1, запускает Wasm smoke test и только затем обновляет плагин.
Android-библиотека собирается NDK из той же Unity с выравниванием LOAD-сегментов
16 KB; скрипт проверяет его через llvm-readelf. Для другой установки путь к
Emscripten или NDK задаётся `--sdk`. Для Android нужен macOS/Linux host.
Патч добавляет callback удаления constraint, необходимый существующей обёртке,
и защиту повторного включения заголовка. Диагностика публичного IVP идёт в stdout.

`Ballance2.Editor.BuildChecks.Run` через `-executeMethod` проверяет доступность
шести модулей и преобразование HTTP/jar/file URL.
`InputChecks.Run` внутри BuildChecks проверяет события клавиатуры, мыши и touch
на виртуальных устройствах Input System. Локализацию проверяет
`Ballance2.Editor.LocalizationChecks.Run`. Smoke test нативной библиотеки
проверяет ABI 2302, выделение векторов, столкновение шара со статическим телом,
создание/удаление шарнира и завершение. Проверка ABI сравнивает 89 сигнатур C# и C++.
Эти проверки не заменяют прохождение уровней на целевых устройствах.

## Проверка выпуска 1.0.9 (25 октября 2026)

Все пять игроков собраны локально Unity 6000.6.7f1 вместе с 13 уровнями.

| Платформа | Выполнено |
| --- | --- |
| Web | Проверка и распаковка release-архива, запуск в Chrome по `/ballance/`, русское меню, первый уровень, управление, падение, возрождение шара и возврат в меню; Web Audio запущен |
| macOS ARM64 | Запуск на M3, загрузка ядра и пакетов, тест столкновения/шарнира, ad-hoc подпись и проверка распакованного ZIP |
| Android ARM64 | APK Signature Scheme v2, zipalign 16 KB и LOAD-сегменты всех шести `.so`, 20 игровых файлов, виртуальные события ввода |
| Windows / Linux x86-64 | Сборка и архивы с 13 уровнями; проверены CRC Windows ZIP и исполняемые права Linux |

Также прошли проверки 89 сигнатур ABI, 294 переводов и 205 подписей prefab,
пять тестов упаковки Pages и actionlint. Реальные Android-устройства,
Windows/Linux gameplay и полное прохождение уровней не проверялись.
iOS в этот выпуск не входит. В Web остаются диагностические сообщения Unity
о метаданных аудио и недоступной телеметрии в тестовой сети; ошибок Wasm
и отсутствующего Projector при проверке уровня нет.

Документация: [Unity 6000.6.7f1](https://unity.com/releases/editor/whats-new/6000.6.7f1),
[развёртывание Unity Web](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-deploying.html),
[GitHub Pages workflows](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages),
[лимиты Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits).
