# FreesiaGerberLib User Manual

## Table Of Contents

- [Overview](#overview)
- [System Requirements](#system-requirements)
- [Supported Formats](#supported-formats)
- [Core Entry Points](#core-entry-points)
  - [Gerber.TryParse(...)](#gerbertryparse)
  - [IGerber](#igerber)
  - [IStep](#istep)
  - [ILayer](#ilayer)
- [Rendering](#rendering)
  - [`ILayer.Render<T>(...)`](#ilayerrendert)
  - [GraphicOption](#graphicoption)
  - [VisualOption](#visualoption)
  - [RenderOption](#renderoption)
  - [Pixel Struct](#pixel-struct)
- [Visual Tree](#visual-tree)
  - [ILayer.GetVisualTree(...)](#ilayergetvisualtree)
  - [VisualRoot](#visualroot)
  - [VisualLayer](#visuallayer)
  - [VisualObject](#visualobject)
  - [VisualObjectType](#visualobjecttype)
  - [VisualPen](#visualpen)
  - [VisualObjectData](#visualobjectdata)
  - [VisualRegionContext](#visualregioncontext)
  - [VisualRegionDescription](#visualregiondescription)
- [Licensing](#licensing)
  - [License.IsValid](#licenseisvalid)
  - [License.Features](#licensefeatures)
  - [License.HasFeature(...)](#licensehasfeature)
  - [GerberLicenseFeature](#gerberlicensefeature)
- [Return Codes And Error Handling](#return-codes-and-error-handling)
- [Integration Guidelines](#integration-guidelines)
- [Notes](#notes)

## Overview

`FreesiaGerberLib.dll` is a commercial class library for loading Gerber data, rendering layer images, and obtaining a visual tree.

This document describes the main public API and standard usage of `FreesiaGerberLib.dll`. The content is organized by the library's main capabilities: core entry points, rendering, visual tree, and licensing.

Main APIs:

- `Gerber.TryParse(...)`: Loads an RS274X file, an ODB++ folder, or a folder containing multiple RS274X files.
- `IGerber`, `IStep`, `ILayer`: Read loaded Gerber data.
- `ILayer.Render<T>(...)`: Renders a layer as a raster image.
- `ILayer.GetVisualTree(...)`: Gets the visual tree of a layer.
- `License`: Checks the hardware dongle and licensed features.

## System Requirements

- `FreesiaGerberLib.dll` targets `netstandard2.0`.
- Using `FreesiaGerberLib.dll` requires a valid hardware dongle.
- The demo application currently uses WPF and .NET Framework 4.6.2.

## Supported Formats

`FreesiaGerberLib.dll` supports the following Gerber data formats:

| Format | Input |
| --- | --- |
| RS274X | File path or folder path |
| ODB++ | Folder path |

When the input path is a folder path, the library first attempts to load it as ODB++. If the folder is not a valid ODB++ folder, the library then attempts to load it as a folder containing multiple RS274X files. When multiple RS274X files are loaded successfully, the library creates one `ORG` Step and adds each RS274X file as a Layer in that Step.

After loading succeeds, use `IGerber.Type` to identify the actual data format.

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

## Core Entry Points

This section describes data loading and the objects returned after loading. A typical usage flow is:

1. Load data with `Gerber.TryParse(...)`.
2. Get an `IStep` from `IGerber`.
3. Get an `ILayer` from `IStep`.
4. Use `ILayer` for rendering or visual tree retrieval.

### Gerber.TryParse(...)

#### Function Name

```csharp
public static FreesiaCode TryParse(string FilePath, out IGerber GerberInfo)
```

#### Purpose

`Gerber.TryParse(...)` is the primary data loading entry point of `FreesiaGerberLib.dll`. It loads an RS274X file, an ODB++ folder, or a folder containing multiple RS274X files. When the input path is a folder path, the library attempts to parse it as ODB++ first, then as multiple RS274X files.

#### Parameters

| Parameter | Description |
| --- | --- |
| `FilePath` | Gerber data path. It can be an RS274X file path, an ODB++ folder path, or a folder path containing multiple RS274X files. |
| `GerberInfo` | Outputs `IGerber` when loading succeeds; `null` when loading fails. |

#### Return Value

| Return Value | Description |
| --- | --- |
| `FreesiaCode.Success` | Loading succeeded. |
| `FreesiaCode.Failure` | The path does not exist, the data format cannot be parsed, or the path content is not supported Gerber data. |
| `FreesiaCode.PermissionDenied` | A hardware dongle is present, but the requested feature is not enabled. |
| `FreesiaCode.Unauthorized` | No valid hardware dongle is available. |

#### Example

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

#### Licensing Requirement

A valid hardware dongle is required before calling `Gerber.TryParse(...)`. If authorization is invalid, the function returns `FreesiaCode.Unauthorized`.

### IGerber

#### Object Definition

`IGerber` is the root object returned after loading succeeds. It represents one Gerber data source and stores the data format, source path, and step list.

When a folder containing multiple RS274X files is loaded, `IGerber.Type` is `GerberTypes.RS274X`, and `Steps` contains the `ORG` Step created by the library.

#### How To Obtain

`IGerber` is obtained from the `out IGerber GerberInfo` parameter of `Gerber.TryParse(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Type` | Gerber data format, such as `GerberTypes.RS274X` or `GerberTypes.ODB`. |
| `Path` | Source data path. |
| `Steps` | Step list. |
| `this[int Index]` | Gets an `IStep` by index. |
| `this[string Name]` | Gets an `IStep` by name. |

### IStep

#### Object Definition

`IStep` represents a step in Gerber data. It stores the step name, unit, bound, and layer list.

For a folder containing multiple RS274X files, the library creates a Step named `ORG` and adds each parsable RS274X file to `Layers`.

#### How To Obtain

`IStep` can be obtained from `IGerber.Steps` or an `IGerber` indexer.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Name` | Step name. |
| `Unit` | Unit used by the step. |
| `Bound` | Step bound. |
| `Fields` | Step field information. |
| `Layers` | Layer list. |
| `this[int Index]` | Gets an `ILayer` by index. |
| `this[string Name]` | Gets an `ILayer` by name. |
| `Parent` | The owning `IGerber`. |

### ILayer

#### Object Definition

`ILayer` represents an operable Gerber layer. It stores the layer name, unit, bound, polarity, and field information. It is also the main entry point for rendering and visual tree retrieval.

#### How To Obtain

`ILayer` can be obtained from `IStep.Layers` or an `IStep` indexer.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Name` | Layer name. |
| `Unit` | Unit used by the layer. |
| `Bound` | Layer bound. |
| `Polarity` | Layer polarity. |
| `Fields` | Layer field information. |
| `Render<T>(...)` | Renders the layer as a raster image. |
| `GetVisualTree(...)` | Gets the visual tree of the layer. |

## Rendering

This section describes how to render a layer as a raster image with `ILayer.Render<T>(...)`.

### `ILayer.Render<T>(...)`

#### Function Name

```csharp
public RenderImage<T> Render<T>(GraphicOption Graphic, VisualOption Visual, RenderOption Render)
    where T : unmanaged, IPixel;

public void Render<T>(RenderImage<T> Image, GraphicOption Graphic, VisualOption Visual, RenderOption Render)
    where T : unmanaged, IPixel;
```

#### Purpose

`ILayer.Render<T>(...)` renders the current layer as a `RenderImage<T>`. The first overload allocates image memory in the library. The second overload renders into a `RenderImage<T>` provided by the caller.

#### Parameters

| Parameter | Description |
| --- | --- |
| `Image` | Existing output image. Used only by the second overload. |
| `Graphic` | Drawing range, resolution, foreground color, and background color. |
| `Visual` | Rotation and flip settings. |
| `Render` | Rendering execution settings. |
| `T` | Pixel struct. It must be a pixel struct supported by the library. |

#### Return Value Or Output

| Overload | Result |
| --- | --- |
| `Render<T>(Graphic, Visual, Render)` | Returns a newly allocated `RenderImage<T>`. |
| `Render<T>(Image, Graphic, Visual, Render)` | Writes the result into an existing `RenderImage<T>`. |

#### Example

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

Rendering into an existing `RenderImage<T>`:

```csharp
using (RenderImage<BGRA> Image = new RenderImage<BGRA>(2000, 2000))
{
    Layer.Render(Image, Graphic, Visual, Render);
}
```

#### Licensing Requirement

Rendering requires the corresponding feature license:

- RS274X layer: `GerberLicenseFeature.GraphicRS274X`
- ODB++ layer: `GerberLicenseFeature.GraphicODB`

### GraphicOption

#### Object Definition

`GraphicOption` stores the drawing range, resolution, background color, and foreground color for raster output.

#### How To Obtain

`GraphicOption` is created by the caller and passed to `ILayer.Render<T>(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `ROI` | Output range. `Layer.Bound` is commonly used to generate the full layer image. |
| `PixelPerUnit` | Number of pixels per Gerber unit. |
| `Background` | Background color. |
| `Foreground` | Foreground color. |
| `ClearCanvas` | Whether to clear the canvas before drawing. |

`PixelPerUnit` affects the output image size:

```text
ImageWidth  = Ceiling(ROI.Width  * PixelPerUnit)
ImageHeight = Ceiling(ROI.Height * PixelPerUnit)
```

### VisualOption

#### Object Definition

`VisualOption` stores rotation and flip settings for rendering or visual tree generation.

#### How To Obtain

`VisualOption` is created by the caller and passed to `ILayer.Render<T>(...)` or `ILayer.GetVisualTree(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Flip` | Flip mode. |
| `FlipCx` | Flip center X. |
| `FlipCy` | Flip center Y. |
| `RotateAngle` | Rotation angle. |
| `RotateCx` | Rotation center X. |
| `RotateCy` | Rotation center Y. |

### RenderOption

#### Object Definition

`RenderOption` stores rendering execution settings.

#### How To Obtain

`RenderOption` is created by the caller and passed to `ILayer.Render<T>(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `MaxDegreeOfParallelism` | Maximum degree of parallelism. `0` uses the library / .NET default parallelism, allowing the runtime to schedule work based on available CPU and ThreadPool state. `1` uses single-threaded rendering. A value greater than `1` limits the maximum parallelism. |

To render with available CPU resources, set `MaxDegreeOfParallelism` to `0`. In the current implementation, values less than `0` also fall back to default parallelism, but this is not the officially recommended usage of this API.

### Pixel Struct

#### Object Definition

A pixel struct determines the pixel channel order and bit depth of `RenderImage<T>`. The `T` used by `ILayer.Render<T>(...)` must be a pixel struct supported by the library.

`IPixel` is the pixel interface used by the Freesia imaging pipeline. For public usage, select one of the pixel structs already provided by the library according to the required output format.

#### Supported Types

| Pixel struct | Description |
| --- | --- |
| `BGRA` | 32-bit BGRA, commonly used with WPF `PixelFormats.Bgra32`. |
| `ARGB` | 32-bit ARGB. |
| `RGBA` | 32-bit RGBA. |
| `ABGR` | 32-bit ABGR. |
| `RGB` | 24-bit RGB, without alpha channel. |
| `BGR` | 24-bit BGR, without alpha channel. |
| `Gray8` | 8-bit grayscale. |

#### Usage

- WPF `PixelFormats.Bgra32` corresponds to `BGRA`.
- For 32-bit output with alpha, choose `BGRA`, `ARGB`, `RGBA`, or `ABGR` according to the target buffer channel order.
- For grayscale output, use `Gray8`.

## Visual Tree

This section describes how to obtain layer geometry data through `ILayer.GetVisualTree(...)` and read objects in the visual tree.

### ILayer.GetVisualTree(...)

#### Function Name

```csharp
public VisualRoot GetVisualTree(VisualOption Visual)
```

#### Purpose

`ILayer.GetVisualTree(...)` gets the visual tree of the current layer. The visual tree can be used for object queries, range hit testing, or custom display workflows.

#### Parameters

| Parameter | Description |
| --- | --- |
| `Visual` | Rotation and flip settings. Pass `null` to avoid applying an additional visual transform. |

#### Return Value

| Return Value | Description |
| --- | --- |
| `VisualRoot` | Visual tree root node. |

#### Example

```csharp
VisualRoot Root = Layer.GetVisualTree(Visual);
VisualLayer Content = Root.Content;
```

#### Licensing Requirement

Visual tree functionality requires the corresponding feature license:

- RS274X layer: `GerberLicenseFeature.VisualTreeRS274X`
- ODB++ layer: `GerberLicenseFeature.VisualTreeODB`

### VisualRoot

#### Object Definition

`VisualRoot` is the root node of a visual tree. It stores the overall information of a step or layer after visual transform.

#### How To Obtain

`VisualRoot` is obtained from `ILayer.GetVisualTree(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Name` | Root name. |
| `Bound` | Root bound. |
| `Angle` | Applied angle. |
| `Flip` | Applied flip mode. |
| `Content` | Main `VisualLayer` under the root. |
| `FindByPoint(...)` | Queries visual objects by coordinate. |
| `FindByBound(...)` | Queries visual objects by range. |

### VisualLayer

#### Object Definition

`VisualLayer` is a layer node in the visual tree. It stores the layer name, bound, child layers, and visual objects.

#### How To Obtain

`VisualLayer` can be obtained from `VisualRoot.Content` or another `VisualLayer.Layers`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Name` | Visual layer name. |
| `Step` | Owning `VisualRoot`. |
| `Bound` | Visual layer bound. |
| `Layers` | Child `VisualLayer` list. |
| `Children` | `VisualObject` instances directly contained in this layer. |
| `FindByPoint(...)` | Queries visual objects by coordinate. |
| `FindByBound(...)` | Queries visual objects by range. |

### VisualObject

#### Object Definition

`VisualObject` is a geometry object in the visual tree. It stores the geometry type, pen, polarity, attributes, and geometry data.

#### How To Obtain

`VisualObject` can be obtained from `VisualLayer.Children`, `VisualRoot.FindByPoint(...)`, `VisualRoot.FindByBound(...)`, `VisualLayer.FindByPoint(...)`, or `VisualLayer.FindByBound(...)`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Type` | Visual object geometry type. |
| `Pen` | Shape source of the aperture or stroke. |
| `Polarity` | Object polarity. |
| `Attributes` | Object attribute data. |
| `Datas` | Object geometry data list. |

### VisualObjectType

#### Object Definition

`VisualObjectType` indicates the geometry type of a `VisualObject` and affects how `VisualObjectData` is represented.

#### How To Obtain

`VisualObjectType` is obtained from `VisualObject.Type`.

#### Type Description

| Type | Description | Primary Data |
| --- | --- | --- |
| `Aperture` | A single aperture / flash. | `Points = { Cx, Cy }`, representing the center coordinate. |
| `Line` | A line segment. | `Points = { Sx, Sy, Ex, Ey }`, representing the start and end points. |
| `Arc` | An arc. | `Points = { Sx, Sy, Ex, Ey, Cx, Cy }`, together with `ArcRadius` and `ArcIsClockwise`. |
| `Region` | A filled region. | Mainly uses `RegionContext` to describe contour data. |

### VisualPen

#### Object Definition

`VisualPen` describes the shape source of an aperture or stroke. `Aperture`, `Line`, and `Arc` usually use `VisualPen`; `Region` usually describes the filled contour through `RegionContext`.

#### How To Obtain

`VisualPen` is obtained from `VisualObject.Pen`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Type` | Determines the shape or source of the aperture / stroke. |
| `Context` | Stores size parameters. The content is determined by `Type`. |
| `Symbols` | Used by `Symbol` pen to represent visual objects inside a composite symbol. |

#### VisualPenType

| Type | Description |
| --- | --- |
| `Rectangle` | Rectangle pen, `Context = { Width, Height }`. |
| `Ellipse` | Ellipse or circle pen, `Context = { Radius }` or `{ Width, Height }`. |
| `Oval` | Oval pen, `Context = { Width, Height }`. |
| `Symbol` | Composite symbol, using `Symbols` to describe internal visual objects. |
| `Special` | Special aperture, with no fixed public size format. |

### VisualObjectData

#### Object Definition

`VisualObjectData` is the actual geometry data of a `VisualObject`. A single `VisualObject` may contain multiple `VisualObjectData` entries, such as repeated data after step repeat or other transforms.

#### How To Obtain

`VisualObjectData` is obtained from `VisualObject.Datas`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Bound` | Bound of this data entry. |
| `ParentBound` | Bound of the source parent drawing scope. |
| `PenTheta` | Pen rotation. |
| `PenFlip` | Pen flip. |
| `Points` | Point sequence data, used by `Aperture`, `Line`, and `Arc`. |
| `ArcRadius` | Arc radius, mainly used by `Arc`. |
| `ArcIsClockwise` | Arc direction, mainly used by `Arc`. |
| `RegionContext` | Filled region contour data, mainly used by `Region`. |

#### Points Layout

| Type | Points |
| --- | --- |
| `Aperture` | `{ Cx, Cy }` |
| `Line` | `{ Sx, Sy, Ex, Ey }` |
| `Arc` | `{ Sx, Sy, Ex, Ey, Cx, Cy }` |
| `Region` | Does not use `Points` as the primary data source. |

#### Example

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

#### Object Definition

`VisualRegionContext` describes the filled contour of a `Region` object. It stores region polarity and a list of contour segments.

#### How To Obtain

`VisualRegionContext` is obtained from `VisualObjectData.RegionContext`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Polarity` | Region polarity. |
| `Descriptions` | List of region contour segments. |

### VisualRegionDescription

#### Object Definition

`VisualRegionDescription` represents a single segment in a region contour. Each segment can be a line segment or an arc segment.

#### How To Obtain

`VisualRegionDescription` is obtained from `VisualRegionContext.Descriptions`.

#### Properties And Members

| Member | Description |
| --- | --- |
| `Points` | Segment coordinate data. |
| `ArcRadius` | Arc radius. If the segment is not an arc segment, the value is `NaN`. |
| `ArcIsClockwise` | Arc direction. It has practical meaning only for arc segments. |

#### Points Layout

| Description | Points | Additional members |
| --- | --- | --- |
| Line segment | `{ Sx, Sy, Ex, Ey }` | None |
| Arc segment | `{ Sx, Sy, Ex, Ey, Cx, Cy }` | `ArcRadius`, `ArcIsClockwise` |

#### Example

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

## Licensing

This section describes how to check whether the hardware dongle is valid and which features are currently available.

### License.IsValid

#### Function Name

```csharp
public static bool IsValid { get; }
```

#### Purpose

`License.IsValid` checks whether the current environment has a valid hardware dongle.

#### Return Value

| Return Value | Description |
| --- | --- |
| `true` | The hardware dongle is valid. |
| `false` | No valid hardware dongle is found, or authorization is invalid. |

#### Example

```csharp
bool IsValid = License.IsValid;

if (!IsValid)
    return;
```

### License.Features

#### Function Name

```csharp
public static GerberLicenseFeature Features { get; }
```

#### Purpose

`License.Features` returns the feature flags enabled by the current hardware dongle.

#### Return Value

| Return Value | Description |
| --- | --- |
| `GerberLicenseFeature` | Flags enum of currently available features. |

#### Example

```csharp
GerberLicenseFeature Features = License.Features;
```

### License.HasFeature(...)

#### Function Name

```csharp
public static bool HasFeature(GerberLicenseFeature Feature)
```

#### Purpose

`License.HasFeature(...)` checks whether a specific feature or feature combination is available.

#### Parameters

| Parameter | Description |
| --- | --- |
| `Feature` | Feature flag to check. |

#### Return Value

| Return Value | Description |
| --- | --- |
| `true` | The current authorization contains all passed flags. |
| `false` | The current authorization does not contain all passed flags. |

#### Example

```csharp
if (License.HasFeature(GerberLicenseFeature.VisualTree))
{
    // Visual tree feature is available.
}
```

### GerberLicenseFeature

#### Object Definition

`GerberLicenseFeature` is a flags enum used to represent the features enabled by the current hardware dongle.

#### How To Obtain

`GerberLicenseFeature` can be obtained from `License.Features`, or used as a parameter of `License.HasFeature(...)`.

#### Feature Flags

| Feature | Description |
| --- | --- |
| `None` | No features are available. |
| `GraphicRS274X` | Allows rendering RS274X layers. |
| `GraphicODB` | Allows rendering ODB++ layers. |
| `VisualTreeRS274X` | Allows obtaining visual trees from RS274X layers. |
| `VisualTreeODB` | Allows obtaining visual trees from ODB++ layers. |
| `RS274X` | Includes graphic and visual tree features for RS274X. |
| `ODB` | Includes graphic and visual tree features for ODB++. |
| `Graphic` | Includes rendering features for RS274X and ODB++. |
| `VisualTree` | Includes visual tree features for RS274X and ODB++. |
| `All` | Includes all currently public features. |

`HasFeature(...)` requires all passed flags to be present. In other words, `GerberLicenseFeature.Graphic` requires both `GraphicRS274X` and `GraphicODB`.

## Return Codes And Error Handling

`Gerber.TryParse(...)` uses `FreesiaCode` to represent the loading result.

| Code | Description |
| --- | --- |
| `Success` | Loading succeeded. |
| `Failure` | The path does not exist, the data format cannot be parsed, or the path content is not supported Gerber data. |
| `PermissionDenied` | A hardware dongle is present, but the requested feature is not enabled. |
| `Unauthorized` | No valid hardware dongle is available. |

In a UI or service layer, different error codes can be converted into user-facing messages.

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

Rendering and visual tree APIs may throw `InvalidOperationException` when the required feature license is unavailable.

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

## Integration Guidelines

- Check `License.IsValid` at startup to determine whether a valid hardware dongle is available.
- Enable or disable feature buttons and menu items according to `License.Features`.
- Use the `FreesiaCode` returned by `Gerber.TryParse(...)` as the primary basis for load error handling.
- Verify `ROI` and `PixelPerUnit` before rendering to avoid creating an excessively large image buffer.
- If only geometry data or object queries are required, use the visual tree. Rendering a raster image first is not required.

## Notes

- The unit of `Bound` is determined by the corresponding `Unit`.
- `PixelPerUnit` affects output image size, memory usage, and rendering time.
- `RenderImage<T>` should be disposed when it is no longer needed.
