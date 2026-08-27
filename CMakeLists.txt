cmake_minimum_required(VERSION 3.20)
project(GenOffice LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include_directories(src)

file(GLOB_RECURSE SOURCES 
    "src/*.cpp"
    "src/*.hpp"
)

add_executable(GenOffice WIN32 ${SOURCES})

target_link_libraries(GenOffice PRIVATE
    gdi32
    user32
    comctl32
    comdlg32
    shell32
    d2d1
    dwrite
    ole32
    windowscodecs
)
