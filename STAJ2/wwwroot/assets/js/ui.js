// STAJ2/wwwroot/assets/js/ui.js

(function () {
    // Görünürlük yardımcıları
    function show(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = "block";
    }

    function hide(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = "none";
    }

    function setText(id, text) {
        const el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    // Navigasyon: Geriye git veya anasayfaya dön
    function backOrHome() {
        if (window.history.length > 1) {
            window.history.back();
        } else {
            window.location.href = "/index.html";
        }
    }

    // Admin paneli için yardımcı (Eski kodunda vardı, koruyoruz)
    function renderUserRow(user) {
        return `
            <tr>
                <td>${user.username}</td>
                <td>
                    <select id="roleSelect_${user.id}" class="role-select">
                        <option value="1" ${user.role === 'Yönetici' ? 'selected' : ''}>Yönetici</option>
                        <option value="2" ${user.role === 'Denetleyici' ? 'selected' : ''}>Denetleyici</option>
                        <option value="3" ${user.role === 'Görüntüleyici' ? 'selected' : ''}>Görüntüleyici</option>
                    </select>
                </td>
                <td>
                    <button onclick="ui.changeRole(${user.id})" class="btn primary small">💾</button>
                </td>
            </tr>
        `;
    }

    // Rol değiştirme mantığı
    async function changeRole(userId) {
        const newRoleId = document.getElementById(`roleSelect_${userId}`).value;
        try {
            await api.put(`/api/Admin/users/${userId}/change-role`, { newRoleId: parseInt(newRoleId) });
            alert("Kullanıcı rolü başarıyla güncellendi.");
        } catch (e) {
            alert("Hata: " + e.message);
        }
    }

    // Tüm fonksiyonları 'ui' objesi altına toplayıp dışarı açıyoruz
    window.ui = {
        show,
        hide,
        setText,
        backOrHome,
        renderUserRow,
        changeRole
    };
})();