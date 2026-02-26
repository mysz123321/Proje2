// STAJ2/wwwroot/assets/js/auth.js
(function () {
    const TOKEN_KEY = "staj2_token";
    const ROLES_KEY = "staj2_roles";
    const PERMISSIONS_KEY = "staj2_permissions"; // YENİ: Yetkiler için anahtar kelime eklendi

    window.auth = {
        // YENİ: permissions parametresi eklendi ve localStorage'a kaydediliyor
        saveAuth: (token, roles, permissions) => {
            localStorage.setItem(TOKEN_KEY, token);
            localStorage.setItem(ROLES_KEY, JSON.stringify(roles));
            localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions || []));
        },
        clearAuth: () => {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(ROLES_KEY);
            localStorage.removeItem(PERMISSIONS_KEY); // YENİ: Çıkış yaparken yetkileri de sil
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

        // --- YENİ EKLENEN FONKSİYONLAR BAŞLANGIÇ ---
        getPermissions: () => {
            try {
                const raw = localStorage.getItem(PERMISSIONS_KEY);
                return raw ? JSON.parse(raw) : [];
            } catch (e) { return []; }
        },
        hasPermission: (permissionName) => {
            // Yöneticilerin her şeye yetkisi varsa (bypass etmek istersen) buraya şu satırı ekleyebilirsin:
            // if (window.auth.hasRole("Yönetici")) return true;

            const permissions = window.auth.getPermissions();
            return permissions.includes(permissionName);
        },
        // --- YENİ EKLENEN FONKSİYONLAR BİTİŞ ---

        isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),

        getAuthHeaders: () => {
            const token = localStorage.getItem(TOKEN_KEY);
            return token ? { "Authorization": `Bearer ${token}` } : {};
        }
    };
})();