#include "SparseMatrix.hpp"
#include <cctype>
#include <algorithm>

namespace GenOffice {

bool SparseMatrix::hasCell(uint32_t row, uint32_t col) const {
    return grid.find(encodeKey(row, col)) != grid.end();
}

const CellData* SparseMatrix::getCell(uint32_t row, uint32_t col) const {
    auto it = grid.find(encodeKey(row, col));
    if (it != grid.end()) return &(it->second);
    return nullptr;
}

CellData& SparseMatrix::getOrCreateCell(uint32_t row, uint32_t col) {
    return grid[encodeKey(row, col)];
}

void SparseMatrix::setCellText(uint32_t row, uint32_t col, const std::string& text) {
    CellData& cell = getOrCreateCell(row, col);
    if (text.empty()) {
        clearCell(row, col);
        return;
    }
    if (text[0] == '=') {
        cell.setFormula(text);
    } else {
        try {
            size_t idx = 0;
            double val = std::stod(text, &idx);
            if (idx == text.length()) {
                cell.setNumber(val);
                return;
            }
        } catch (...) {}
        cell.setString(text);
    }
}

void SparseMatrix::clearCell(uint32_t row, uint32_t col) {
    grid.erase(encodeKey(row, col));
}

void SparseMatrix::clear() {
    grid.clear();
}

std::string SparseMatrix::colIndexToName(uint32_t col) {
    std::string name;
    while (col > 0) {
        uint32_t rem = (col - 1) % 26;
        name.insert(name.begin(), static_cast<char>('A' + rem));
        col = (col - 1) / 26;
    }
    return name.empty() ? "A" : name;
}

uint32_t SparseMatrix::colNameToIndex(const std::string& colName) {
    uint32_t col = 0;
    for (char c : colName) {
        if (std::isalpha(static_cast<unsigned char>(c))) {
            col = col * 26 + (std::toupper(static_cast<unsigned char>(c)) - 'A' + 1);
        }
    }
    return col;
}

std::pair<uint32_t, uint32_t> SparseMatrix::parseCellAddress(const std::string& addr) {
    std::string colStr;
    std::string rowStr;
    for (char c : addr) {
        if (std::isalpha(static_cast<unsigned char>(c))) {
            colStr += static_cast<char>(std::toupper(static_cast<unsigned char>(c)));
        } else if (std::isdigit(static_cast<unsigned char>(c))) {
            rowStr += c;
        }
    }
    uint32_t col = colNameToIndex(colStr);
    uint32_t row = rowStr.empty() ? 1 : static_cast<uint32_t>(std::stoul(rowStr));
    return {row, col};
}

} // namespace GenOffice
