#include "FormulaEngine.hpp"
#include <sstream>
#include <algorithm>
#include <cctype>
#include <cmath>
#include <iostream>

namespace ZenQOffice {

static std::string trim(const std::string& str) {
    size_t first = str.find_first_not_of(" \t\r\n");
    if (first == std::string::npos) return "";
    size_t last = str.find_last_not_of(" \t\r\n");
    return str.substr(first, (last - first + 1));
}

static std::string toUpper(const std::string& str) {
    std::string res = str;
    for (char& c : res) c = static_cast<char>(std::toupper(static_cast<unsigned char>(c)));
    return res;
}

std::vector<std::pair<uint32_t, uint32_t>> FormulaEngine::parseRange(const std::string& rangeStr) {
    std::vector<std::pair<uint32_t, uint32_t>> cells;
    size_t colonPos = rangeStr.find(':');
    if (colonPos == std::string::npos) {
        cells.push_back(SparseMatrix::parseCellAddress(rangeStr));
        return cells;
    }

    std::string startAddr = rangeStr.substr(0, colonPos);
    std::string endAddr = rangeStr.substr(colonPos + 1);

    auto [r1, c1] = SparseMatrix::parseCellAddress(startAddr);
    auto [r2, c2] = SparseMatrix::parseCellAddress(endAddr);

    uint32_t minR = (std::min)(r1, r2);
    uint32_t maxR = (std::max)(r1, r2);
    uint32_t minC = (std::min)(c1, c2);
    uint32_t maxC = (std::max)(c1, c2);

    for (uint32_t r = minR; r <= maxR; ++r) {
        for (uint32_t c = minC; c <= maxC; ++c) {
            cells.push_back({r, c});
        }
    }
    return cells;
}

std::vector<std::string> FormulaEngine::splitArgs(const std::string& argsStr) {
    std::vector<std::string> args;
    std::string current;
    int parenDepth = 0;
    bool inQuote = false;

    for (char c : argsStr) {
        if (c == '"') {
            inQuote = !inQuote;
            current += c;
        } else if (c == '(' && !inQuote) {
            parenDepth++;
            current += c;
        } else if (c == ')' && !inQuote) {
            parenDepth--;
            current += c;
        } else if (c == ',' && parenDepth == 0 && !inQuote) {
            args.push_back(trim(current));
            current.clear();
        } else {
            current += c;
        }
    }
    if (!current.empty()) {
        args.push_back(trim(current));
    }
    return args;
}

CellData FormulaEngine::evaluateFormula(const std::string& formula, const SparseMatrix& sheet) {
    CellData result;
    result.type = CellType::Formula;
    result.formulaStr = formula;

    std::string expr = trim(formula);
    if (expr.empty() || expr[0] != '=') {
        result.setString(formula);
        return result;
    }

    expr = trim(expr.substr(1)); // Remove '='
    std::string upperExpr = toUpper(expr);

    // Check Function Calls: SUM, AVERAGE, COUNT, MIN, MAX, IF, CONCATENATE
    size_t openParen = expr.find('(');
    size_t closeParen = expr.rfind(')');
    if (openParen != std::string::npos && closeParen != std::string::npos && closeParen > openParen) {
        std::string funcName = toUpper(trim(expr.substr(0, openParen)));
        std::string argsStr = expr.substr(openParen + 1, closeParen - openParen - 1);
        std::vector<std::string> args = splitArgs(argsStr);

        if (funcName == "SUM") {
            double sum = 0.0;
            for (const auto& arg : args) {
                auto cells = parseRange(arg);
                for (auto [r, c] : cells) {
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell) sum += cell->numericValue;
                }
            }
            result.setNumber(sum);
            return result;
        } else if (funcName == "AVERAGE") {
            double sum = 0.0;
            size_t count = 0;
            for (const auto& arg : args) {
                auto cells = parseRange(arg);
                for (auto [r, c] : cells) {
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell) {
                        sum += cell->numericValue;
                        count++;
                    }
                }
            }
            result.setNumber(count > 0 ? (sum / count) : 0.0);
            return result;
        } else if (funcName == "COUNT") {
            size_t count = 0;
            for (const auto& arg : args) {
                auto cells = parseRange(arg);
                for (auto [r, c] : cells) {
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell && cell->type == CellType::Number) count++;
                }
            }
            result.setNumber(static_cast<double>(count));
            return result;
        } else if (funcName == "MIN") {
            double minVal = 1e308;
            bool found = false;
            for (const auto& arg : args) {
                auto cells = parseRange(arg);
                for (auto [r, c] : cells) {
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell) {
                        minVal = (std::min)(minVal, cell->numericValue);
                        found = true;
                    }
                }
            }
            result.setNumber(found ? minVal : 0.0);
            return result;
        } else if (funcName == "MAX") {
            double maxVal = -1e308;
            bool found = false;
            for (const auto& arg : args) {
                auto cells = parseRange(arg);
                for (auto [r, c] : cells) {
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell) {
                        maxVal = (std::max)(maxVal, cell->numericValue);
                        found = true;
                    }
                }
            }
            result.setNumber(found ? maxVal : 0.0);
            return result;
        } else if (funcName == "CONCATENATE") {
            std::string concatRes;
            for (const auto& arg : args) {
                if (!arg.empty() && arg.front() == '"' && arg.back() == '"') {
                    concatRes += arg.substr(1, arg.length() - 2);
                } else {
                    auto [r, c] = SparseMatrix::parseCellAddress(arg);
                    const CellData* cell = sheet.getCell(r, c);
                    if (cell) concatRes += cell->evaluatedStr;
                }
            }
            result.setString(concatRes);
            return result;
        } else if (funcName == "IF" && args.size() >= 2) {
            double condVal = parseAndEvaluateNumeric(args[0], sheet);
            if (condVal != 0.0) {
                return evaluateFormula("=" + args[1], sheet);
            } else if (args.size() >= 3) {
                return evaluateFormula("=" + args[2], sheet);
            } else {
                result.setNumber(0.0);
                return result;
            }
        }
    }

    // Try evaluating arithmetic formula: e.g. A1 + B1, A1 * 5
    double val = parseAndEvaluateNumeric(expr, sheet);
    result.setNumber(val);
    return result;
}

