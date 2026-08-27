#ifndef GENOFFICE_PIECE_TABLE_HPP
#ifndef GENOFFICE_PIECE_TABLE_HPP
#define GENOFFICE_PIECE_TABLE_HPP

#include <string>
#include <vector>
#include <cstdint>

namespace GenOffice {

enum class BufferType { Original, Add };

struct Piece {
    BufferType bufferType;
    size_t start;
    size_t length;

    Piece(BufferType type, size_t s, size_t len) 
        : bufferType(type), start(s), length(len) {}
};

class PieceTable {
private:
    std::string originalBuffer;
    std::string addBuffer;
    std::vector<Piece> pieces;

public:
    PieceTable();
    explicit PieceTable(const std::string& initialText);

    void setInitialText(const std::string& text);
    void insert(size_t index, const std::string& text);
    void remove(size_t index, size_t length);
    
    std::string getText() const;
    size_t getLength() const;
    void clear();

private:
    std::pair<size_t, size_t> findPieceIndex(size_t offset) const;
};

} // namespace GenOffice

#endif // GENOFFICE_PIECE_TABLE_HPP
