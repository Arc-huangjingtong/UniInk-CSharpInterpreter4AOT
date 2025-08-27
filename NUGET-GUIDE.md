# NuGet Package Guide for UniInk C# Interpreter

## 📦 Package Overview

UniInk is now available as a NuGet package! This high-performance, zero-GC, AOT-compatible C# expression interpreter is perfect for Unity IL2CPP environments and performance-critical applications.

**Package ID**: `Arc.UniInk`  
**Current Version**: `1.1.0`  
**Target Framework**: `.NET Standard 2.0`

## 🚀 Quick Start

### Installing the Package

```bash
# Using .NET CLI
dotnet add package Arc.UniInk

# Using Package Manager Console in Visual Studio
Install-Package Arc.UniInk

# Using PackageReference in .csproj
<PackageReference Include="Arc.UniInk" Version="1.1.0" />
```

### Basic Usage

```csharp
using Arc.UniInk;

// Create interpreter instance
var ink = new UniInk();

// Evaluate expressions
var result1 = ink.Evaluate("3 + 5 * 2").GetResult_Int(); // Returns 13
var result2 = ink.Evaluate("\"Hello\" + \" \" + \"World\"").GetResult_String(); // Returns "Hello World"
var result3 = ink.Evaluate("true && false || 1 == 1").GetResult_Bool(); // Returns true
```

## 🔧 Development Workflow

### Building the Package Locally

```bash
# Navigate to the project directory
cd Arc.UniInk/Arc.UniInk

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Create NuGet package
dotnet pack --configuration Release
```

The package will be created at: `bin/Release/Arc.UniInk.1.1.0.nupkg`

### Package Structure

```
Arc.UniInk.1.1.0.nupkg
├── lib/
│   └── netstandard2.0/
│       └── Arc.UniInk.dll
├── Arc.UniInk.nuspec
└── README.md
```

## 📋 Publishing Steps

### 1. Prepare for Publishing

Before publishing to NuGet.org, ensure:

- [ ] All unit tests pass
- [ ] Version number is updated in `.csproj`
- [ ] Release notes are updated
- [ ] Documentation is current

### 2. Create NuGet.org Account

1. Visit [nuget.org](https://www.nuget.org/)
2. Create an account or sign in
3. Generate an API key from your account settings

### 3. Publish the Package

```bash
# Method 1: Using dotnet CLI with API key
dotnet nuget push bin/Release/Arc.UniInk.1.1.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Method 2: Upload via NuGet.org web interface
# 1. Go to nuget.org/packages/manage/upload
# 2. Upload the .nupkg file
# 3. Follow the verification steps
```

### 4. Verify Publication

- Check package page: `https://www.nuget.org/packages/Arc.UniInk/`
- Test installation in a new project
- Verify package metadata and documentation

## 🔄 Version Management

### Semantic Versioning

UniInk follows [Semantic Versioning](https://semver.org/):

- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible functionality additions  
- **PATCH** version for backwards-compatible bug fixes

### Updating Package Version

Update the version in `Arc.UniInk.csproj`:

```xml
<PropertyGroup>
  <PackageVersion>1.2.0</PackageVersion>
  <AssemblyVersion>1.2.0.0</AssemblyVersion>
  <FileVersion>1.2.0.0</FileVersion>
</PropertyGroup>
```

## 📋 Pre-publish Checklist

- [ ] Code builds without warnings in Release configuration
- [ ] All tests pass
- [ ] Version number incremented appropriately
- [ ] Release notes updated in `.csproj`
- [ ] Documentation updated
- [ ] Package metadata reviewed
- [ ] Dependencies verified and minimized
- [ ] Package tested in clean environment

## 🔍 Testing the Package

### Local Testing

```bash
# Create a test project
mkdir TestUniInk
cd TestUniInk
dotnet new console

# Add local package reference
dotnet add package Arc.UniInk --source ../Arc.UniInk/Arc.UniInk/bin/Release

# Test the functionality
# Add test code to Program.cs and run with 'dotnet run'
```

### Integration Testing

1. Create minimal test applications
2. Test in different .NET implementations (.NET Core, .NET Framework, Unity)
3. Verify performance characteristics
4. Test AOT compilation scenarios

## 🎯 Package Features

### Core Benefits

- **Zero GC**: No garbage collection during expression evaluation
- **High Performance**: 3-9x faster than competing libraries
- **AOT Compatible**: Works with Unity IL2CPP and other AOT environments
- **Single File**: Simple integration with minimal dependencies
- **Cross Platform**: .NET Standard 2.0 compatibility

### Supported Scenarios

- Unity game development (IL2CPP)
- High-performance applications
- Embedded scripting engines
- Configuration-driven expression evaluation
- Real-time calculation systems

## 📞 Support

For questions, issues, or contributions:

- **GitHub Issues**: [UniInk Issues](https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4AOT/issues)
- **Documentation**: See README.md and REPOSITORY_ANALYSIS.md
- **License**: MIT License (see LICENSE file)

## 📈 Package Statistics

Once published, monitor package health:

- Download statistics on nuget.org
- GitHub issues and discussions
- Community feedback and contributions
- Performance benchmarks and comparisons

---

*This guide will help you successfully package, publish, and maintain the UniInk NuGet package. For the latest information, refer to the project repository.*