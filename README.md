# Fraljiculator

**Fraljiculator** is a multifunctional graphing calculator written in C#, with an emphasis on visualizing both **real-valued** and **complex-valued** functions.

It combines conventional curve plotting with 2D function rendering, complex domain coloring, contour-style visualization, iterative maps, and interactive graph inspection in a Windows Forms interface.

## Features

### Real and complex visualization

Fraljiculator can evaluate functions over a two-dimensional coordinate domain and render either real or complex outputs.

Complex functions can be visualized through several coloring schemes, making properties such as phase, magnitude, zeros, poles, and spatial variation easier to inspect visually.

### Multiple coloring modes

The renderer currently provides five display modes:

* **Commonplace**
* **Monochromatic**
* **Bichromatic**
* **Kaleidoscopic**
* **Miscellaneous**

For complex functions, coloring can be derived from either Cartesian components or polar characteristics.

### Cartesian and polar contours

Two contour interpretations are available:

* **Cartesian `(x, y)`**
* **Polar `(r, θ)`**

These can be combined with the different color modes to produce conventional contour-like displays as well as domain-coloring visualizations.

### Curve plotting

Fraljiculator supports several kinds of curves:

* ordinary Cartesian functions;
* polar curves;
* parametric curves.

Curve sampling range and increment can be controlled explicitly, allowing both quick previews and denser renders.

### Iterative systems

The graph engine includes support for repeatedly evaluating real or complex expressions.

This makes it possible to experiment with iterative maps and other recurrence-based visualizations in addition to ordinary function plotting.

### Interactive inspection

The graph surface supports mouse-based inspection of rendered data.

Depending on the current mode, the interface can display information such as:

* `x` and `y` coordinates;
* modulus and argument;
* real and imaginary components of a complex result;
* the value of a real-valued function at the selected point.

Selected points can also be recorded in the program's output/history area.

### Rendering controls

The application includes controls for graph density, curve thickness, coordinate ranges, coloring behavior, axes/grids, preview rendering, and full rendering.

The renderer uses bitmap-based drawing and parallel numerical evaluation for computationally intensive graph displays.

## Technology

Fraljiculator is implemented in **C#** and uses the Windows desktop graphics stack, including:

* Windows Forms;
* `System.Drawing`;
* bitmap/pixel rendering;
* parallel computation;
* unsafe/pointer-based operations in performance-sensitive rendering paths;
* double-precision floating-point arithmetic.

The main user-interface class is `Graph`, implemented as a Windows Forms `Form`.

## Repository Status

The repository is currently a **source snapshot** rather than a complete distributable application project.

At present it contains:

```text
Fraljiculator/
├── Form1.cs
└── README.md
```

`Form1.cs` contains the main graphing and rendering implementation, but the published repository does not currently include the complete Visual Studio project structure required for a normal clone-and-build workflow.

In particular, the source references Windows Forms initialization and supporting application components that are not separately included in the repository.

For that reason, the code should currently be treated primarily as a reference implementation of Fraljiculator's graphing engine and UI logic.

## Source Overview

Most of the currently published implementation lives in:

```text
Form1.cs
```

The file contains the graph display layer together with substantial numerical and rendering logic, including:

```text
Input / expression processing
        │
        ▼
Real or complex evaluation
        │
        ├── Cartesian / polar / parametric curves
        ├── 2D real-function rendering
        ├── complex-function rendering
        └── iterative evaluation
        │
        ▼
Color / contour mapping
        │
        ▼
Bitmap and curve rendering
        │
        ▼
Interactive Windows Forms graph surface
```

The implementation uses `double` as its real-number representation:

```csharp
using Real = System.Double;
```

and maintains separate computational paths for real and complex graph data.

## Complex Domain Coloring

One of Fraljiculator's more distinctive capabilities is complex-function visualization.

Instead of reducing a complex result to a single scalar, the renderer can encode complex information in color. Depending on the selected mode, it can use real/imaginary components or modulus/argument information to construct patterns and color wheels.

This is particularly useful for visually exploring functions that would be difficult to understand using an ordinary Cartesian curve alone.

## Performance

Rendering a function over a two-dimensional pixel grid can require a large number of evaluations.

Fraljiculator therefore contains several performance-oriented implementation choices, including:

* parallel row computation;
* reusable matrix storage;
* direct bitmap manipulation;
* pointer-based loops in rendering paths;
* separate preview and main rendering regions.

These choices reflect the project's focus on interactive mathematical visualization rather than only scalar calculator operations.

## Development

A future repository layout suitable for normal development could look like:

```text
Fraljiculator/
├── Fraljiculator.sln
├── Fraljiculator/
│   ├── Fraljiculator.csproj
│   ├── Program.cs
│   ├── Graph.cs
│   ├── Graph.Designer.cs
│   ├── Numerics/
│   ├── Rendering/
│   └── ...
├── README.md
└── LICENSE
```

Separating numerical evaluation, rendering, and Windows Forms UI code would also make the graphing engine easier to test and reuse independently of the desktop interface.

## Contributing

The project is currently compact and experimental.

Useful areas for future work include:

* publishing the complete buildable Visual Studio project;
* separating the expression engine from rendering and UI code;
* documenting the supported expression syntax;
* adding examples for real, complex, polar, and parametric graphs;
* adding automated numerical tests;
* documenting keyboard and mouse controls;
* adding screenshots or example renders;
* improving portability of display-size-dependent UI constants.

Issues and pull requests are welcome once the corresponding project files and development workflow are available.

## License

No license file is currently included in the repository.

A license should be added before the project is intended for external reuse, modification, or redistribution.

---

**Fraljiculator** — exploring real and complex mathematics through computation and visualization.
