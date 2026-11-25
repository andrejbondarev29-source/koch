# Дворовой файтинг — C# / MonoGame

Полная переработка исходной одностраничной HTML-игры (`Dvor_Fight (2).html`) на C#
с чистой объектно-ориентированной архитектурой и MonoGame (`DesktopGL`) на .NET 8.

Оригинал был одним файлом на 2,9 МБ с минифицированным JavaScript. Здесь — обычный
C#-проект: слои, классы, наследование, интерфейсы, никакой «магии» в одной строке.

---

## Что реализовано

- **Два бойца:** Андрей (`ShoesThrow`, `Metamorphosis`) и Пантера (`EarBeam`, `Summon`).
- **Бой:** шаг, прыжок, присед, удары (джеб/кик), тяжёлые удары в форме, разведение тел,
  гравитация, отбрасывание.
- **Спецухи `Z`/`X`:** ботинки, ушной луч, призыв танцовщицы, превращение с cutscene.
- **AI-бот:** таймер решений, сближение, атаки, спецухи; умеет приседать под луч
  (в оригинале не умел — фикс из ревью).
- **Эффекты:** hit-stop, тряска экрана, искры, вспышка при попадании, HP-бары, HUD кулдаунов.
- **Процедурный звук:** осцилляторы (синус/треугольник/пила) + шум с частотным свипом и
  огибающей. Ни одного аудиофайла — как и в оригинале на WebAudio.
- **Кат-сцена метаморфозы:** исходный MP4 раскадрован в 60 PNG и проигрывается в игре.
- **Меню и экран результата** с кириллическим шрифтом (FontStashSharp, без Content Pipeline).

## Что исправлено по сравнению с оригиналом (см. код-ревью)

| Проблема оригинала | Решение здесь |
|---|---|
| Бот запускал кат-сцену превращения и замораживал игру | cutscene запускается только игроком; бот превращается мгновенно |
| Софтлок, если видео не запустилось | кат-сцена — таймер + кадры, софтлок невозможен; есть фолбэк на вспышку |
| Бот не умел приседать → луч неотразим | `BotController` приседает при замахе луча; луч ограничен дистанцией |
| ~151 `fillRect` на кадр (фон каждый кадр) | статичный фон запекается в одну текстуру один раз |
| Магические числа по всему коду | все числа в `Config/Balance.cs` |

## Требования

- **.NET 8 SDK** (в этой среде установлен в `%LOCALAPPDATA%\Microsoft\dotnet`).
- Windows x64 (MonoGame DesktopGL, OpenGL).

Если `dotnet` не в PATH, используйте `build.cmd` / `run.cmd` — они выставляют `DOTNET_ROOT` сами.

## Сборка и запуск

```cmd
build.cmd                :: сборка Release
run.cmd                  :: запуск игры
run.cmd --selftest       :: headless-самотест геймплея (без окна)
```

Или вручную:

```powershell
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
& "$env:DOTNET_ROOT\dotnet.exe" build DvorFight.csproj -c Release
& ".\bin\Release\net8.0\DvorFight.exe"
```

## Управление

| Действие | Клавиши |
|---|---|
| Меню: выбрать бойца | `1` / `2`, клик по левой/правой половине |
| Шаг | `A`/`D` или `←`/`→` |
| Прыжок | `W` / `↑` / `Space` |
| Присед | `S` / `↓` |
| Удар | ЛКМ |
| Спецуха Z | `Z` |
| Спецуха X | `X` |
| Реванш / меню | `R` / `Esc` |

## Архитектура (ООП)

```
DvorFight/
├── Program.cs              точка входа (+ --selftest)
├── Game1.cs                MonoGame-оболочка: окно, цикл, экраны меню/бой/результат
├── Core/
│   ├── Entity.cs           абстрактная база: X, Y, Draw()
│   └── Enums.cs            FighterKind, Face, MoveType, SoundType, Command, GameScreen
├── Config/
│   └── Balance.cs          ВСЕ игровые числа (баланс 1:1 из оригинала)
├── Domain/
│   ├── Fighter.cs          база бойца: физика, кулдауны, state-машина атаки, спрайты
│   ├── Andrey.cs           : Fighter — ноги + метаморфоза
│   ├── Pantera.cs          : Fighter — луч + призыв
│   ├── Projectile.cs       абстрактный снаряд
│   ├── Shoe.cs             : Projectile
│   ├── Dancer.cs           : Projectile — танцовщица по дуге
│   └── Spark.cs            эффект искр
├── Combat/
│   └── FightSession.cs     бой: бойцы, снаряды, hit-stop, тряска, cutscene, KO
├── AI/
│   └── BotController.cs    поведение бота
├── Input/
│   └── InputController.cs  клавиатура/мышь → held + edge-команды
├── Audio/
│   ├── IAudioEngine.cs     интерфейс (DIP)
│   ├── AudioEngine.cs      процедурная генерация звука
│   └── NullAudioEngine.cs  заглушка для тестов
├── Rendering/
│   ├── AssetStore.cs       загрузка PNG-анимаций в рантайме
│   ├── SpriteAnimation.cs  кадры анимации
│   ├── Renderer.cs         блит спрайтов, прямоугольники, линии
│   ├── Background.cs       запечённый фон (один draw вместо сотен)
│   ├── MorphCutscene.cs    кат-сцена превращения
│   ├── Hud.cs              HP-бары, имена, кулдауны
│   └── UI.cs               текст с выравниванием
└── Diagnostics/
    └── SelfTest.cs         headless-проверка геймплея
```

**Принципы:**
- доменный слой (`Domain`, `Combat`, `AI`, `Config`) не знает про MonoGame-рендер и ввод —
  только `Update`-логику;
- наследование и полиморфизм: `Entity` → `Fighter` → `Andrey`/`Pantera`,
  `Entity` → `Projectile` → `Shoe`/`Dancer`;
- абстрактные члены: `Entity.Draw`, `Fighter.SpritesFaceRight`, `Fighter.OnSpecialX`,
  `Projectile.Update`;
- инверсия зависимостей: `FightSession` работает через `IAudioEngine`;
- никаких `null`-проверок на каждом шагу — состояние инкапсулировано.

## Ассеты

Исходные спрайты были base64-WebP внутри HTML. Скрипты извлечения:

```
tools/extract_assets.py   → Assets/Sprites (84 PNG, 18 анимаций) + Assets/morph.mp4
tools/extract_morph.py    → Assets/MorphFrames (60 PNG кат-сцены, PyAV)
```

Далее ассеты копируются в `DvorFight/Content/Sprites` и `DvorFight/Content/MorphFrames`
и вкомпилируются в выходную папку. Content Pipeline (MGCB) **не используется** —
текстуры грузятся в рантайме через `Texture2D.FromStream`.

## Самотест

`run.cmd --selftest` прогоняет настоящий боевой код без окна и звуковой карты
(2 сценария × 40 секунд симуляции) и проверяет: игрок наносит урон, бот наносит урон
(AI жив), спецуха создаёт снаряд/призыв, метаморфоза срабатывает. Возвращает
`RESULT: PASS` и код 0.

Пример вывода:

```
=== DvorFight headless self-test ===
[Андрей vs бот] frames=487 (8.1 с) playing=False winner=игрок
          playerHp=47 botHp=0 peakProjectiles=3 botDmg=100 playerDmg=53 morphed=True
[Пантера vs бот] frames=655 (10.9 с) playing=False winner=бот
          playerHp=0 botHp=7 peakProjectiles=3 botDmg=93 playerDmg=100 morphed=False
RESULT: PASS
```
