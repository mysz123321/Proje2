// STAJ2/wwwroot/assets/js/auth.js
(function () {
    const TOKEN_KEY = "staj2_token";
    const ROLES_KEY = "staj2_roles";

    window.auth = {
        saveAuth: (token, roles) => {
            localStorage.setItem(TOKEN_KEY, token);
            // Rolleri dizi olarak kaydet
            localStorage.setItem(ROLES_KEY, JSON.stringify(roles));
        },
        clearAuth: () => {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(ROLES_KEY);
        },
        getToken: () => localStorage.getItem(TOKEN_KEY),
        // Rolleri dizi (array) olarak döndürür
        getRoles: () => {
            try {
                const raw = localStorage.getItem(ROLES_KEY);
                return raw ? JSON.parse(raw) : [];
            } catch (e) { return []; }
        },
        // Kullanıcıda bu rol var mı?
        hasRole: (roleName) => {
            const roles = window.auth.getRoles();
            return roles.includes(roleName);
        },
        isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY)
    };
})();