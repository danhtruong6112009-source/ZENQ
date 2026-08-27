#ifndef GENOFFICE_SPARSE_MATRIX_HPP
#ifndef GENOFFICE_SPARSE_MATRIX_HPP
#define GENOFFICE_SPARSE_MATRIX_HPP

#include <string>
#include <unordered_map>
#include <cstdint>

namespace GenOffice {

enum class CellType { Empty, String, Number, Formula, Boolean, Error };

struct CellFormat {
    std::wstring fontName = L"Segoe UI";
    float fontSize = 11.0f;
    bool bold = false;
    bool italic = false;
    uint32_t textColor = 0x000000;
    uint32_t bgColor = 0xFFFFFF;
    int alignment = 0; // 0: Left/Auto, 1: Center, 2: Right
    std::string numberFormat = "General"; // "General", "Currency", "Percentage", "Integer"
};

struct CellData {
    CellType type = CellType::Empty;
    std::string rawValue;
    std::string formulaStr;
    double numericValue = 0.0;
    std::string evaluatedStr;
    CellFormat format;

    void setString(const std::string& val) {
        type = CellType::String;
        rawValue = val;
        formulaStr.clear();
        evaluatedStr = val;
    }

    void setNumber(double val) {
        type = CellType::Number;
        numericValue = val;
        rawValue = std::to_string(val);
        formulaStr.clear();
        evaluatedStr = rawValue;
    }

    void setFormula(const std::string& fStr) {
        type = CellType::Formula;
        formulaStr = fStr;
        rawValue = fStr;
    }
};

class SparseMatrix {
private:
    std::unordered_map<uint64_t, CellData> grid;

public:
    static uint64_t encodeKey(uint32_t row, uint32_t col) {
        return (static_cast<uint64_t>(row) << 32) | static_cast<uint64_t>(col);
    }

    static std::pair<uint32_t, uint32_t> decodeKey(uint64_t key) {
        return { static_cast<uint32_t>(key >> 32), static_cast<uint32_t>(key & 0xFFFFFFFF) };
    }

    bool hasCell(uint32_t row, uint32_t col) const;
    const CellData* getCell(uint32_t row, uint32_t col) const;
    CellData& getOrCreateCell(uint32_t row, uint32_t col);
    void setCellText(uint32_t row, uint32_t col, const std::string& text);
    void clearCell(uint32_t row, uint32_t col);
    void clear();

    const std::unordered_map<uint64_t, CellData>& getAllCells() const { return grid; }
    
    static std::string colIndexToName(uint32_t col);
    static uint32_t colNameToIndex(const std::string& colName);
    static std::pair<uint32_t, uint32_t> parseCellAddress(const std::string& addr);
};

} // namespace GenOffice

#endif // GENOFFICE_SPARSE_MATRIX_HPP