double FormulaEngine::parseAndEvaluateNumeric(const std::string& expr, const SparseMatrix& sheet) {
    std::string cleanExpr = trim(expr);
    if (cleanExpr.empty()) return 0.0;

    // Check if simple cell reference like "A1"
    bool isCellRef = true;
    for (char c : cleanExpr) {
        if (!std::isalnum(static_cast<unsigned char>(c))) {
            isCellRef = false;
            break;
        }
    }
    if (isCellRef) {
        auto [r, c] = SparseMatrix::parseCellAddress(cleanExpr);
        const CellData* cell = sheet.getCell(r, c);
        return cell ? cell->numericValue : 0.0;
    }

    // Handle basic operator '+' '-' '*' '/'
    size_t plusPos = cleanExpr.find('+');
    if (plusPos != std::string::npos) {
        return parseAndEvaluateNumeric(cleanExpr.substr(0, plusPos), sheet) +
               parseAndEvaluateNumeric(cleanExpr.substr(plusPos + 1), sheet);
    }
    size_t mulPos = cleanExpr.find('*');
    if (mulPos != std::string::npos) {
        return parseAndEvaluateNumeric(cleanExpr.substr(0, mulPos), sheet) *
               parseAndEvaluateNumeric(cleanExpr.substr(mulPos + 1), sheet);
    }

    // Try direct number conversion
    try {
        return std::stod(cleanExpr);
    } catch (...) {
        return 0.0;
    }
}

void FormulaEngine::evaluateSheet(SparseMatrix& sheet) {
    auto cells = sheet.getAllCells();
    for (const auto& [key, cell] : cells) {
        if (cell.type == CellType::Formula) {
            auto [r, c] = SparseMatrix::decodeKey(key);
            CellData evalRes = evaluateFormula(cell.formulaStr, sheet);
            CellData& targetCell = sheet.getOrCreateCell(r, c);
            targetCell.numericValue = evalRes.numericValue;
            targetCell.evaluatedStr = evalRes.evaluatedStr;
        }
    }
}

} // namespace ZenQOffice
