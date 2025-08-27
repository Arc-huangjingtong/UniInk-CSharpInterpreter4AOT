#!/bin/bash

# UniInk NuGet Package Build and Test Script
# Usage: ./build-package.sh [clean|build|pack|test|all]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/Arc.UniInk/Arc.UniInk"
PACKAGE_OUTPUT="$PROJECT_DIR/bin/Release"
TEST_DIR="/tmp/UniInkPackageTest"

function print_usage() {
    echo "Usage: $0 [clean|build|pack|test|all]"
    echo ""
    echo "Commands:"
    echo "  clean  - Clean build artifacts"
    echo "  build  - Build the project"
    echo "  pack   - Create NuGet package"
    echo "  test   - Test the package in a new project"
    echo "  all    - Run all steps above"
    echo ""
}

function clean_artifacts() {
    echo "🧹 Cleaning build artifacts..."
    if [ -d "$PROJECT_DIR/bin" ]; then
        rm -rf "$PROJECT_DIR/bin"
    fi
    if [ -d "$PROJECT_DIR/obj" ]; then
        rm -rf "$PROJECT_DIR/obj"
    fi
    echo "✅ Clean completed"
}

function build_project() {
    echo "🔨 Building project..."
    cd "$PROJECT_DIR"
    dotnet restore
    dotnet build --configuration Release --no-restore
    echo "✅ Build completed"
}

function create_package() {
    echo "📦 Creating NuGet package..."
    cd "$PROJECT_DIR"
    dotnet pack --configuration Release --no-build
    
    # List created packages
    echo "📋 Created packages:"
    ls -la "$PACKAGE_OUTPUT"/*.nupkg 2>/dev/null || echo "No packages found"
    echo "✅ Package creation completed"
}

function test_package() {
    echo "🧪 Testing package..."
    
    # Clean up previous test
    if [ -d "$TEST_DIR" ]; then
        rm -rf "$TEST_DIR"
    fi
    
    # Create test project
    mkdir -p "$TEST_DIR"
    cd "$TEST_DIR"
    
    echo "Creating test console application..."
    dotnet new console --force
    
    echo "Adding package reference..."
    dotnet add package Arc.UniInk --source "$PACKAGE_OUTPUT"
    
    # Create test program
    cat > Program.cs << 'EOF'
using Arc.UniInk;

Console.WriteLine("Testing UniInk NuGet Package");
Console.WriteLine("============================");

try {
    var ink = new UniInk();
    
    // Test basic arithmetic
    var result1 = ink.Evaluate("3 + 5 * 2");
    Console.WriteLine($"✅ Arithmetic: 3 + 5 * 2 = {result1.GetResult_Int()}");
    
    // Test string operations
    var result2 = ink.Evaluate("\"Hello\" + \" \" + \"NuGet\"");
    Console.WriteLine($"✅ String: {result2.GetResult_String()}");
    
    // Test boolean operations
    var result3 = ink.Evaluate("true && (1 == 1)");
    Console.WriteLine($"✅ Boolean: {result3.GetResult_Bool()}");
    
    Console.WriteLine("\n🎉 All tests passed! Package is working correctly.");
} catch (Exception ex) {
    Console.WriteLine($"❌ Test failed: {ex.Message}");
    Environment.Exit(1);
}
EOF
    
    echo "Running test..."
    dotnet run
    
    echo "✅ Package test completed successfully"
}

function run_all() {
    clean_artifacts
    build_project
    create_package
    test_package
    echo "🎉 All operations completed successfully!"
}

# Main script logic
case "${1:-all}" in
    clean)
        clean_artifacts
        ;;
    build)
        build_project
        ;;
    pack)
        create_package
        ;;
    test)
        test_package
        ;;
    all)
        run_all
        ;;
    *)
        print_usage
        exit 1
        ;;
esac