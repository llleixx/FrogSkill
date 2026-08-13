# FrogSkill

[English](#english) | [中文](#中文)

![FrogSkill gameplay demo](https://raw.githubusercontent.com/llleixx/FrogSkill/main/docs/media/frog-skill-demo.gif)

## English

FrogSkill gives every living Scout a frog tongue that can hook and pull other Scouts and Zombies.

### Features

- **Frog-style pulling:** Uses PEAK's current Frog pull curve, maximum lift distance, force formula, drag, and release ordering.
- **Scout and Zombie targets:** Pull living Scouts, transformed Zombie Scouts, and NPC Mushroom Zombies.
- **Miss animation:** A missed shot briefly extends and retracts along the aimed direction without applying force.
- **Multiplayer visuals:** Firing, pulling, releasing, and tongue visuals are synchronized between modded clients.

### Installation

Install with Thunderstore Mod Manager or r2modman. Manual installation requires BepInEx 5 and ModConfig; place `FrogSkill.dll` in `PEAK/BepInEx/plugins/FrogSkill/`.

All players in a multiplayer lobby must install the same FrogSkill version. FrogSkill adds custom Photon RPC receivers and is not compatible with vanilla clients.

### Controls

Aim the center crosshair directly at a living Scout or supported Zombie and press `G` to fire. Press `G` again while pulling to release early. The activation key can be rebound from ModConfig's modded controls page.

If the crosshair does not hit a valid target, the tongue performs a short miss animation. Its maximum visual length is 35% of the configured target range and it stops early on physical obstructions.

### Configuration

The config file is generated at `BepInEx/config/com.github.lllei.FrogSkill.cfg`.

| Key | Default | Allowed range |
|---|---:|---:|
| `General.Enabled` | `true` | boolean |
| `Controls.ActivationKey` | `G` | keyboard key |
| `Tongue.MaxDistance` | `40` | `5` to `60` |
| `Tongue.PullForce` | `450` | `0` to `2000` |
| `Tongue.LiftForce` | `30` | `0` to `500` |
| `Tongue.MaxHookDuration` | `1` | `0.5` to `10` seconds |
| `Tongue.StopDistance` | `5` | `1` to `10` |
| `Tongue.ExtraDragOther` | `0.95` | `0` to `1` |
| `Tongue.ExtraDragLetGo` | `0.1` | `0` to `1` |
| `Tongue.Cooldown` | `0.5` | `0` to `60` seconds |

Frogs and other non-`Character` mobs are not currently supported targets. Dead or fully passed-out Scouts cannot fire or be targeted.

### Building

Copy `PeakGameDir.props.example` to the gitignored `PeakGameDir.props` and set the PEAK directory, or set `PEAK_GAME_DIR`.

```powershell
dotnet build FrogSkill.sln -c Release
dotnet msbuild src\FrogSkill\FrogSkill.csproj -t:Deploy -p:Configuration=Release
dotnet msbuild src\FrogSkill\FrogSkill.csproj -t:PackageThunderstore -p:Configuration=Release
```

Normal builds do not deploy to PEAK. `Deploy` explicitly copies the DLL into the configured game installation. Packaging writes `artifacts/lllei-FrogSkill-<version>.zip` and validates its metadata, DLL version, icon, and contents.

For a release, update `Version` in `FrogSkill.csproj`, `version_number` in `manifest.json`, and the dated section in `CHANGELOG.md`.

## 中文

FrogSkill 为每个存活的 Scout 添加青蛙舌头，可以抓住并拖拽其他 Scout 和 Zombie。

### 功能

- **青蛙式拉取：** 使用当前 PEAK 青蛙的距离曲线、最大升力距离、受力公式、阻力与释放顺序。
- **支持 Scout 和 Zombie：** 可以拉取存活 Scout、变身后的 Zombie Scout 和 NPC Mushroom Zombie。
- **空吐动画：** 未命中有效目标时，舌头会沿瞄准方向短暂伸出并收回，但不会施加拉力。
- **多人同步：** 安装 Mod 的客户端之间会同步发射、拉取、释放和舌头视觉。

### 安装与多人要求

推荐使用 Thunderstore Mod Manager 或 r2modman 安装。手动安装需要 BepInEx 5 和 ModConfig，并将 `FrogSkill.dll` 放入 `PEAK/BepInEx/plugins/FrogSkill/`。

多人房间内所有玩家都必须安装同一版本的 FrogSkill。该 Mod 添加了自定义 Photon RPC 接收器，不兼容未安装 Mod 的原版客户端。房主身份不会代替其他玩家运行 Mod。

### 操作

用屏幕中心准心直接瞄准存活 Scout 或受支持的 Zombie，按 `G` 发射；拉取期间再次按 `G` 可以提前释放。按键可在 ModConfig 的 Mod 控制页面中重新绑定。

未命中有效目标时会播放短促的空吐动画。空吐的最大视觉长度为配置射程的 35%，遇到物理障碍时会提前停止。

配置文件位于 `BepInEx/config/com.github.lllei.FrogSkill.cfg`，所有配置键、默认值和范围见上方英文表格。

目前不能抓取青蛙或其他非 `Character` 生物。死亡或完全昏迷的 Scout 无法发射，也不能成为目标。

### 构建与发布

将 `PeakGameDir.props.example` 复制为已被忽略的 `PeakGameDir.props` 并填写 PEAK 路径，或者设置 `PEAK_GAME_DIR`。普通构建不会写入游戏目录；只有显式 `Deploy` 才会部署。发布 ZIP 的生成命令及版本要求见上方英文构建章节。
