// Basit fetch wrapper: otomatik JSON, hata mesajı, auth header
(function () {
    function getToken() {
        return localStorage.getItem("staj2_token");
    }

    async function request(path, options = {}) {
        const base = window.APP_CONFIG?.API_BASE ?? "";
        const url = base + path;

        const headers = options.headers ? { ...options.headers } : {};
        if (!headers["Content-Type"] && options.body) headers["Content-Type"] = "application/json";

        const token = getToken();
        if (token) headers["Authorization"] = `Bearer ${token}`;

        const res = await fetch(url, { ...options, headers });

        const contentType = res.headers.get("content-type") || "";
        const isJson = contentType.includes("application/json");

        const data = isJson ? await res.json().catch(() => null) : await res.text().catch(() => "");

        if (!res.ok) {
            const msg = (typeof data === "string" && data) ? data
                : (data && data.message) ? data.message
                    : `HTTP ${res.status}`;
            throw new Error(msg);
        }

        return data;
    }

    window.api = {
        get: (path) => request(path),
        post: (path, body) => request(path, { method: "POST", body: JSON.stringify(body) }),
        del: (path) => request(path, { method: "DELETE" })
    };
})();
