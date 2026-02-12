(function () {
    function requireAuth() {
        if (!window.auth.isLoggedIn()) {
            window.location.href = "/login.html?reason=auth";
            return false;
        }
        return true;
    }

    function requireRole(allowedRoles) {
        if (!requireAuth()) return false;

        const role = window.auth.getRole();
        if (!role || !allowedRoles.includes(role)) {
            window.location.href = "/login.html?reason=forbidden";
            return false;
        }
        return true;
    }

    window.guards = { requireAuth, requireRole };
})();
