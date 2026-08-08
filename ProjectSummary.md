# 点名分组工具（NameSelector）

## 项目概述

WPF 桌面点名程序（.NET Framework 3.5，C# 5），用于课堂随机点名与名单管理。名单数据持久化到 exe 同目录下的 `namelist.json`（UTF-8）。

## 技术要点

- 单实例运行：通过命名 `Mutex` 防止多开互相覆盖数据。
- 数据服务与界面解耦：`DataService` 负责 `namelist.json` 读写与数据归一化，界面层只负责展示与交互。
- 比例式自适应：`Converters/Scale.cs` 依据设计基准尺寸对窗口内容按比例缩放。
- 名单卡片支持点击切换点名状态，卡片带 1 秒防抖，避免双击误操作。
- 随机选人按“全部 / 已点 / 未点”三个池子抽取，结果以独立窗口展示。

## 核心数据模型（AppData）

- `Students`：学生列表，每名学生含 `Id`、`Name`、`IsCalled`（是否已点）、`Order`（点名次序，未点为 0）。
- `NextOrder`：下一个点名次序，初始为 1；“结束本轮点名”或“修改名单”保存后重置为 1。

## 主要功能

- 点名卡片：点击切换“未点 / 已点”，已点卡片分配当前次序并推进 `NextOrder`。
- 结束本轮点名：确认后清除全部点名记录，`NextOrder` 重置为 1。
- 随机选人：从指定池子随机抽取一人并弹出结果。
- 修改名单：全量重写名单，重新编号并重置点名状态。
- 启动未结束检测：启动时若 `NextOrder != 1`，说明上一轮点名尚未结束，弹出“上次点名没有结束”对话框；选择“开启新的点名”执行结束本轮点名逻辑并重置，选择“维持当前状态”不做处理。

## 目录结构

- `Models/`：`AppData`、`Student`
- `Services/`：`DataService`（数据读写与归一化）
- `Converters/`：`Scale`、`StatusTextConverter`、`OrderOnlyConverter`
- 窗口：`MainWindow`（主界面）、`EditListDialog`（修改名单）、`ResultWindow`（选人结果）、`UnfinishedRollCallDialog`（启动未结束确认）

## 提交记录

- 最近提交：启动时检测“当前点名顺序”是否重置，未结束时弹窗询问是否开启新一轮点名。
