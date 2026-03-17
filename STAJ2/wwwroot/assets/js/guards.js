// STAJ2/wwwroot/assets/js/guards.js
window.guards = {
    // Sadece giriş yapmış mı diye bakan fonksiyon
    requireAuth: function () {
        if (!auth.isLoggedIn()) {
            window.location.href = "/login.html?reason=auth";
            return false;
        }
        return true;
    },

    // Rolleri umursamayıp sadece bir rolü var mı diye bakan esnek fonksiyon
    requireRole: function (requiredRoles) {
        if (!auth.isLoggedIn()) {
            window.location.href = "/login.html?reason=auth";
            return false;
        }

        const userRoles = auth.getRoles();
        if (!userRoles || userRoles.length === 0) {
            window.location.href = "/login.html?reason=forbidden";
            return false;
        }
        return true;
    }
};