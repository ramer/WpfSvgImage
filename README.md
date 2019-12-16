<h1 align="center"> WpfSvgImage </h1>
<h3 align="center"> <a href="../../blob/master/WpfSvgImageVBNet/SVGImage.vb">SvgImage VB.NET</a> class for WPF example project</h3>
<h3 align="center"> <a href="../../blob/master/WpfSvgImageCSharp/SvgImage.cs">SvgImage C#</a> class for WPF example project</h3>

## Table of Contents

- [Introduction](#introduction)
- [Usage](#usage)
- [Dependency Properties](#dependency-properties)
- [Feedback](#feedback)


## Introduction

WpfSvgImage is WPF Example application, which demonstrates how to use SvgImage class.

SvgImage class allows you to use .SVG images in WPF projects as ordinary images (.PNG .JPG)

## Usage

Source - Binding
```XAML
  <local:SvgImage Source="{Binding ElementName=TextBoxPath, Path=Text}" Fill="Black"/>
```
or Source - Resource
```XAML
  <local:SvgImage Source="WpfSvgImage;component/drawing.svg" Fill="Black"/>
```

## Dependency Properties

* **Source** - (Uri) Absolute or relative path to .svg file.
* **Data** - (Geometry) Read-only. Contains Geometry (or GeometryGroup) generated from .svg file.
* **ViewBoxWidth** - (Double) Read-only. Contains width of viewBox from .svg file.
* **ViewBoxHeight** - (Double) Read-only. Contains height of viewBox from .svg file.

## Feedback

Feel free to send [feature request or issue](../../issues). Feature requests are always welcome.
