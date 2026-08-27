#ifndef GENOFFICE_UNDO_REDO_STACK_HPP
#ifndef GENOFFICE_UNDO_REDO_STACK_HPP
#define GENOFFICE_UNDO_REDO_STACK_HPP

#include <vector>
#include <functional>
#include <string>

namespace GenOffice {

struct ActionState {
    std::string description;
    std::function<void()> undoAction;
    std::function<void()> redoAction;
};

class UndoRedoStack {
private:
    std::vector<ActionState> undoStack;
    std::vector<ActionState> redoStack;
    size_t maxCapacity = 50;

public:
    void pushAction(const std::string& desc, std::function<void()> undo, std::function<void()> redo) {
        undoStack.push_back({desc, undo, redo});
        if (undoStack.size() > maxCapacity) {
            undoStack.erase(undoStack.begin());
        }
        redoStack.clear();
    }

    bool canUndo() const { return !undoStack.empty(); }
    bool canRedo() const { return !redoStack.empty(); }

    void undo() {
        if (!canUndo()) return;
        ActionState action = undoStack.back();
        undoStack.pop_back();
        if (action.undoAction) action.undoAction();
        redoStack.push_back(action);
    }

    void redo() {
        if (!canRedo()) return;
        ActionState action = redoStack.back();
        redoStack.pop_back();
        if (action.redoAction) action.redoAction();
        undoStack.push_back(action);
    }

    void clear() {
        undoStack.clear();
        redoStack.clear();
    }
};

} // namespace GenOffice

#endif // GENOFFICE_UNDO_REDO_STACK_HPP
