#ifndef GENOFFICE_FORMULA_ENGINE_HPP
#ifndef GENOFFICE_FORMULA_ENGINE_HPP
#define GENOFFICE_FORMULA_ENGINE_HPP

#include "SparseMatrix.hpp"
#include <string>
#include <vector>
#include <unordered_map>
#include <unordered_set>

namespace GenOffice {

class FormulaEngine {
public:
    static void evaluateSheet(SparseMatrix& sheet);
    static CellData evaluateFormula(const std::string& formula, const SparseMatrix& sheet);

private:
    static double parseAndEvaluateNumeric(const std::string& expr, const SparseMatrix& sheet);
    static std::vector<std::pair<uint32_t, uint32_t>> parseRange(const std::string& rangeStr);
    static std::vector<std::string> splitArgs(const std::string& argsStr);
};

} // namespace GenOffice

#endif // GENOFFICE_FORMULA_ENGINE_HPP
