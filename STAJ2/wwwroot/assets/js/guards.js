// STAJ2/wwwroot/assets/js/guards.js
window.guards = {
    requireRole: function (requiredRoles) {
        if (!auth.isLoggedIn()) {
            window.location.href = "/login.html?reason=auth";
            return false;
        }

        const userRoles = auth.getRoles();
        // Gerekli rollerden en az birine sahip mi?
        const hasAccess = requiredRoles.some(r => userRoles.includes(r));

        if (!hasAccess) {
            window.location.href = "/login.html?reason=forbidden";
            return false;
        }
        return true;
    }
};