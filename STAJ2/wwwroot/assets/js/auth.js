// STAJ2/wwwroot/assets/js/auth.js
(function () {
    const TOKEN_KEY = "staj2_token";
    const ROLES_KEY = "staj2_roles";
    const PERMISSIONS_KEY = "staj2_permissions";

    window.auth = {
        // YENİ: 4. parametre olarak 'username' eklendi
        saveAuth: (token, roles, permissions, username) => {
            localStorage.setItem(TOKEN_KEY, token);
            localStorage.setItem(ROLES_KEY, JSON.stringify(roles));
            localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions || []));

            // Kullanıcı adını da tam bu anda kaydediyoruz
            if (username) {
                localStorage.setItem("staj2_username", username);
            }
        },
        clearAuth: () => {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(ROLES_KEY);
            localStorage.removeItem(PERMISSIONS_KEY);
            localStorage.removeItem("staj2_username"); // Çıkışta temizlenir
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
        getPermissions: () => {
            try {
                const raw = localStorage.getItem(PERMISSIONS_KEY);
                return raw ? JSON.parse(raw) : [];
            } catch (e) { return []; }
        },
        hasPermission: (permissionName) => {
            const permissions = window.auth.getPermissions();
            return permissions.includes(permissionName);
        },
        isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),

        getAuthHeaders: () => {
            const token = localStorage.getItem(TOKEN_KEY);
            return token ? { "Authorization": `Bearer ${token}` } : {};
        }
    };
})();