# 点名分组工具（NameSelector）

## 项目概述

WPF 桌面点名程序（.NET Framework 3.5，C# 5），用于课堂随机点名与名单管理。名单数据持久化到 exe 同目录下的 `namelist.json`（UTF-8）。

当前为 Demo 阶段：程序启动先进临时功能入口窗口，分流「点名分组工具」与「周次情况记录表」两个功能。

## 技术要点

- 单实例运行：通过命名 `Mutex` 防止多开互相覆盖数据。
- 数据服务与界面解耦：`DataService` 负责 `namelist.json` 读写与数据归一化，界面层只负责展示与交互。
- 比例式自适应：`Converters/Scale.cs` 依据设计基准尺寸对窗口内容按比例缩放。
- 统一对话框：所有提示 / 确认均通过 `Dialogs/DialogService` 弹出 `Dialogs/NoticeDialog`，大字号大按钮、按类型着色（信息蓝 / 成功绿 / 警告橙 / 错误红），适合教室白板。
- 名单卡片支持点击切换点名状态，卡片带 1 秒防抖，避免双击误操作。
- 随机选人按“全部 / 已点 / 未点”三个池子抽取，结果以独立窗口展示。
- 周次情况记录表：按当前名单生成某一周次的表，同一周次只能有一张；每张表独立保存为一个 JSON 文件（`weekly\日期跨度_周次.json`，如 `2026-08-03_2026-08-09_week25.json`，不使用中文命名）。
- 周次表按小组分组展示：小组行 + 组长 / 组员行；列头为背默（2 列）、课堂（5 列）、作业（3 列）、总结（1 列），每个交叉单元格为可编辑字符串。
- 开学日期所在周为第 1 周；开学日期记录在 `namelist.json` 的 `SemesterStart`，首次生成周次表时要求输入。

## 核心数据模型（AppData）

- `Students`：学生列表，每名学生含 `Id`、`Name`、`IsCalled`（是否已点）、`Order`（点名次序，未点为 0）。
- 学生含 `Group`（小组名，未分组为「未分组」）与 `IsLeader`（是否组长）；名单编辑时使用 `组名:姓名` 分组，组内第一个人自动为组长。
- `NextOrder`：下一个点名次序，初始为 1；“结束本轮点名”或“修改名单”保存后重置为 1。
- `SemesterStart`：开学日期（yyyy-MM-dd），周次表按此计算周次。
- `WeeklyRecordTable` / `WeeklyRecordRow` / `WeeklyColumns`：一张周次表的模型，行内 `Cells` 字典按列 Key（Back1、Class1、Homework1、Summary 等）存字符串。

## 主要功能

- 点名卡片：点击切换“未点 / 已点”，已点卡片分配当前次序并推进 `NextOrder`。
- 结束本轮点名：确认后清除全部点名记录，`NextOrder` 重置为 1。
- 随机选人：从指定池子随机抽取一人并弹出结果。
- 修改名单：全量重写名单，重新编号并重置点名状态。
- 启动未结束检测：启动时若 `NextOrder != 1`，说明上一轮点名尚未结束，弹出“上次点名没有结束”对话框；选择“开启新的点名”执行结束本轮点名逻辑并重置，选择“维持当前状态”不做处理。
- 临时入口：启动进入 `EntryWindow`，可选择打开点名窗口或周次记录表。
- 生成周次表：输入开学日期与周次（默认本机日期所在周），同一周次已存在时拒绝重复生成；生成后写入独立 JSON。
- 周次表编辑：下拉框展示已读取到的周次表，切换时若有未保存修改会询问；每个单元格编辑后需点击「保存」落盘，关闭窗口时也会兜底询问。

## 目录结构

- `Models/`：`AppData`、`Student`、`WeeklyRecord`（周次表模型与列定义）
- `Services/`：`DataService`（名单读写与归一化）、`WeeklyRecordService`（周次表独立 JSON 读写与生成）
- `Converters/`：`Scale`、`StatusTextConverter`、`OrderOnlyConverter`
- `Dialogs/`：`NoticeDialog`（通用提示 / 确认对话框）、`InputDialog`（输入对话框）、`DialogService`（统一对话框入口）
- 窗口：`EntryWindow`（临时功能入口）、`MainWindow`（点名主界面）、`EditListDialog`（修改名单）、`ResultWindow`（选人结果）、`WeekRecordWindow`（周次情况记录表）

## 提交记录

- 最近提交：新增临时入口窗口，分流点名与周次记录表；新增周次情况记录表 Demo（小组分组表格、独立 JSON 存取、开学日期 / 周次生成）。
