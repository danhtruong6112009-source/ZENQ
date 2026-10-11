#include "PieceTable.hpp"
#include <algorithm>

namespace ZenQOffice {

PieceTable::PieceTable() {
    clear();
}

PieceTable::PieceTable(const std::string& initialText) {
    setInitialText(initialText);
}

void PieceTable::clear() {
    originalBuffer.clear();
    addBuffer.clear();
    pieces.clear();
}

void PieceTable::setInitialText(const std::string& text) {
    originalBuffer = text;
    addBuffer.clear();
    pieces.clear();
    if (!originalBuffer.empty()) {
        pieces.emplace_back(BufferType::Original, 0, originalBuffer.length());
    }
}

size_t PieceTable::getLength() const {
    size_t total = 0;
    for (const auto& piece : pieces) {
        total += piece.length;
    }
    return total;
}

std::pair<size_t, size_t> PieceTable::findPieceIndex(size_t offset) const {
    size_t accumulated = 0;
    for (size_t i = 0; i < pieces.size(); ++i) {
        if (offset <= accumulated + pieces[i].length) {
            return {i, offset - accumulated};
        }
        accumulated += pieces[i].length;
    }
    return {pieces.size(), 0};
}

void PieceTable::insert(size_t index, const std::string& text) {
    if (text.empty()) return;

    size_t textLen = text.length();
    size_t addOffset = addBuffer.length();
    addBuffer.append(text);

    if (pieces.empty()) {
        pieces.emplace_back(BufferType::Add, addOffset, textLen);
        return;
    }

    index = (std::min)(index, getLength());
    auto [pieceIdx, pieceOffset] = findPieceIndex(index);

    if (pieceIdx >= pieces.size()) {
        pieces.emplace_back(BufferType::Add, addOffset, textLen);
        return;
    }

    Piece targetPiece = pieces[pieceIdx];

    if (pieceOffset == 0) {
        pieces.insert(pieces.begin() + pieceIdx, Piece(BufferType::Add, addOffset, textLen));
    } else if (pieceOffset == targetPiece.length) {
        pieces.insert(pieces.begin() + pieceIdx + 1, Piece(BufferType::Add, addOffset, textLen));
    } else {
        Piece leftPiece(targetPiece.bufferType, targetPiece.start, pieceOffset);
        Piece rightPiece(targetPiece.bufferType, targetPiece.start + pieceOffset, targetPiece.length - pieceOffset);

        pieces[pieceIdx] = leftPiece;
        pieces.insert(pieces.begin() + pieceIdx + 1, Piece(BufferType::Add, addOffset, textLen));
        pieces.insert(pieces.begin() + pieceIdx + 2, rightPiece);
    }
}

void PieceTable::remove(size_t index, size_t length) {
    if (length == 0 || pieces.empty()) return;

    size_t totalLen = getLength();
    if (index >= totalLen) return;
    length = (std::min)(length, totalLen - index);

    size_t remainingToRemove = length;
    while (remainingToRemove > 0 && index < getLength()) {
        auto [pieceIdx, pieceOffset] = findPieceIndex(index);
        if (pieceIdx >= pieces.size()) break;

        Piece& p = pieces[pieceIdx];
        size_t availableInPiece = p.length - pieceOffset;
        size_t toRemoveHere = (std::min)(remainingToRemove, availableInPiece);

        if (pieceOffset == 0 && toRemoveHere == p.length) {
            pieces.erase(pieces.begin() + pieceIdx);
        } else if (pieceOffset == 0) {
            p.start += toRemoveHere;
            p.length -= toRemoveHere;
        } else if (pieceOffset + toRemoveHere == p.length) {
            p.length -= toRemoveHere;
        } else {
            Piece rightPiece(p.bufferType, p.start + pieceOffset + toRemoveHere, p.length - (pieceOffset + toRemoveHere));
            p.length = pieceOffset;
            pieces.insert(pieces.begin() + pieceIdx + 1, rightPiece);
        }

        remainingToRemove -= toRemoveHere;
    }
}

std::string PieceTable::getText() const {
    std::string result;
    result.reserve(getLength());

    for (const auto& piece : pieces) {
        const std::string& src = (piece.bufferType == BufferType::Original) ? originalBuffer : addBuffer;
        result.append(src, piece.start, piece.length);
    }
    return result;
}

} // namespace ZenQOffice
