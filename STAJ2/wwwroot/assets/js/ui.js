// STAJ2/wwwroot/assets/js/ui.js

(function () {
    // --- Görünürlük Yardımcıları ---
    function show(id) { const el = document.getElementById(id); if (el) el.style.display = "block"; }
    function hide(id) { const el = document.getElementById(id); if (el) el.style.display = "none"; }
    function setText(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }
    function backOrHome() { if (window.history.length > 1) window.history.back(); else window.location.href = "/login.html"; }

    function toggleTheme() {
        const html = document.documentElement;
        const currentTheme = html.getAttribute('data-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';

        html.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);

        const icon = document.getElementById('theme-icon');
        if (icon) {
            icon.classList.add('icon-spin-out');
            setTimeout(() => {
                icon.className = newTheme === 'light' ? 'bi bi-moon-stars-fill' : 'bi bi-sun-fill';
                icon.classList.remove('icon-spin-out');
                icon.classList.add('icon-spin-in');
                setTimeout(() => icon.classList.remove('icon-spin-in'), 400);
            }, 200);
        }
    }

    // --- Sidebar ---
    function renderSidebar() {
        const nav = document.getElementById('main-nav');
        if (!nav) return;

        // Yetki kontrolleri
        const canManageUsers = window.auth.hasPermission("User.Manage");
        const canManageTags = window.auth.hasPermission("Tag.Manage");
        const canManageRoles = window.auth.hasPermission("Role.Manage"); // YENİ EKLENDİ

        const hasAdminPanel = canManageUsers || canManageTags || canManageRoles;

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

        if (hasAdminPanel) {
            html += `
                <li class="px-4 mt-4 mb-2"><small class="text-uppercase fw-bold" style="font-size:0.7rem; letter-spacing:1px; color:var(--text-muted);">Yönetim Paneli</small></li>`;

            if (canManageUsers) {
                html += `
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-requests" class="nav-link" onclick="ui.switchView('requests')">
                        <i class="bi bi-envelope-paper"></i> <span>Kayıt İstekleri</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-users" class="nav-link" onclick="ui.switchView('users')">
                        <i class="bi bi-people"></i> <span>Kullanıcılar</span>
                    </a>
                </li>`;
            }

            if (canManageRoles) {
                html += `
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-roles" class="nav-link" onclick="ui.switchView('roles')">
                        <i class="bi bi-shield-lock"></i> <span>Roller ve Yetkiler</span>
                    </a>
                </li>`;
            }

            if (canManageTags) {
                html += `
                <li class="nav-item">
                    <a href="javascript:void(0)" id="nav-tags" class="nav-link" onclick="ui.switchView('tags')">
                        <i class="bi bi-tags"></i> <span>Etiketler</span>
                    </a>
                </li>`;
            }
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

        const canEdit = window.auth.hasPermission('Computer.Delete') ||
            window.auth.hasPermission('Computer.Rename') ||
            window.auth.hasPermission('Computer.SetThreshold') ||
            window.auth.hasPermission('Computer.AssignTag');

        switch (view) {
            case 'computers':
                title.innerText = "Canlı İzleme";
                subtitle.innerText = "Sistemdeki cihazların canlı performansı.";
                content.innerHTML = `
        <div id="agentGrid" class="row row-cols-1 row-cols-lg-2 row-cols-xl-3 g-4">
            </div>
        <div id="livePagination" class="d-flex justify-content-center mt-4 pb-2"></div>
    `;
                if (window.loadAgents) loadAgents();
                break;

            case 'all-computers':
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
                        <div id="allPagination" class="d-flex justify-content-center mt-3 pb-2"></div>
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
                subtitle.innerText = "Kullanıcı rolleri ve cihaz/etiket erişimleri.";
                await loadUsersView(content);
                break;
            case 'roles':
                title.innerText = "Roller ve Yetkiler";
                subtitle.innerText = "Sistemdeki rollerin yetkilerini (permissions) düzenleyin.";
                await loadRolesView(content);
                break;
            case 'tags':
                title.innerText = "Etiket Yönetimi";
                subtitle.innerText = "Sistemdeki etiketleri yönetin ve cihazlara atayın.";
                content.innerHTML = `
        <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
            <div class="card-body">
                <div class="input-group mb-4">
                    <input type="text" id="newTagName" class="form-control" placeholder="Yeni etiket adı..." style="background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                    <button class="btn btn-primary" onclick="ui.createNewTag()">+ Ekle</button>
                </div>
                <div class="table-responsive">
                    <table class="table" style="color:var(--text-main);">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th>Etiket Adı</th>
                                <th class="text-end">İşlemler</th>
                            </tr>
                        </thead>
                        <tbody id="tagTableBody">
                            <tr><td colspan="2" class="text-center">Yükleniyor...</td></tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="modal fade" id="tagAssignModal" tabindex="-1">
            <div class="modal-dialog modal-md">
                <div class="modal-content" style="background:var(--bg-card); color:var(--text-main);">
                    <div class="modal-header border-bottom border-secondary">
                        <h5 class="modal-title">Etiketi Cihazlara Ata</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" id="assignTagId">
                        <div id="assignComputerList" class="list-group list-group-flush" style="max-height: 400px; overflow-y: auto;">
                            </div>
                    </div>
                    <div class="modal-footer border-top border-secondary">
                        <button class="btn btn-secondary" data-bs-dismiss="modal">İptal</button>
                        <button class="btn btn-success" onclick="ui.saveTagAssignments()">Kaydet</button>
                    </div>
                </div>
            </div>
        </div>
    `;
                ui.loadTagTable();
                break;
        }
    }

    // --- Alt Görünümler ---

    async function loadRequestsView(container) {
        try {
            // YENİ: Artık rolleri statik diziden değil dinamik olarak API'den çekiyoruz
            const [reqs, roles] = await Promise.all([
                api.get("/api/Admin/requests"),
                api.get("/api/Admin/roles")
            ]);

            let rows = reqs.map(r => `
                <tr>
                    <td class="fw-bold">${r.username}</td>
                    <td>
                        <select id="reqRole_${r.id}" class="form-select form-select-sm small-select" style="max-width: 150px; background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                            ${roles.map(x => `<option value="${x.id}" ${x.name === 'Görüntüleyici' ? 'selected' : ''}>${x.name}</option>`).join("")}
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

    // YENİDEN YAZILAN KULLANICILAR SAYFASI
    async function loadUsersView(container) {
        try {
            const users = await api.get("/api/Admin/users");

            // Sisteme giriş yapmış olan kullanıcının kullanıcı adını alıyoruz
            const currentUsername = localStorage.getItem("staj2_username") || "";

            let rows = users
                // 1. KENDİSİNİ GİZLE: Giriş yapan kullanıcı listede görünmesin
                .filter(u => u.username !== currentUsername)
                .map(u => {
                    const roleBadges = u.roles.map(r => `<span class="badge bg-secondary me-1">${r}</span>`).join('');

                    // 2. YÖNETİCİ KONTROLÜ: Kullanıcı Yönetici mi?
                    const isAdmin = u.roles.includes("Yönetici");

                    return `
                        <tr>
                            <td class="fw-bold" style="color:#38bdf8;">${u.username}</td>
                            <td>${roleBadges || '<span class="text-muted small fst-italic">Rol Atanmamış</span>'}</td>
                            <td class="text-end">
                                ${!isAdmin ? `
                                    <button class="btn btn-outline-primary btn-sm me-1 mb-1" onclick="ui.openUserRolesModal(${u.id}, '${u.username}')" title="Rol İşlemleri"><i class="bi bi-shield-check"></i> Roller</button>
                                    <button class="btn btn-outline-success btn-sm me-1 mb-1" onclick="ui.openUserComputerAccessModal(${u.id}, '${u.username}')" title="Cihaz Erişimleri"><i class="bi bi-pc-display"></i> Cihazlar</button>
                                    <button class="btn btn-outline-warning btn-sm me-2 mb-1" onclick="ui.openUserTagAccessModal(${u.id}, '${u.username}')" title="Etiket Erişimleri"><i class="bi bi-tags"></i> Etiketler</button>
                                    <button class="btn btn-outline-danger btn-sm mb-1" onclick="ui.deleteUser(${u.id})" title="Kullanıcıyı Sil"><i class="bi bi-trash"></i></button>
                                ` : `<span class="text-muted small fst-italic"><i class="bi bi-shield-lock-fill text-warning"></i> &emsp;&emsp;&emsp;</span>`}
                            </td>
                        </tr>`;
                }).join("");

            container.innerHTML = `
                <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Kullanıcı Adı</th><th>Sahip Olduğu Roller</th><th class="text-end">İşlemler</th></tr></thead>
                            <tbody>${rows || '<tr><td colspan="3" class="text-center text-muted py-4">Listelenecek başka kullanıcı bulunamadı.</td></tr>'}</tbody>
                        </table>
                    </div>
                </div>`;
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    // YENİ EKLENEN ROLLER SAYFASI
    async function loadRolesView(container) {
        try {
            const roles = await api.get("/api/Admin/roles");
            let rows = roles.map(r => `
                <tr>
                    <td class="fw-bold" style="color:var(--text-main);"><i class="bi bi-shield-fill text-warning me-2"></i> ${r.name}</td>
                    <td class="text-end">
                        <button class="btn btn-primary btn-sm" onclick="ui.openRolePermissionsModal(${r.id}, '${r.name}')">
                            <i class="bi bi-list-check"></i> Yetkileri Düzenle
                        </button>
                    </td>
                </tr>
            `).join("");

            container.innerHTML = `
                <div class="card border-0 shadow-sm" style="background:var(--bg-card); max-width: 600px;">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Sistem Rolü</th><th class="text-end">İşlem</th></tr></thead>
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
                ui.switchView('requests');
            } catch (e) { alert(e.message || "Bir hata oluştu."); }
        },

        rejectRequest: async (id) => {
            const reason = prompt("Lütfen ret sebebini giriniz:");
            if (reason === null) return;
            if (reason.length > 200) { alert("Ret sebebi 200 karakterden uzun olamaz."); return; }
            try {
                await api.post(`/api/admin/requests/reject`, { requestId: id, rejectionReason: reason });
                alert("Talep reddedildi.");
                ui.switchView('requests');
            } catch (e) { alert(e.message || "Bir hata oluştu."); }
        },

        deleteUser: async (id) => {
            if (confirm("Kullanıcı silinsin mi?")) {
                try {
                    await api.del(`/api/Admin/users/${id}`);
                    ui.switchView('users');
                } catch (e) { alert(e.message); }
            }
        },
        // ui nesnesinin içine eklenecek yeni fonksiyonlar:

loadTagTable: async () => {
    try {
        const tags = await api.get("/api/Admin/tags");
        const tbody = document.getElementById("tagTableBody");
        tbody.innerHTML = tags.map(t => `
            <tr style="border-bottom: 1px solid var(--border-color);">
                <td class="align-middle fw-bold">${t.name}</td>
                <td class="text-end">
                    <button class="btn btn-sm btn-outline-info me-2" onclick="ui.openAssignModal(${t.id}, '${t.name}')">
                        <i class="bi bi-pc-display"></i> Cihaza Ata
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="ui.deleteTag(${t.id})">
                        <i class="bi bi-trash"></i> Sil
                    </button>
                </td>
            </tr>
        `).join('');
    } catch (e) { console.error(e); }
},


        openAssignModal: async (tagId, tagName) => {
            document.getElementById("assignTagId").value = tagId;
            const listContainer = document.getElementById("assignComputerList");
            listContainer.innerHTML = '<div class="text-center py-3"><div class="spinner-border spinner-border-sm"></div></div>';

            const modal = new bootstrap.Modal(document.getElementById("tagAssignModal"));
            modal.show();

            try {
                // İki isteği aynı anda atıyoruz: Tüm aktif cihazlar ve mevcut atamalar
                const [allComputers, assignedIds] = await Promise.all([
                    api.get("/api/Admin/computers/all"),
                    api.get(`/api/Admin/tags/${tagId}/assigned-computer-ids`)
                ]);

                listContainer.innerHTML = allComputers.map(c => {
                    // Eğer cihazın ID'si atanmışlar listesinde varsa 'checked' ekle
                    const isChecked = assignedIds.includes(c.id) ? 'checked' : '';

                    return `
                <label class="list-group-item d-flex justify-content-between align-items-center" style="background:transparent; color:var(--text-main); border-color:var(--border-color); cursor:pointer;">
                    <div>
                        <input class="form-check-input me-2 comp-check" type="checkbox" value="${c.id}" ${isChecked}>
                        <span class="fw-bold">${c.displayName || c.machineName}</span>
                    </div>
                    <small class="text-muted" style="font-family:monospace;">${c.ipAddress || 'IP Yok'}</small>
                </label>
            `;
                }).join('');

                if (allComputers.length === 0) {
                    listContainer.innerHTML = '<div class="text-center p-3 text-muted">Atanabilecek aktif cihaz bulunamadı.</div>';
                }

            } catch (e) {
                console.error(e);
                listContainer.innerHTML = '<div class="text-danger p-3 text-center">Cihazlar yüklenirken bir hata oluştu.</div>';
            }
        },

saveTagAssignments: async () => {
    const tagId = document.getElementById("assignTagId").value;
    const selectedIds = Array.from(document.querySelectorAll('.comp-check:checked')).map(cb => parseInt(cb.value));

    try {
        await api.post(`/api/Admin/tags/${tagId}/assign-computers`, { computerIds: selectedIds });
        bootstrap.Modal.getInstance(document.getElementById("tagAssignModal")).hide();
        alert("Atama işlemi başarılı!");
    } catch (e) { alert("Hata: " + e.message); }
},

        createNewTag: async () => {
            const nameInput = document.getElementById("newTagName");
            const name = nameInput.value.trim();

            if (name) {
                try {
                    await api.post("/api/Admin/tags", { name });

                    // Giriş kutusunu temizle
                    nameInput.value = "";

                    // HATALI SATIR BURASIYDI: ui.switchView yerine doğrudan switchView çağırılmalı
                    await switchView('tags');

                    // Üst filtreyi ve seçim kutularını anında güncelle
                    if (window.loadFilterTags) window.loadFilterTags();

                } catch (e) {
                    alert("Etiket eklenirken hata: " + e.message);
                }
            }
        },
        deleteTag: async (id) => {
            if (confirm("Etiket silinsin mi?")) {
                try {
                    await api.del(`/api/Admin/tags/${id}`);
                    ui.switchView('tags');
                    // YENİ: Etiket silindikten sonra üst filtreyi güncelle
                    if (window.loadFilterTags) window.loadFilterTags();
                } catch (e) { alert(e.message); }
            }
        },

        // --- YENİ MODAL İŞLEMLERİ BAŞLANGIÇ ---

        // 1. Rol Yetkileri Yönetimi
        openRolePermissionsModal: async (roleId, roleName) => {
            document.getElementById('editRoleIdInput').value = roleId;
            document.getElementById('modalRoleNameText').innerText = roleName;
            const container = document.getElementById('permissionsCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            new bootstrap.Modal(document.getElementById('rolePermissionsModal')).show();

            try {
                const [allPerms, rolePerms] = await Promise.all([
                    api.get('/api/Admin/permissions'),
                    api.get(`/api/Admin/roles/${roleId}/permissions`)
                ]);

                container.innerHTML = allPerms.map(p => `
                    <div class="col-12">
                        <label class="permission-card d-flex align-items-center w-100 py-2" for="perm_${p.id}">
                            <input class="form-check-input custom-toggle m-0 me-3 flex-shrink-0" type="checkbox" id="perm_${p.id}" value="${p.id}" ${rolePerms.includes(p.id) ? 'checked' : ''}>
                            <div class="flex-grow-1" style="min-width: 0;">
                                <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem; white-space: normal; word-break: normal;">
                                    ${p.description || p.name}
                                </div>
                            </div>
                        </label>
                    </div>
                `).join('');
            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },

        saveRolePermissions: async () => {
            const roleId = document.getElementById('editRoleIdInput').value;
            const checkedBoxes = document.querySelectorAll('#permissionsCheckboxContainer input[type="checkbox"]:checked');
            const permIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            try {
                await api.post(`/api/Admin/roles/${roleId}/permissions`, { permissionIds: permIds });
                bootstrap.Modal.getInstance(document.getElementById('rolePermissionsModal')).hide();
                alert("Yetkiler başarıyla güncellendi. Değişikliklerin size yansıması için çıkış yapıp tekrar girmelisiniz.");
            } catch (e) { alert(e.message); }
        },

        // 2. Kullanıcı Rol Yönetimi
        openUserRolesModal: async (userId, username) => {
            document.getElementById('editUserRole_UserId').value = userId;
            document.getElementById('roleModalUserName').innerText = username;
            const container = document.getElementById('userRolesCheckboxContainer');
            container.innerHTML = '<div class="text-center"><div class="spinner-border text-info"></div></div>';

            new bootstrap.Modal(document.getElementById('userRolesModal')).show();

            try {
                // Sadece seçili kullanıcının bilgilerini ve sistemdeki tüm rolleri çeker
                const [users, allRoles] = await Promise.all([
                    api.get('/api/Admin/users'),
                    api.get('/api/Admin/roles')
                ]);
                const user = users.find(u => u.id === userId);

                // YENİ: "Yönetici" rolünü ekrana basılacak listeden çıkarıyoruz
                const assignableRoles = allRoles.filter(r => r.name !== "Yönetici");

                container.innerHTML = assignableRoles.map(r => `
                    <label class="permission-card d-flex align-items-center w-100" for="urole_${r.id}">
                        <input class="form-check-input custom-toggle m-0 me-3" type="checkbox" id="urole_${r.id}" value="${r.id}" ${(user && user.roles.includes(r.name)) ? 'checked' : ''}>
                        <div class="fw-bold" style="color:var(--text-title);">${r.name}</div>
                    </label>
                `).join('');
            } catch (e) { container.innerHTML = `<div class="text-danger">${e.message}</div>`; }
        },

        saveUserRoles: async () => {
            const userId = document.getElementById('editUserRole_UserId').value;
            const checkedBoxes = document.querySelectorAll('#userRolesCheckboxContainer input[type="checkbox"]:checked');
            const roleIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            try {
                await api.put(`/api/Admin/users/${userId}/change-roles`, { newRoleIds: roleIds });
                bootstrap.Modal.getInstance(document.getElementById('userRolesModal')).hide();
                ui.switchView('users');
            } catch (e) { alert(e.message); }
        },

        // 3. Kullanıcı Cihaz Erişimi Yönetimi
        openUserComputerAccessModal: async (userId, username) => {
            document.getElementById('accessModal_UserId').value = userId;
            document.getElementById('computerAccessUserName').innerText = username;
            document.getElementById('computerSearchInput').value = "";
            const container = document.getElementById('userComputerCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            new bootstrap.Modal(document.getElementById('userComputerAccessModal')).show();

            try {
                // DÜZELTME: /api/Computer yerine, az önce yazdığımız kısıtlamasız endpoint'i çağırıyoruz.
                const [allComputers, accessInfo] = await Promise.all([
                    api.get('/api/Admin/computers/all'), // BURASI DEĞİŞTİ
                    api.get(`/api/Admin/users/${userId}/access`)
                ]);

                container.innerHTML = allComputers.map(c => {
                    // YENİ: Cihazın durumunu hesapla ve HTML rozetini (badge) oluştur
                    let statusHtml = "";
                    const isPassive = (new Date() - new Date(c.lastSeen)) > 150000; // 2.5 dakikadan eskiyse pasif
                    if (c.isDeleted) {
                        statusHtml = `<span class="badge bg-danger ms-2" style="font-size:0.65rem;">Silinmiş</span>`;
                    } else if (!isPassive) {
                        statusHtml = `<span class="badge bg-success ms-2" style="font-size:0.65rem;">Aktif</span>`;
                    } else {
                        statusHtml = `<span class="badge bg-secondary ms-2" style="font-size:0.65rem;">Pasif</span>`;
                    }

                    return `
                    <div class="col-md-6 computer-access-item">
                        <label class="permission-card d-flex align-items-center w-100" for="ucomp_${c.id}" style="${c.isDeleted ? 'opacity:0.6;' : ''}">
                            <input class="form-check-input custom-toggle m-0 me-3" type="checkbox" id="ucomp_${c.id}" value="${c.id}" ${accessInfo.computerIds.includes(c.id) ? 'checked' : ''}>
                            <div>
                                <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem;">
                                    ${c.displayName || c.machineName} ${statusHtml}
                                </div>
                                <div style="font-size:0.75rem; color:var(--text-muted); font-family:monospace;">${c.ipAddress || '-'}</div>
                            </div>
                        </label>
                    </div>
                    `;
                }).join('');
            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },

        filterComputerCheckboxes: () => {
            const input = document.getElementById('computerSearchInput').value.toLowerCase();
            const items = document.querySelectorAll('.computer-access-item');
            items.forEach(item => {
                const text = item.innerText.toLowerCase();
                item.style.display = text.includes(input) ? 'block' : 'none';
            });
        },

        saveUserComputerAccess: async () => {
            const userId = document.getElementById('accessModal_UserId').value;
            const checkedBoxes = document.querySelectorAll('#userComputerCheckboxContainer input[type="checkbox"]:checked');
            const compIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            try {
                await api.post(`/api/Admin/users/${userId}/assign-computers`, { computerIds: compIds });
                bootstrap.Modal.getInstance(document.getElementById('userComputerAccessModal')).hide();
            } catch (e) { alert(e.message); }
        },

        // 4. Kullanıcı Etiket Erişimi Yönetimi
        openUserTagAccessModal: async (userId, username) => {
            document.getElementById('tagAccessModal_UserId').value = userId;
            document.getElementById('tagAccessUserName').innerText = username;
            const container = document.getElementById('userTagCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            new bootstrap.Modal(document.getElementById('userTagAccessModal')).show();

            try {
                const [allTags, accessInfo] = await Promise.all([
                    api.get('/api/Admin/tags'),
                    api.get(`/api/Admin/users/${userId}/access`)
                ]);

                container.innerHTML = allTags.map(t => `
                    <label class="permission-card d-flex align-items-center flex-grow-1" for="utag_${t.id}" style="min-width: 150px;">
                        <input class="form-check-input custom-toggle m-0 me-2" type="checkbox" id="utag_${t.id}" value="${t.id}" ${accessInfo.tagIds.includes(t.id) ? 'checked' : ''}>
                        <div class="fw-bold" style="color:var(--text-title);"><i class="bi bi-tag-fill text-secondary me-1"></i> ${t.name}</div>
                    </label>
                `).join('');

                if (allTags.length === 0) {
                    container.innerHTML = `<span class="text-muted small">Sistemde hiç etiket bulunamadı. Önce etiket oluşturun.</span>`;
                }

            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },

        saveUserTagAccess: async () => {
            const userId = document.getElementById('tagAccessModal_UserId').value;
            const checkedBoxes = document.querySelectorAll('#userTagCheckboxContainer input[type="checkbox"]:checked');
            const tagIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            try {
                await api.post(`/api/Admin/users/${userId}/assign-tags`, { tagIds: tagIds });
                bootstrap.Modal.getInstance(document.getElementById('userTagAccessModal')).hide();

                // YENİ: Yetki değiştirildiğinde kullanıcının menüsündeki etiketleri anında yenile
                if (window.loadFilterTags) window.loadFilterTags();

            } catch (e) { alert(e.message); }
        }
        // --- YENİ MODAL İŞLEMLERİ BİTİŞ ---
    };

    // --- Tema Başlatma (Sayfa Yüklenince) ---
    (function initTheme() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        document.documentElement.setAttribute('data-theme', savedTheme);
    })();

})();