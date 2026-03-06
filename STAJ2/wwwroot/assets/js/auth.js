// STAJ2/wwwroot/assets/js/auth.js
(function () {
    const TOKEN_KEY = "staj2_token";
    let livePermissions = null;
    // JWT Token'ı güvenli okumak için yardımcı fonksiyon (hasPermission'daki mantığının aynısı, Türkçe karakter destekli)
    function decodeTokenSafe() {
        const token = localStorage.getItem(TOKEN_KEY);
        if (!token) return null;
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            return JSON.parse(jsonPayload);
        } catch (e) {
            return null;
        }
    }

    window.auth = {
        saveAuth: (token, roles, permissions, username) => {
            localStorage.setItem(TOKEN_KEY, token);

            // Kullanıcı adını kaydediyoruz
            if (username) {
                localStorage.setItem("staj2_username", username);
            }

            // F12'YE YAZMA KISIMLARI SİLİNDİ! 
            // Eskiden kalanlar varsa temizliyoruz ki F12'de görünmesinler
            localStorage.removeItem("staj2_roles");
            localStorage.removeItem("staj2_permissions");
        },
        clearAuth: () => {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem("staj2_username");
            localStorage.removeItem("staj2_roles");
            localStorage.removeItem("staj2_permissions");
            livePermissions = null;
        },
        getToken: () => localStorage.getItem(TOKEN_KEY),

        getRoles: () => {
            // LocalStorage yerine direkt Token'dan okuyoruz
            const decoded = decodeTokenSafe();
            if (!decoded) return [];
            let userRoles = decoded.role || decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || [];
            return typeof userRoles === 'string' ? [userRoles] : userRoles;
        },
        hasRole: (roleName) => {
            const roles = window.auth.getRoles();
            return roles.includes(roleName);
        },
        loadLivePermissions: async () => {
            try {
                if (window.auth.isLoggedIn()) {
                    livePermissions = await api.get('/api/Ui/my-permissions');
                }
            } catch (error) {
                console.error("Canlı yetkiler çekilemedi, token yetkilerine dönülüyor.", error);
                livePermissions = null;
            }
        },
        getPermissions: () => {
            // LocalStorage yerine direkt Token'dan okuyoruz
            if (livePermissions !== null) {
                return livePermissions;
            }
            const decoded = decodeTokenSafe();
            if (!decoded) return [];
            let userPermissions = decoded.Permission || decoded["Permission"] || [];
            return typeof userPermissions === 'string' ? [userPermissions] : userPermissions;
        },
        hasPermission: function (requiredPermission) {
            const permissions = window.auth.getPermissions();
            return permissions.includes(requiredPermission);
        },
        isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),

        getAuthHeaders: () => {
            const token = localStorage.getItem(TOKEN_KEY);
            return token ? { "Authorization": `Bearer ${token}` } : {};
        }
    };
})();