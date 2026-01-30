const STORAGE_KEY = "zoa-nav-order";

export function loadOrder() {
    const json = localStorage.getItem(STORAGE_KEY);
    if (!json) return null;
    try {
        return JSON.parse(json);
    } catch {
        return null;
    }
}

export function saveOrder(order) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(order));
}

export function enableDragDrop(ulElement, dotnetRef) {
    let draggedLi = null;
    let reorderEnabled = false;

    const getLis = () => Array.from(ulElement.querySelectorAll("li[data-nav-key]"));

    const clearIndicators = () => {
        getLis().forEach(el => {
            el.style.borderLeft = "";
            el.style.borderRight = "";
        });
    };

    ulElement.addEventListener("dragstart", (e) => {
        if (!reorderEnabled) { e.preventDefault(); return; }
        const li = e.target.closest("li[data-nav-key]");
        if (!li) return;
        draggedLi = li;
        li.style.opacity = "0.3";
        e.dataTransfer.effectAllowed = "move";
    });

    ulElement.addEventListener("dragend", (e) => {
        const li = e.target.closest("li[data-nav-key]");
        if (li) li.style.opacity = "";
        draggedLi = null;
        clearIndicators();
    });

    ulElement.addEventListener("dragover", (e) => {
        if (!reorderEnabled) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = "move";

        const li = e.target.closest("li[data-nav-key]");
        if (!li || li === draggedLi) return;

        clearIndicators();

        const rect = li.getBoundingClientRect();
        const midX = rect.left + rect.width / 2;
        if (e.clientX < midX) {
            li.style.borderLeft = "2px solid #ea580c";
        } else {
            li.style.borderRight = "2px solid #ea580c";
        }
    });

    ulElement.addEventListener("dragleave", (e) => {
        const li = e.target.closest("li[data-nav-key]");
        if (li) {
            li.style.borderLeft = "";
            li.style.borderRight = "";
        }
    });

    ulElement.addEventListener("drop", (e) => {
        if (!reorderEnabled) return;
        e.preventDefault();
        const targetLi = e.target.closest("li[data-nav-key]");
        if (!targetLi || !draggedLi || targetLi === draggedLi) return;

        clearIndicators();

        // Compute new order without touching the DOM — let Blazor re-render
        const currentOrder = getLis().map(li => li.getAttribute("data-nav-key"));
        const draggedKey = draggedLi.getAttribute("data-nav-key");
        const targetKey = targetLi.getAttribute("data-nav-key");

        const without = currentOrder.filter(k => k !== draggedKey);
        const targetIndex = without.indexOf(targetKey);

        const rect = targetLi.getBoundingClientRect();
        const midX = rect.left + rect.width / 2;
        const insertIndex = e.clientX < midX ? targetIndex : targetIndex + 1;

        without.splice(insertIndex, 0, draggedKey);

        saveOrder(without);
        dotnetRef.invokeMethodAsync("OnOrderChanged", without);
    });

    return {
        setReorderEnabled(enabled) { reorderEnabled = enabled; }
    };
}

export function setDraggable(handle, ulElement, enabled) {
    handle.setReorderEnabled(enabled);
    ulElement.querySelectorAll("li[data-nav-key]").forEach(li => {
        if (enabled) {
            li.setAttribute("draggable", "true");
            li.style.cursor = "grab";
        } else {
            li.removeAttribute("draggable");
            li.style.cursor = "";
        }
    });
}
