echo "what type of build do you want? available: debug / release"
read build
make $build
dotnet build -c $build