# Freesia Gerber Demo

## Overview

This repository provides a WPF demo application that demonstrates how to use `FreesiaGerberLib.dll`.

The demo source code is provided for learning, integration reference, and evaluation of the commercial Gerber processing library workflow.

The demo focuses on the main public API workflows:

- Loading Gerber data with `Gerber.TryParse(...)`.
- Rendering layers with `ILayer.Render<T>(...)`.
- Reading visual tree data with `ILayer.GetVisualTree(...)`.
- Checking hardware dongle and feature availability with `License`.

## Requirements

- Visual Studio with WPF desktop development support.
- The demo app targets .NET Framework 4.6.2.
- `FreesiaGerberLib.dll` targets `netstandard2.0`.
- Using `FreesiaGerberLib.dll` requires a valid hardware dongle.

## Documentation

The updated Markdown user manuals are the primary technical references:

- [Traditional Chinese User Manual](docs/USER-MANUAL.zh-TW.md)
- [English User Manual](docs/USER-MANUAL.en-US.md)

If a PDF user manual is included in this repository, it may be an older reference document. Please use the Markdown manuals above as the current documentation.

## License

The Apache License 2.0 applies only to the demo source code and sample code in this repository.

`FreesiaGerberLib.dll` is proprietary commercial software and is not licensed under the Apache License 2.0.

Using `FreesiaGerberLib.dll` requires a valid hardware dongle purchased from the author.

This repository does not grant any right to redistribute, modify, reverse engineer, or bypass the licensing mechanism of `FreesiaGerberLib.dll`.

For details, see:

- `LICENSE`
- `DLL-LICENSE.md`

## Commercial Contact

For hardware dongle purchase or commercial licensing inquiries, please contact:

Email: `freesiatech0308@gmail.com`

[Contact by email](mailto:freesiatech0308@gmail.com?subject=FreesiaGerberLib%20Commercial%20License%20Inquiry&body=Hello%2C%0A%0AI%20am%20interested%20in%20purchasing%20a%20hardware%20dongle%20for%20FreesiaGerberLib.%0A%0ACompany%20%2F%20Name%3A%20%0ACountry%20%2F%20Region%3A%20%0AUse%20case%3A%20%0AQuantity%3A%20%0A%0AThank%20you.)
