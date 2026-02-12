(function () {
    const TOKEN_KEY = "staj2_token";
    const ROLE_KEY = "staj2_role";

    function saveAuth(token, role) {
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(ROLE_KEY, role);
    }

    function clearAuth() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(ROLE_KEY);
    }

    function getToken() {
        return localStorage.getItem(TOKEN_KEY);
    }

    function getRole() {
        return localStorage.getItem(ROLE_KEY);
    }

    function isLoggedIn() {
        return !!getToken();
    }

    function redirectByRole(role) {
        if (role === "Yönetici") window.location.href = "/admin.html";
        else if (role === "Denetleyici") window.location.href = "/auditor.html";
        else window.location.href = "/viewer.html"; // Görüntüleyici
    }

    window.auth = {
        saveAuth,
        clearAuth,
        getToken,
        getRole,
        isLoggedIn,
        redirectByRole
    };
})();
