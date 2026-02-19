// STAJ2/wwwroot/assets/js/ui.js

(function () {
    const systemRoles = [
        { id: 1, name: "Yönetici", icon: "bi-person-gear" },
        { id: 2, name: "Denetleyici", icon: "bi-shield-shaded" },
        { id: 3, name: "Görüntüleyici", icon: "bi-eye" }
    ];

    // --- Görünürlük Yardımcıları ---
    function show(id) { const el = document.getElementById(id); if (el) el.style.display = "block"; }
    function hide(id) { const el = document.getElementById(id); if (el) el.style.display = "none"; }
    function setText(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }
    function backOrHome() { if (window.history.length > 1) window.history.back(); else window.location.href = "/login.html"; }

    // --- Tema Yönetimi ---
    function toggleTheme() {
        const html = document.documentElement;
        const currentTheme = html.getAttribute('data-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';

        html.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);

        const icon = document.getElementById('theme-icon');
        if (icon) {
            icon.className = newTheme === 'light' ? 'bi bi-moon-stars-fill' : 'bi bi-sun-fill';
        }
    }

    // --- Sidebar ---
    function renderSidebar(roles) {
        const nav = document.getElementById('main-nav');
        if (!nav) return;
        const isAdmin = roles.includes("Yönetici");

        // YENİ: Bilgisayarlar menüsü ikiye ayrıldı
        let html = `
            <li class="nav-item">
                <a href="javascript:void(0)" id="nav-computers" class="nav-link active" onclick="ui.switchView('computers')">
                    <i class="bi bi-activity text-success"></i> <span>Canlı İzleme</span>
                </a>
            </li>
            <li class="nav-item">
                <a href="javascript:void(0)" id="nav-all-computers" class="nav-link" onclick="ui.switchView('all-computers')">
                    <i class="bi bi-pc-display"></i> <span>Tüm Bilgisayarlar</span>
                </a>
            </li>`;

        if (isAdmin) {
            html += `
                <li class="px-4 mt-4 mb-2"><small class="text-uppercase fw-bold" style="font-size:0.7rem; letter-spacing:1px; color:var(--text-muted);">Yönetim Paneli</small></li>
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-requests" class="nav-link" onclick="ui.switchView('requests')">
                        <i class="bi bi-envelope-paper"></i> <span>Kayıt İstekleri</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-users" class="nav-link" onclick="ui.switchView('users')">
                        <i class="bi bi-people"></i> <span>Kullanıcılar</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-tags" class="nav-link" onclick="ui.switchView('tags')">
                        <i class="bi bi-tags"></i> <span>Etiketler</span>
                    </a>
                </li>`;
        }
        nav.innerHTML = html;
    }

    async function switchView(view) {
        const content = document.getElementById('dynamic-content');
        const title = document.getElementById('view-title');
        const subtitle = document.getElementById('view-subtitle');
        if (!content) return;

        document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
        const activeNav = document.getElementById(`nav-${view}`);
        if (activeNav) activeNav.classList.add('active');

        content.innerHTML = `<div class="d-flex justify-content-center p-5"><div class="spinner-border text-info" role="status"></div></div>`;

        const canEdit = auth.hasRole('Yönetici') || auth.hasRole('Denetleyici');

        switch (view) {
            case 'computers':
                title.innerText = "Canlı İzleme";
                subtitle.innerText = "Sistemdeki cihazların canlı performansı.";
                content.innerHTML = `
                    <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead>
                                    <tr style="color:var(--text-muted);">
                                        <th>Cihaz & Etiketler</th>
                                        <th>IP</th>
                                        <th>CPU</th>
                                        <th>RAM</th>
                                        <th>Diskler</th>
                                        <th>Durum</th>
                                        ${canEdit ? '<th>İşlemler</th>' : ''} 
                                    </tr>
                                </thead>
                                <tbody id="agentRows"></tbody>
                            </table>
                        </div>
                    </div>`;
                if (window.loadAgents) loadAgents();
                break;

            case 'all-computers': // YENİ EKLENEN SAYFA
                title.innerText = "Tüm Bilgisayarlar";
                subtitle.innerText = "Sisteme kayıtlı aktif ve pasif tüm cihazlar.";
                content.innerHTML = `
                    <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead>
                                    <tr style="color:var(--text-muted);">
                                        <th>Cihaz & Etiketler</th>
                                        <th>IP</th>
                                        <th>Son Görülme</th>
                                        <th>Durum</th>
                                        ${canEdit ? '<th>İşlemler</th>' : ''}
                                    </tr>
                                </thead>
                                <tbody id="allComputersRows"></tbody>
                            </table>
                        </div>
                    </div>`;
                if (window.loadAllComputers) loadAllComputers();
                break;

            case 'requests':
                title.innerText = "Kayıt Talepleri";
                subtitle.innerText = "Onay bekleyen yeni kullanıcılar.";
                await loadRequestsView(content);
                break;
            case 'users':
                title.innerText = "Kullanıcı Yönetimi";
                subtitle.innerText = "Rol atama ve kullanıcı silme işlemleri.";
                await loadUsersView(content);
                break;
            case 'tags':
                title.innerText = "Etiket Yönetimi";
                subtitle.innerText = "Cihazları gruplandırmak için etiketler.";
                await loadTagsView(content);
                break;
        }
    }

    // --- Alt Görünümler ---

    async function loadRequestsView(container) {
        try {
            const reqs = await api.get("/api/Admin/requests");

            let rows = reqs.map(r => `
                <tr>
                    <td class="fw-bold">${r.username}</td>
                    <td>
                        <select id="reqRole_${r.id}" class="form-select form-select-sm small-select" style="max-width: 150px; background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                            ${systemRoles.map(x => `<option value="${x.id}" ${x.id == 3 ? 'selected' : ''}>${x.name}</option>`).join("")}
                        </select>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-success" onclick="ui.approveRequest(${r.id})">Onayla</button>
                        <button class="btn btn-sm btn-danger" onclick="ui.rejectRequest(${r.id})">Reddet</button>
                    </td>
                </tr>`).join("");

            container.innerHTML = `
                <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Kullanıcı</th><th>Atanacak Rol</th><th class="text-end">İşlem</th></tr></thead>
                            <tbody>${rows || '<tr><td colspan="3" class="text-center text-muted py-4">Bekleyen kayıt talebi yok.</td></tr>'}</tbody>
                        </table>
                    </div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    async function loadUsersView(container) {
        try {
            const users = await api.get("/api/Admin/users");
            let rows = users.map(u => {
                const roleChecks = systemRoles.map(r => `
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="role_${u.id}_${r.id}" class="user-role-cb-${u.id}" value="${r.id}" ${u.roles.includes(r.name) ? 'checked' : ''}>
                        <label class="form-check-label small" style="color:var(--text-main);" for="role_${u.id}_${r.id}">${r.name}</label>
                    </div>`).join("");
                return `
                    <tr>
                        <td class="fw-bold" style="color:#38bdf8;">${u.username}</td>
                        <td><div class="user-role-group-${u.id}">${roleChecks}</div></td>
                        <td class="text-end">
                            <button class="btn btn-primary btn-sm me-2" onclick="ui.updateUserRoles(${u.id})"><i class="bi bi-save"></i> Kaydet</button>
                            ${!u.roles.includes("Yönetici") ? `<button class="btn btn-outline-danger btn-sm" onclick="ui.deleteUser(${u.id})"><i class="bi bi-trash"></i> Sil</button>` : ""}
                        </td>
                    </tr>`;
            }).join("");
            container.innerHTML = `
                <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Kullanıcı Adı</th><th>Yetkiler</th><th class="text-end">İşlemler</th></tr></thead>
                            <tbody>${rows}</tbody>
                        </table>
                    </div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    async function loadTagsView(container) {
        try {
            const tags = await api.get("/api/Admin/tags");
            let tagPills = tags.map(t => `
                <div class="badge p-2 d-flex align-items-center gap-2 border" style="font-size: 0.9rem; background:var(--bg-hover); color:var(--text-main); border-color:var(--border-color);">
                    ${t.name}
                    <i class="bi bi-x-circle-fill text-danger" style="cursor:pointer" onclick="ui.deleteTag(${t.id})"></i>
                </div>`).join("");
            container.innerHTML = `
                <div class="card p-4 border-0 shadow-sm" style="background:var(--bg-card);">
                    <div class="input-group mb-4" style="max-width: 500px;">
                        <input type="text" id="newTagName" class="form-control" placeholder="Yeni etiket adı giriniz..." style="background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                        <button class="btn btn-primary fw-bold" onclick="ui.createNewTag()">Ekle</button>
                    </div>
                    <h6 class="text-uppercase small fw-bold mb-3" style="color:var(--text-muted);">Mevcut Etiketler</h6>
                    <div class="d-flex flex-wrap gap-2">${tagPills || '<span class="text-muted fst-italic">Henüz etiket eklenmemiş.</span>'}</div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    // --- Fonksiyonları Dışarı Aç (Window.UI) ---
    window.ui = {
        show, hide, setText, backOrHome,
        renderSidebar, switchView, toggleTheme,

        approveRequest: async (id) => {
            if (!confirm("Bu kullanıcıyı onaylamak istiyor musunuz?")) return;
            try {
                const roleId = document.getElementById(`reqRole_${id}`).value;
                await api.post(`/api/admin/requests/approve/${id}`, { newRoleId: parseInt(roleId) });
                alert("Kullanıcı onaylandı.");
                document.getElementById('dynamic-content').innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div></div>';
                setTimeout(() => ui.switchView('requests'), 100);
            } catch (e) { alert(e.message || "Bir hata oluştu."); }
        },

        rejectRequest: async (id) => {
            const reason = prompt("Lütfen ret sebebini giriniz:");
            if (reason === null) return;
            if (reason.length > 200) { alert("Ret sebebi 200 karakterden uzun olamaz."); return; }
            try {
                await api.post(`/api/admin/requests/reject`, { requestId: id, rejectionReason: reason });
                alert("Talep reddedildi.");
                document.getElementById('dynamic-content').innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div></div>';
                setTimeout(() => ui.switchView('requests'), 100);
            } catch (e) { alert(e.message || "Bir hata oluştu."); }
        },

        updateUserRoles: async (userId) => {
            const container = document.querySelector(`.user-role-group-${userId}`);
            const checkedBoxes = container.querySelectorAll('input[type="checkbox"]:checked');
            const roleIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));
            try {
                await api.put(`/api/Admin/users/${userId}/change-roles`, { newRoleIds: roleIds });
                alert("Roller güncellendi.");
                ui.switchView('users');
            } catch (e) { alert(e.message); }
        },

        deleteUser: async (id) => {
            if (confirm("Kullanıcı silinsin mi?")) {
                await api.del(`/api/Admin/users/${id}`);
                ui.switchView('users');
            }
        },

        createNewTag: async () => {
            const name = document.getElementById("newTagName").value.trim();
            if (name) { await api.post("/api/Admin/tags", { name }); ui.switchView('tags'); }
        },
        deleteTag: async (id) => {
            if (confirm("Etiket silinsin mi?")) {
                await api.del(`/api/Admin/tags/${id}`);
                ui.switchView('tags');
            }
        }
    };

    // --- Tema Başlatma (Sayfa Yüklenince) ---
    (function initTheme() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        document.documentElement.setAttribute('data-theme', savedTheme);
    })();

})();