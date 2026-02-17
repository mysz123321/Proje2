// STAJ2/wwwroot/assets/js/auth.js
(function () {
    const TOKEN_KEY = "staj2_token";
    const ROLES_KEY = "staj2_roles";

    window.auth = {
        saveAuth: (token, roles) => {
            localStorage.setItem(TOKEN_KEY, token);
            localStorage.setItem(ROLES_KEY, JSON.stringify(roles));
        },
        clearAuth: () => {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(ROLES_KEY);
        },
        getToken: () => localStorage.getItem(TOKEN_KEY),
        getRoles: () => {
            try {
                const raw = localStorage.getItem(ROLES_KEY);
                return raw ? JSON.parse(raw) : [];
            } catch (e) { return []; }
        },
        hasRole: (roleName) => {
            const roles = window.auth.getRoles();
            return roles.includes(roleName);
        },
        isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),

        // EKSİK OLAN VE HATAYI ÇÖZEN FONKSİYON:
        getAuthHeaders: () => {
            const token = localStorage.getItem(TOKEN_KEY);
            return token ? { "Authorization": `Bearer ${token}` } : {};
        }
    };
})();