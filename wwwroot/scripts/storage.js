export function get(key) {
    const json = localStorage.getItem(key);
    if (!json) return null;
    try {
        return JSON.parse(json);
    } catch {
        return null;
    }
}

export function set(key, value) {
    localStorage.setItem(key, JSON.stringify(value));
}

export function remove(key) {
    localStorage.removeItem(key);
}
