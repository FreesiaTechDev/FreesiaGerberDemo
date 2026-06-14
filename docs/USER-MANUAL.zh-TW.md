# FreesiaGerberLib 使用手冊

## 目錄

- [概述](#概述)
- [系統需求](#系統需求)
- [支援格式](#支援格式)
- [核心入口](#核心入口)
  - [Gerber.TryParse(...)](#gerbertryparse)
  - [IGerber](#igerber)
  - [IStep](#istep)
  - [ILayer](#ilayer)
- [繪圖](#繪圖)
  - [`ILayer.Render<T>(...)`](#ilayerrendert)
  - [GraphicOption](#graphicoption)
  - [VisualOption](#visualoption)
  - [RenderOption](#renderoption)
  - [Pixel Struct](#pixel-struct)
- [視覺樹](#視覺樹)
  - [ILayer.GetVisualTree(...)](#ilayergetvisualtree)
  - [VisualRoot](#visualroot)
  - [VisualLayer](#visuallayer)
  - [VisualObject](#visualobject)
  - [VisualObjectType](#visualobjecttype)
  - [VisualPen](#visualpen)
  - [VisualObjectData](#visualobjectdata)
  - [VisualRegionContext](#visualregioncontext)
  - [VisualRegionDescription](#visualregiondescription)
- [授權](#授權)
  - [License.IsValid](#licenseisvalid)
  - [License.Features](#licensefeatures)
  - [License.HasFeature(...)](#licensehasfeature)
  - [GerberLicenseFeature](#gerberlicensefeature)
- [回傳碼與錯誤處理](#回傳碼與錯誤處理)
- [整合指引](#整合指引)
- [注意事項](#注意事項)

## 概述

`FreesiaGerberLib.dll` 是用於讀取 Gerber 資料、渲染圖層影像，以及取得 visual tree 的商業類別庫。

本文件說明 `FreesiaGerberLib.dll` 的主要 public API 與標準使用方式。內容依照類別庫的主要能力分為核心入口、繪圖、視覺樹與授權。

主要 API：

- `Gerber.TryParse(...)`：載入 RS274X file、ODB++ folder，或包含多個 RS274X file 的 folder。
- `IGerber`、`IStep`、`ILayer`：讀取載入後的 Gerber 資料。
- `ILayer.Render<T>(...)`：將 layer 渲染為 raster image。
- `ILayer.GetVisualTree(...)`：取得 layer 的 visual tree。
- `License`：檢查 hardware dongle 與授權功能。

## 系統需求

- `FreesiaGerberLib.dll` target framework 為 `netstandard2.0`。
- 使用 `FreesiaGerberLib.dll` 需要有效的 hardware dongle。
- Demo application 目前使用 WPF 與 .NET Framework 4.6.2。

## 支援格式

`FreesiaGerberLib.dll` 支援下列 Gerber 資料格式：

| 格式 | 輸入方式 |
| --- | --- |
| RS274X | File path 或 folder path |
| ODB++ | Folder path |

當輸入路徑為 folder path 時，library 會先嘗試以 ODB++ 載入；若不是有效的 ODB++ folder，會再嘗試以多個 RS274X file 載入。多個 RS274X file 載入成功時，library 會建立一個 `ORG` Step，並將每個 RS274X file 作為該 Step 中的一個 Layer。

載入成功後，可透過 `IGerber.Type` 判斷實際資料格式。

```csharp
FreesiaCode Code = Gerber.TryParse(DataPath, out IGerber GerberInfo);

if (Code != FreesiaCode.Success)
    return;

switch (GerberInfo.Type)
{
    case GerberTypes.RS274X:
        {
            break;
        }
    case GerberTypes.ODB:
        {
            break;
        }
}
```

## 核心入口

核心入口章節說明資料載入與載入後資料物件。一般使用流程為：

1. 使用 `Gerber.TryParse(...)` 載入資料。
2. 從 `IGerber` 取得 `IStep`。
3. 從 `IStep` 取得 `ILayer`。
4. 使用 `ILayer` 進行繪圖或取得 visual tree。

### Gerber.TryParse(...)

#### 函數名稱

```csharp
public static FreesiaCode TryParse(string FilePath, out IGerber GerberInfo)
```

#### 目的用途

`Gerber.TryParse(...)` 是 `FreesiaGerberLib.dll` 的主要資料載入入口，用於載入 RS274X file、ODB++ folder，或包含多個 RS274X file 的 folder。當輸入路徑為 folder path 時，library 會先嘗試解析為 ODB++，再嘗試解析為多個 RS274X file。

#### 參數

| 參數 | 說明 |
| --- | --- |
| `FilePath` | Gerber 資料路徑。可以是 RS274X file path、ODB++ folder path，或包含多個 RS274X file 的 folder path。 |
| `GerberInfo` | 載入成功時輸出 `IGerber`；載入失敗時為 `null`。 |

#### 回傳值

| 回傳值 | 說明 |
| --- | --- |
| `FreesiaCode.Success` | 載入成功。 |
| `FreesiaCode.Failure` | 路徑不存在、資料格式無法解析，或路徑內容不是支援的 Gerber 資料。 |
| `FreesiaCode.PermissionDenied` | Hardware dongle 存在，但未開放要求的功能。 |
| `FreesiaCode.Unauthorized` | 沒有有效的 hardware dongle。 |

#### 範例程式碼

```csharp
FreesiaCode Code = Gerber.TryParse(DataPath, out IGerber GerberInfo);

switch (Code)
{
    case FreesiaCode.Success:
        {
            IReadOnlyList<IStep> Steps = GerberInfo.Steps;
            break;
        }
    case FreesiaCode.Unauthorized:
        {
            return;
        }
    case FreesiaCode.PermissionDenied:
        {
            return;
        }
    case FreesiaCode.Failure:
        {
            return;
        }
}
```

#### 授權需求

呼叫 `Gerber.TryParse(...)` 前必須具有有效的 hardware dongle。若授權無效，函數會回傳 `FreesiaCode.Unauthorized`。

### IGerber

#### 物件定義

`IGerber` 是載入成功後的根物件，代表一份 Gerber 資料。它儲存資料格式、來源路徑，以及 step 清單。

若以 folder path 載入多個 RS274X file，`IGerber.Type` 會是 `GerberTypes.RS274X`，且 `Steps` 會包含 library 建立的 `ORG` Step。

#### 取得方式

`IGerber` 由 `Gerber.TryParse(...)` 的 `out IGerber GerberInfo` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Type` | Gerber 資料格式，例如 `GerberTypes.RS274X` 或 `GerberTypes.ODB`。 |
| `Path` | 來源資料路徑。 |
| `Steps` | Step 清單。 |
| `this[int Index]` | 依 index 取得 `IStep`。 |
| `this[string Name]` | 依 name 取得 `IStep`。 |

### IStep

#### 物件定義

`IStep` 代表 Gerber 資料中的 step。它儲存 step 名稱、單位、邊界，以及 layer 清單。

對於多個 RS274X file 組成的 folder，library 會建立名稱為 `ORG` 的 Step，並將每個可解析的 RS274X file 放入 `Layers`。

#### 取得方式

`IStep` 可由 `IGerber.Steps` 或 `IGerber` indexer 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Name` | Step 名稱。 |
| `Unit` | Step 使用的單位。 |
| `Bound` | Step 邊界。 |
| `Fields` | Step 欄位資訊。 |
| `Layers` | Layer 清單。 |
| `this[int Index]` | 依 index 取得 `ILayer`。 |
| `this[string Name]` | 依 name 取得 `ILayer`。 |
| `Parent` | 所屬的 `IGerber`。 |

### ILayer

#### 物件定義

`ILayer` 代表可操作的 Gerber layer。它儲存 layer 名稱、單位、邊界、極性與欄位資訊，也是繪圖與 visual tree 的主要操作入口。

#### 取得方式

`ILayer` 可由 `IStep.Layers` 或 `IStep` indexer 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Name` | Layer 名稱。 |
| `Unit` | Layer 使用的單位。 |
| `Bound` | Layer 邊界。 |
| `Polarity` | Layer 極性。 |
| `Fields` | Layer 欄位資訊。 |
| `Render<T>(...)` | 將 layer 渲染為 raster image。 |
| `GetVisualTree(...)` | 取得 layer 的 visual tree。 |

## 繪圖

繪圖章節說明如何透過 `ILayer.Render<T>(...)` 將 layer 渲染為 raster image。

### `ILayer.Render<T>(...)`

#### 函數名稱

```csharp
public RenderImage<T> Render<T>(GraphicOption Graphic, VisualOption Visual, RenderOption Render)
    where T : unmanaged, IPixel;

public void Render<T>(RenderImage<T> Image, GraphicOption Graphic, VisualOption Visual, RenderOption Render)
    where T : unmanaged, IPixel;
```

#### 目的用途

`ILayer.Render<T>(...)` 用於將目前 layer 渲染為 `RenderImage<T>`。第一個 overload 由 library 配置影像記憶體；第二個 overload 渲染到呼叫端提供的 `RenderImage<T>`。

#### 參數

| 參數 | 說明 |
| --- | --- |
| `Image` | 既有輸出影像。只用於第二個 overload。 |
| `Graphic` | 繪圖範圍、解析度、前景色與背景色。 |
| `Visual` | 旋轉與翻轉設定。 |
| `Render` | 渲染執行設定。 |
| `T` | Pixel struct，必須使用 library 支援的 pixel struct。 |

#### 回傳值或輸出

| Overload | 結果 |
| --- | --- |
| `Render<T>(Graphic, Visual, Render)` | 回傳新配置的 `RenderImage<T>`。 |
| `Render<T>(Image, Graphic, Visual, Render)` | 將結果寫入既有 `RenderImage<T>`。 |

#### 範例程式碼

```csharp
Bound ROI = Layer.Bound;
double PixelPerUnit = 800d;

GraphicOption Graphic = new GraphicOption(
    ROI,
    PixelPerUnit,
    new ARGB(0, 0, 0, 0),
    new ARGB(255, 255, 0, 0));

VisualOption Visual = new VisualOption
{
    Flip = FlipMode.None,
    FlipCx = 0d,
    FlipCy = 0d,
    RotateAngle = 0d,
    RotateCx = 0d,
    RotateCy = 0d
};

RenderOption Render = new RenderOption
{
    MaxDegreeOfParallelism = 0
};

using (RenderImage<BGRA> Image = Layer.Render<BGRA>(Graphic, Visual, Render))
{
    // Use Image here.
}
```

渲染到既有 `RenderImage<T>`：

```csharp
using (RenderImage<BGRA> Image = new RenderImage<BGRA>(2000, 2000))
{
    Layer.Render(Image, Graphic, Visual, Render);
}
```

#### 授權需求

渲染功能需要對應授權：

- RS274X layer：`GerberLicenseFeature.GraphicRS274X`
- ODB++ layer：`GerberLicenseFeature.GraphicODB`

### GraphicOption

#### 物件定義

`GraphicOption` 儲存 raster output 的繪圖範圍、解析度、背景色與前景色。

#### 取得方式

`GraphicOption` 由呼叫端建立，並傳入 `ILayer.Render<T>(...)`。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `ROI` | 輸出範圍。常用 `Layer.Bound` 產生完整 layer 影像。 |
| `PixelPerUnit` | 每個 Gerber unit 對應的 pixel 數。 |
| `Background` | 背景色。 |
| `Foreground` | 前景色。 |
| `ClearCanvas` | 是否在繪圖前清除 canvas。 |

`PixelPerUnit` 會影響輸出影像尺寸：

```text
ImageWidth  = Ceiling(ROI.Width  * PixelPerUnit)
ImageHeight = Ceiling(ROI.Height * PixelPerUnit)
```

### VisualOption

#### 物件定義

`VisualOption` 儲存繪圖或 visual tree 的旋轉與翻轉設定。

#### 取得方式

`VisualOption` 由呼叫端建立，並傳入 `ILayer.Render<T>(...)` 或 `ILayer.GetVisualTree(...)`。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Flip` | 翻轉模式。 |
| `FlipCx` | 翻轉中心 X。 |
| `FlipCy` | 翻轉中心 Y。 |
| `RotateAngle` | 旋轉角度。 |
| `RotateCx` | 旋轉中心 X。 |
| `RotateCy` | 旋轉中心 Y。 |

### RenderOption

#### 物件定義

`RenderOption` 儲存渲染執行設定。

#### 取得方式

`RenderOption` 由呼叫端建立，並傳入 `ILayer.Render<T>(...)`。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `MaxDegreeOfParallelism` | 最大平行度。`0` 表示使用 library / .NET 預設平行度，由 runtime 依可用 CPU 與 ThreadPool 狀態調度；`1` 表示單執行緒渲染；大於 `1` 表示限制最大平行數。 |

使用可用 CPU 進行渲染時，請將 `MaxDegreeOfParallelism` 設為 `0`。目前實作中，小於 `0` 的值也會落到預設平行度，但這不是此 API 的正式建議用法。

### Pixel Struct

#### 物件定義

Pixel struct 決定 `RenderImage<T>` 的 pixel channel order 與 bit depth。`ILayer.Render<T>(...)` 的 `T` 必須使用 library 支援的 pixel struct。

`IPixel` 是 Freesia imaging pipeline 使用的 pixel interface。對外使用時，請依照輸出格式選擇 library 已提供的 pixel struct。

#### 支援類型

| Pixel struct | 說明 |
| --- | --- |
| `BGRA` | 32-bit BGRA，常用於 WPF `PixelFormats.Bgra32`。 |
| `ARGB` | 32-bit ARGB。 |
| `RGBA` | 32-bit RGBA。 |
| `ABGR` | 32-bit ABGR。 |
| `RGB` | 24-bit RGB，不包含 alpha channel。 |
| `BGR` | 24-bit BGR，不包含 alpha channel。 |
| `Gray8` | 8-bit grayscale。 |

#### 使用方式

- WPF `PixelFormats.Bgra32` 對應 `BGRA`。
- 需要 alpha 的 32-bit output 可依照目標 buffer channel order 選擇 `BGRA`、`ARGB`、`RGBA` 或 `ABGR`。
- 灰階輸出可使用 `Gray8`。

## 視覺樹

視覺樹章節說明如何透過 `ILayer.GetVisualTree(...)` 取得 layer 的幾何資料，並讀取 visual tree 中的物件。

### ILayer.GetVisualTree(...)

#### 函數名稱

```csharp
public VisualRoot GetVisualTree(VisualOption Visual)
```

#### 目的用途

`ILayer.GetVisualTree(...)` 用於取得目前 layer 的 visual tree。Visual tree 可用於物件查詢、範圍命中測試或自訂顯示流程。

#### 參數

| 參數 | 說明 |
| --- | --- |
| `Visual` | 旋轉與翻轉設定。可傳入 `null` 表示不套用額外 visual transform。 |

#### 回傳值

| 回傳值 | 說明 |
| --- | --- |
| `VisualRoot` | Visual tree 根節點。 |

#### 範例程式碼

```csharp
VisualRoot Root = Layer.GetVisualTree(Visual);
VisualLayer Content = Root.Content;
```

#### 授權需求

Visual tree 功能需要對應授權：

- RS274X layer：`GerberLicenseFeature.VisualTreeRS274X`
- ODB++ layer：`GerberLicenseFeature.VisualTreeODB`

### VisualRoot

#### 物件定義

`VisualRoot` 是 visual tree 的根節點，儲存 step 或 layer 經過 visual transform 後的整體資訊。

#### 取得方式

`VisualRoot` 由 `ILayer.GetVisualTree(...)` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Name` | Root 名稱。 |
| `Bound` | Root 邊界。 |
| `Angle` | 套用後的角度。 |
| `Flip` | 套用後的翻轉模式。 |
| `Content` | Root 下的主要 `VisualLayer`。 |
| `FindByPoint(...)` | 依座標查詢 visual object。 |
| `FindByBound(...)` | 依範圍查詢 visual object。 |

### VisualLayer

#### 物件定義

`VisualLayer` 是 visual tree 中的 layer 節點，儲存 layer 名稱、邊界、子 layer 與 visual objects。

#### 取得方式

`VisualLayer` 可由 `VisualRoot.Content` 或其他 `VisualLayer.Layers` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Name` | Visual layer 名稱。 |
| `Step` | 所屬 `VisualRoot`。 |
| `Bound` | Visual layer 邊界。 |
| `Layers` | 子 `VisualLayer` 清單。 |
| `Children` | 此 layer 直接包含的 `VisualObject`。 |
| `FindByPoint(...)` | 依座標查詢 visual object。 |
| `FindByBound(...)` | 依範圍查詢 visual object。 |

### VisualObject

#### 物件定義

`VisualObject` 是 visual tree 中的幾何物件，儲存圖形型別、pen、polarity、attributes 與 geometry data。

#### 取得方式

`VisualObject` 可由 `VisualLayer.Children`、`VisualRoot.FindByPoint(...)`、`VisualRoot.FindByBound(...)`、`VisualLayer.FindByPoint(...)` 或 `VisualLayer.FindByBound(...)` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Type` | Visual object 幾何型別。 |
| `Pen` | Aperture 或 stroke 的形狀來源。 |
| `Polarity` | 物件極性。 |
| `Attributes` | 物件屬性資料。 |
| `Datas` | 物件幾何資料清單。 |

### VisualObjectType

#### 物件定義

`VisualObjectType` 表示 `VisualObject` 的幾何型別，會影響 `VisualObjectData` 的資料呈現方式。

#### 取得方式

`VisualObjectType` 由 `VisualObject.Type` 取得。

#### 類型說明

| Type | 說明 | 主要資料 |
| --- | --- | --- |
| `Aperture` | 單一 aperture / flash。 | `Points = { Cx, Cy }`，表示中心點座標。 |
| `Line` | 線段。 | `Points = { Sx, Sy, Ex, Ey }`，表示起點與終點。 |
| `Arc` | 弧線。 | `Points = { Sx, Sy, Ex, Ey, Cx, Cy }`，並搭配 `ArcRadius` 與 `ArcIsClockwise`。 |
| `Region` | 填滿區域。 | 主要使用 `RegionContext` 描述輪廓資料。 |

### VisualPen

#### 物件定義

`VisualPen` 描述 aperture 或 stroke 的形狀來源。`Aperture`、`Line`、`Arc` 通常會搭配 `VisualPen`；`Region` 通常以 `RegionContext` 描述填滿輪廓。

#### 取得方式

`VisualPen` 由 `VisualObject.Pen` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Type` | 決定 aperture / stroke 的形狀或來源。 |
| `Context` | 儲存尺寸參數，內容由 `Type` 決定。 |
| `Symbols` | 用於 `Symbol` pen，表示複合 symbol 內部的 visual objects。 |

#### VisualPenType

| Type | 說明 |
| --- | --- |
| `Rectangle` | 矩形 pen，`Context = { Width, Height }`。 |
| `Ellipse` | 橢圓或圓形 pen，`Context = { Radius }` 或 `{ Width, Height }`。 |
| `Oval` | Oval pen，`Context = { Width, Height }`。 |
| `Symbol` | 複合 symbol，使用 `Symbols` 描述內部 visual objects。 |
| `Special` | 特殊 aperture，無固定公開尺寸格式。 |

### VisualObjectData

#### 物件定義

`VisualObjectData` 是 `VisualObject` 的實際幾何資料。單一 `VisualObject` 可能包含多筆 `VisualObjectData`，例如 step repeat 或其他轉換後的重複資料。

#### 取得方式

`VisualObjectData` 由 `VisualObject.Datas` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Bound` | 該 data 的邊界。 |
| `ParentBound` | 來源 parent drawing scope 的邊界。 |
| `PenTheta` | Pen rotation。 |
| `PenFlip` | Pen flip。 |
| `Points` | 點列資料，用於 `Aperture`、`Line`、`Arc`。 |
| `ArcRadius` | Arc 半徑，主要用於 `Arc`。 |
| `ArcIsClockwise` | Arc 方向，主要用於 `Arc`。 |
| `RegionContext` | 填滿區域輪廓資料，主要用於 `Region`。 |

#### Points 排列

| Type | Points |
| --- | --- |
| `Aperture` | `{ Cx, Cy }` |
| `Line` | `{ Sx, Sy, Ex, Ey }` |
| `Arc` | `{ Sx, Sy, Ex, Ey, Cx, Cy }` |
| `Region` | 不以 `Points` 作為主要資料來源。 |

#### 範例程式碼

```csharp
foreach (VisualObject Object in Objects)
{
    foreach (VisualObjectData Data in Object.Datas)
    {
        switch (Object.Type)
        {
            case VisualObjectType.Aperture:
                {
                    double Cx = Data.Points[0],
                           Cy = Data.Points[1];
                    break;
                }
            case VisualObjectType.Line:
                {
                    double Sx = Data.Points[0],
                           Sy = Data.Points[1],
                           Ex = Data.Points[2],
                           Ey = Data.Points[3];
                    break;
                }
            case VisualObjectType.Arc:
                {
                    double ArcSx = Data.Points[0],
                           ArcSy = Data.Points[1],
                           ArcEx = Data.Points[2],
                           ArcEy = Data.Points[3],
                           ArcCx = Data.Points[4],
                           ArcCy = Data.Points[5],
                           Radius = Data.ArcRadius;
                    bool IsClockwise = Data.ArcIsClockwise;
                    break;
                }
            case VisualObjectType.Region:
                {
                    VisualRegionContext[] RegionContext = Data.RegionContext;
                    break;
                }
        }
    }
}
```

### VisualRegionContext

#### 物件定義

`VisualRegionContext` 描述 `Region` object 的填滿輪廓。它儲存區域極性與輪廓片段清單。

#### 取得方式

`VisualRegionContext` 由 `VisualObjectData.RegionContext` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Polarity` | 區域極性。 |
| `Descriptions` | 區域輪廓片段清單。 |

### VisualRegionDescription

#### 物件定義

`VisualRegionDescription` 表示 region 輪廓中的單一片段。每個片段可能是 line segment 或 arc segment。

#### 取得方式

`VisualRegionDescription` 由 `VisualRegionContext.Descriptions` 取得。

#### 屬性與內容

| Member | 說明 |
| --- | --- |
| `Points` | 片段座標資料。 |
| `ArcRadius` | Arc 半徑。若不是 arc segment，值為 `NaN`。 |
| `ArcIsClockwise` | Arc 方向。僅 arc segment 具有實際意義。 |

#### Points 排列

| Description | Points | Additional members |
| --- | --- | --- |
| Line segment | `{ Sx, Sy, Ex, Ey }` | 無 |
| Arc segment | `{ Sx, Sy, Ex, Ey, Cx, Cy }` | `ArcRadius`、`ArcIsClockwise` |

#### 範例程式碼

```csharp
foreach (VisualRegionContext Context in Data.RegionContext)
{
    foreach (VisualRegionDescription Description in Context.Descriptions)
    {
        if (double.IsNaN(Description.ArcRadius))
        {
            double Sx = Description.Points[0],
                   Sy = Description.Points[1],
                   Ex = Description.Points[2],
                   Ey = Description.Points[3];
        }
        else
        {
            double Sx = Description.Points[0],
                   Sy = Description.Points[1],
                   Ex = Description.Points[2],
                   Ey = Description.Points[3],
                   Cx = Description.Points[4],
                   Cy = Description.Points[5],
                   Radius = Description.ArcRadius;
            bool IsClockwise = Description.ArcIsClockwise;
        }
    }
}
```

## 授權

授權章節說明如何檢查 hardware dongle 是否有效，以及目前可用功能。

### License.IsValid

#### 函數名稱

```csharp
public static bool IsValid { get; }
```

#### 目的用途

`License.IsValid` 用於檢查目前環境是否具有有效的 hardware dongle。

#### 回傳值

| 回傳值 | 說明 |
| --- | --- |
| `true` | Hardware dongle 有效。 |
| `false` | 找不到有效 hardware dongle，或授權無效。 |

#### 範例程式碼

```csharp
bool IsValid = License.IsValid;

if (!IsValid)
    return;
```

### License.Features

#### 函數名稱

```csharp
public static GerberLicenseFeature Features { get; }
```

#### 目的用途

`License.Features` 回傳目前 hardware dongle 開放的功能旗標。

#### 回傳值

| 回傳值 | 說明 |
| --- | --- |
| `GerberLicenseFeature` | 目前可用功能的 flags enum。 |

#### 範例程式碼

```csharp
GerberLicenseFeature Features = License.Features;
```

### License.HasFeature(...)

#### 函數名稱

```csharp
public static bool HasFeature(GerberLicenseFeature Feature)
```

#### 目的用途

`License.HasFeature(...)` 用於檢查特定功能或功能組合是否可用。

#### 參數

| 參數 | 說明 |
| --- | --- |
| `Feature` | 要檢查的功能旗標。 |

#### 回傳值

| 回傳值 | 說明 |
| --- | --- |
| `true` | 目前授權包含傳入的全部 flags。 |
| `false` | 目前授權不包含傳入的全部 flags。 |

#### 範例程式碼

```csharp
if (License.HasFeature(GerberLicenseFeature.VisualTree))
{
    // Visual tree feature is available.
}
```

### GerberLicenseFeature

#### 物件定義

`GerberLicenseFeature` 是 flags enum，用於表示目前 hardware dongle 開放的功能。

#### 取得方式

`GerberLicenseFeature` 可由 `License.Features` 取得，也可作為 `License.HasFeature(...)` 的參數。

#### 功能旗標

| Feature | 說明 |
| --- | --- |
| `None` | 沒有可用功能。 |
| `GraphicRS274X` | 允許渲染 RS274X layer。 |
| `GraphicODB` | 允許渲染 ODB++ layer。 |
| `VisualTreeRS274X` | 允許取得 RS274X layer 的 visual tree。 |
| `VisualTreeODB` | 允許取得 ODB++ layer 的 visual tree。 |
| `RS274X` | 包含 RS274X 的 graphic 與 visual tree 功能。 |
| `ODB` | 包含 ODB++ 的 graphic 與 visual tree 功能。 |
| `Graphic` | 包含 RS274X 與 ODB++ 的渲染功能。 |
| `VisualTree` | 包含 RS274X 與 ODB++ 的 visual tree 功能。 |
| `All` | 包含所有目前公開功能。 |

`HasFeature(...)` 會要求傳入的所有 flags 都存在。也就是說，`GerberLicenseFeature.Graphic` 需要同時具備 `GraphicRS274X` 與 `GraphicODB`。

## 回傳碼與錯誤處理

`Gerber.TryParse(...)` 使用 `FreesiaCode` 表示載入結果。

| Code | 說明 |
| --- | --- |
| `Success` | 載入成功。 |
| `Failure` | 路徑不存在、資料格式無法解析，或路徑內容不是支援的 Gerber 資料。 |
| `PermissionDenied` | Hardware dongle 存在，但未開放要求的功能。 |
| `Unauthorized` | 沒有有效的 hardware dongle。 |

可在 UI 或 service layer 將不同錯誤碼轉成使用者可理解的訊息。

```csharp
string Message;

switch (Code)
{
    case FreesiaCode.Success:
        {
            Message = "Success.";
            break;
        }
    case FreesiaCode.Failure:
        {
            Message = "Unsupported or invalid Gerber data.";
            break;
        }
    case FreesiaCode.PermissionDenied:
        {
            Message = "The hardware dongle does not allow this feature.";
            break;
        }
    case FreesiaCode.Unauthorized:
        {
            Message = "A valid hardware dongle is required.";
            break;
        }
    default:
        {
            Message = "Unknown error.";
            break;
        }
}
```

渲染與 visual tree API 在功能授權不足時可能拋出 `InvalidOperationException`。

```csharp
try
{
    VisualRoot Root = Layer.GetVisualTree(Visual);
}
catch (InvalidOperationException)
{
    // The required feature is not available.
}
```

## 整合指引

- 啟動時可先檢查 `License.IsValid`，用於判斷是否有有效 hardware dongle。
- 功能按鈕或選單可依照 `License.Features` 決定是否啟用。
- 載入資料時以 `Gerber.TryParse(...)` 的 `FreesiaCode` 作為主要錯誤處理依據。
- 渲染前先確認 `ROI` 與 `PixelPerUnit`，避免產生過大的 image buffer。
- 若只需要幾何資料或物件查詢，可使用 visual tree，不一定需要先 render raster image。

## 注意事項

- `Bound` 的單位由對應的 `Unit` 決定。
- `PixelPerUnit` 會影響輸出影像尺寸、記憶體使用量與渲染時間。
- `RenderImage<T>` 使用完畢後應適時釋放。
