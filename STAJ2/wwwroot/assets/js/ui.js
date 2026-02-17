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

    // --- Sidebar ---
    function renderSidebar(roles) {
        const nav = document.getElementById('main-nav');
        if (!nav) return;
        const isAdmin = roles.includes("Yönetici");

        let html = `
            <li class="nav-item">
                <a href="javascript:void(0)" id="nav-computers" class="nav-link active" onclick="ui.switchView('computers')">
                    <i class="bi bi-cpu"></i> <span>Bilgisayarlar</span>
                </a>
            </li>`;

        if (isAdmin) {
            html += `
                <li class="px-4 mt-4 mb-2"><small class="text-uppercase text-secondary fw-bold" style="font-size:0.7rem; letter-spacing:1px;">Yönetim Paneli</small></li>
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

        switch (view) {
            case 'computers':
                title.innerText = "Bilgisayarlar";
                subtitle.innerText = "Sistemdeki tüm cihazların canlı performansı.";
                content.innerHTML = `
                    <div class="card bg-dark border-secondary shadow-sm">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead class="table-dark">
                                    <tr>
                                        <th>Cihaz & Etiketler</th>
                                        <th>IP</th>
                                        <th>CPU</th>
                                        <th>RAM</th>
                                        <th>Diskler</th>
                                        <th>Güncelleme</th>
                                        ${auth.hasRole('Yönetici') ? '<th>İşlemler</th>' : ''}
                                    </tr>
                                </thead>
                                <tbody id="agentRows" class="text-light"></tbody>
                            </table>
                        </div>
                    </div>`;
                if (window.loadAgents) loadAgents();
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

    // --- Alt Görünümler (Renk düzeltmeleri yapıldı) ---

    async function loadRequestsView(container) {
        try {
            const reqs = await api.get("/api/Admin/requests");
            let rows = reqs.map(r => `
                <tr>
                    <td class="fw-bold">${r.username}</td>
                    <td>
                        <select id="reqRole_${r.id}" class="form-select form-select-sm text-white bg-dark border-secondary" style="max-width: 150px;">
                            ${systemRoles.map(x => `<option value="${x.id}" ${x.id == 3 ? 'selected' : ''}>${x.name}</option>`).join("")}
                        </select>
                    </td>
                    <td class="text-end">
                        <button class="btn btn-success btn-sm text-white" onclick="ui.approveReq(${r.id})"><i class="bi bi-check-lg"></i> Onayla</button>
                    </td>
                </tr>`).join("");

            container.innerHTML = `
                <div class="card bg-dark border-secondary">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-dark"><tr><th>Kullanıcı</th><th>Atanacak Rol</th><th class="text-end">İşlem</th></tr></thead>
                            <tbody class="text-light">${rows || '<tr><td colspan="3" class="text-center text-muted py-4">Bekleyen kayıt talebi yok.</td></tr>'}</tbody>
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
                        <label class="form-check-label text-light small" for="role_${u.id}_${r.id}">${r.name}</label>
                    </div>`).join("");
                return `
                    <tr>
                        <td class="fw-bold text-info">${u.username}</td>
                        <td><div class="user-role-group-${u.id}">${roleChecks}</div></td>
                        <td class="text-end">
                            <button class="btn btn-primary btn-sm me-2" onclick="ui.updateUserRoles(${u.id})"><i class="bi bi-save"></i> Kaydet</button>
                            ${!u.roles.includes("Yönetici") ? `<button class="btn btn-outline-danger btn-sm" onclick="ui.deleteUser(${u.id})"><i class="bi bi-trash"></i> Sil</button>` : ""}
                        </td>
                    </tr>`;
            }).join("");
            container.innerHTML = `
                <div class="card bg-dark border-secondary">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-dark"><tr><th>Kullanıcı Adı</th><th>Yetkiler</th><th class="text-end">İşlemler</th></tr></thead>
                            <tbody class="text-light">${rows}</tbody>
                        </table>
                    </div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    async function loadTagsView(container) {
        try {
            const tags = await api.get("/api/Admin/tags");
            let tagPills = tags.map(t => `
                <div class="badge bg-secondary p-2 d-flex align-items-center gap-2 border border-secondary" style="font-size: 0.9rem;">
                    ${t.name}
                    <i class="bi bi-x-circle-fill text-danger" style="cursor:pointer" onclick="ui.deleteTag(${t.id})"></i>
                </div>`).join("");
            container.innerHTML = `
                <div class="card bg-dark border-secondary p-4">
                    <div class="input-group mb-4" style="max-width: 500px;">
                        <input type="text" id="newTagName" class="form-control" placeholder="Yeni etiket adı giriniz...">
                        <button class="btn btn-info text-white fw-bold" onclick="ui.createNewTag()">Ekle</button>
                    </div>
                    <h6 class="text-muted text-uppercase small fw-bold mb-3">Mevcut Etiketler</h6>
                    <div class="d-flex flex-wrap gap-2">${tagPills || '<span class="text-muted fst-italic">Henüz etiket eklenmemiş.</span>'}</div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    // --- Fonksiyonları Dışarı Aç ---
    window.ui = {
        show, hide, setText, backOrHome,
        renderSidebar, switchView,
        approveReq: async (id) => {
            const roleId = document.getElementById(`reqRole_${id}`).value;
            await api.post(`/api/Admin/approve/${id}`, { roleId: parseInt(roleId) });
            ui.switchView('requests');
        },
        updateUserRoles: async (userId) => {
            // Checkbox toplama mantığını düzeltiyoruz: name veya class kullanmak yerine container içinden bulalım
            const container = document.querySelector(`.user-role-group-${userId}`);
            const checkedBoxes = container.querySelectorAll('input[type="checkbox"]:checked');
            const roleIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            try { await api.put(`/api/Admin/users/${userId}/change-roles`, { newRoleIds: roleIds }); alert("Roller güncellendi."); ui.switchView('users'); } catch (e) { alert(e.message); }
        },
        deleteUser: async (id) => { if (confirm("Kullanıcı silinsin mi?")) { await api.del(`/api/Admin/users/${id}`); ui.switchView('users'); } },
        createNewTag: async () => {
            const name = document.getElementById("newTagName").value.trim();
            if (name) { await api.post("/api/Admin/tags", { name }); ui.switchView('tags'); }
        },
        deleteTag: async (id) => { if (confirm("Etiket silinsin mi?")) { await api.del(`/api/Admin/tags/${id}`); ui.switchView('tags'); } }
    };
})();