// STAJ2/wwwroot/assets/js/ui.js

(function () {
    // --- SAYFALAMA VE HAFIZA (STATE) YÖNETİMİ ---
    const ITEMS_PER_PAGE = 7; // Ana tablolar için sayfadaki eleman sayısı
    const MODAL_ITEMS_PER_PAGE = 6; // Modallar için sayfadaki eleman sayısı

    let pgState = {
        requests: { data: [], roles: [], page: 1 },
        users: { data: [], actions: [], page: 1 },
        roles: { data: [], page: 1 },
        tags: { data: [], page: 1 },
        rolePerm: { data: [], assignedIds: [], page: 1 },
        tagAssign: { data: [], filtered: [], assignedIds: [], page: 1 },
        userRoles: { data: [], assignedIds: [], page: 1 },
        userComp: { data: [], filtered: [], assignedIds: [], page: 1 },
        userTag: { data: [], assignedIds: [], page: 1 }
    };

    function renderPagination(containerId, currentPage, totalItems, itemsPerPage, changePageFnString) {
        const container = document.getElementById(containerId);
        if (!container) return;
        const totalPages = Math.ceil(totalItems / itemsPerPage);

        if (totalPages <= 1) { container.innerHTML = ''; return; }

        let html = '<ul class="pagination pagination-sm justify-content-center mt-3 mb-0 shadow-sm">';
        html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="javascript:void(0)" onclick="${changePageFnString}(${currentPage - 1})">Önceki</a>
                 </li>`;
        for (let i = 1; i <= totalPages; i++) {
            html += `<li class="page-item ${currentPage === i ? 'active' : ''}">
                        <a class="page-link" href="javascript:void(0)" onclick="${changePageFnString}(${i})">${i}</a>
                     </li>`;
        }
        html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="javascript:void(0)" onclick="${changePageFnString}(${currentPage + 1})">Sonraki</a>
                 </li>`;
        html += '</ul>';
        container.innerHTML = html;
    }

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
    async function renderSidebar() {
        const nav = document.getElementById('main-nav');
        if (!nav) return;

        try {
            // Doğrudan dinamik menü elemanlarını API'den çek (Backend sadece yetkimiz olanları gönderir)
            const sidebarItems = await api.get('/api/Ui/sidebar-items');

            let html = '';

            // YENİ SİSTEM: Artık isProtected flag'ine göre ayırıyoruz
            const mainItems = sidebarItems.filter(item => !item.isProtected);
            const adminItems = sidebarItems.filter(item => item.isProtected);

            // 1. Herkese Açık / Temel Menüleri Oluştur
            mainItems.forEach(item => {
                const isActive = item.targetView === 'computers' ? 'active' : '';

                html += `
        <li class="nav-item">
            <a href="javascript:void(0)" id="nav-${item.targetView}" class="nav-link ${isActive}" onclick="ui.switchView('${item.targetView}')">
                <i class="${item.icon || 'bi bi-circle'}"></i> <span>${item.title}</span>
            </a>
        </li>`;
            });

            // 2. Yönetim Paneli Menülerini Oluştur
            if (adminItems.length > 0) {
                html += `
        <li class="px-4 mt-4 mb-2">
            <small class="text-uppercase fw-bold" style="font-size:0.7rem; letter-spacing:1px; color:var(--text-muted);">Yönetim Paneli</small>
        </li>`;

                adminItems.forEach(item => {
                    html += `
            <li class="nav-item">
                <a href="javascript:void(0)" id="nav-${item.targetView}" class="nav-link" onclick="ui.switchView('${item.targetView}')">
                    <i class="${item.icon || 'bi bi-circle'}"></i> <span>${item.title}</span>
                </a>
            </li>`;
                });
            }

            nav.innerHTML = html;

        } catch (error) {
            console.error("Menü veritabanından çekilirken hata oluştu:", error);
            nav.innerHTML = `
    <li class="nav-item">
        <a href="javascript:void(0)" id="nav-computers" class="nav-link active" onclick="ui.switchView('computers')">
            <i class="bi bi-activity text-success"></i> <span>Canlı İzleme</span>
        </a>
    </li>`;
        }
    }

    async function switchView(view) {
        const content = document.getElementById('dynamic-content');
        const title = document.getElementById('view-title');
        const subtitle = document.getElementById('view-subtitle');
        if (!content) return;

        document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
        const activeNav = document.getElementById(`nav-${view}`);
        if (activeNav) activeNav.classList.add('active');

        // Filtrenin Sadece Belirli Sayfalarda Görünmesi
        const filterEl = document.getElementById('globalFilters');
        if (filterEl) {
            if (['computers', 'all-computers', 'tags'].includes(view)) {
                filterEl.classList.remove('d-none');
                filterEl.classList.add('d-flex');
            } else {
                filterEl.classList.remove('d-flex');
                filterEl.classList.add('d-none');
            }
        }

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
                <div id="tagsPg" class="pb-3"></div>
            </div>
        </div>

        <div class="modal fade" id="tagAssignModal" tabindex="-1">
            <div class="modal-dialog modal-lg modal-dialog-centered">
                <div class="modal-content" style="background:var(--bg-card); color:var(--text-main);">
                    <div class="modal-header border-bottom border-secondary">
                        <h5 class="modal-title">Etiketi Cihazlara Ata</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
    <input type="hidden" id="assignTagId">

    <div class="input-group mb-3">
        <span class="input-group-text" style="background:var(--bg-input); border-color:var(--border-color); color:var(--text-muted);"><i class="bi bi-search"></i></span>
        <input type="text" id="tagAssignSearchInput" class="form-control" placeholder="Cihaz adı veya IP ara..." onkeyup="ui.filterTagAssignComputers()" style="background:var(--bg-input); border-color:var(--border-color); color:var(--text-main);">
    </div>
    <div id="assignComputerList" class="list-group list-group-flush" style="max-height: 400px; overflow-y: auto;"></div>
    <div id="tagAssignPg" class="mt-2"></div>
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

    // --- Alt Görünümler (Sayfalamaya Uygun Hale Getirildi) ---

    async function loadRequestsView(container) {
        try {
            const [reqs, roles] = await Promise.all([
                api.get("/api/Admin/requests"),
                api.get("/api/Admin/roles")
            ]);
            pgState.requests.data = reqs;
            pgState.requests.roles = roles;
            pgState.requests.page = 1;

            container.innerHTML = `
                <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Kullanıcı</th><th>Atanacak Rol</th><th class="text-end">İşlem</th></tr></thead>
                            <tbody id="reqTbody"></tbody>
                        </table>
                    </div>
                    <div id="reqPg" class="pb-3"></div>
                </div>`;
            ui.renderRequestsTable();
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    async function loadUsersView(container) {
        try {
            // Sadece kullanıcıları çekiyoruz
            const users = await api.get("/api/Admin/users");

            pgState.users.data = users;
            // pgState.users.actions sildik
            pgState.users.page = 1;

            container.innerHTML = `
            <div class="card border-0 shadow-sm" style="background:var(--bg-card);">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead>
                            <tr style="color:var(--text-muted);">
                                <th>Kullanıcı Adı</th>
                                <th>Sahip Olduğu Roller</th>
                                <th class="text-end">İşlemler</th>
                            </tr>
                        </thead>
                        <tbody id="usersTbody"></tbody>
                    </table>
                </div>
                <div id="usersPg" class="pb-3"></div>
            </div>`;

            ui.renderUsersTable();

        } catch (e) {
            container.innerHTML = `
            <div class="d-flex flex-column align-items-center justify-content-center p-5 text-center" style="min-height: 400px; color:var(--text-muted);">
                <i class="bi bi-shield-lock-fill text-danger mb-3" style="font-size: 3rem;"></i>
                <h4 class="text-white">Yetkisiz Erişim veya Bağlantı Hatası</h4>
                <p>${e.message || 'Bu veriyi görüntüleme yetkiniz bulunmamaktadır.'}</p>
            </div>`;
        }
    }

    async function loadRolesView(container) {
        try {
            const roles = await api.get("/api/Admin/roles");
            // Sabit string yerine APP_CONFIG kullanımı
            pgState.roles.data = roles.filter(r => r.name !== window.APP_CONFIG.ADMIN_ROLE_NAME);
            pgState.roles.page = 1;

            // KONTROL: Sadece Role.Manage yetkisi olanlar butonu görebilir
            const canManageRoles = window.auth.hasPermission('Role.Manage');
            const addRoleBtn = canManageRoles ? `<button class="btn btn-success btn-sm" onclick="ui.openCreateRoleModal()">+ Yeni Rol Ekle</button>` : '';

            container.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-3" style="max-width: 600px;">
                    <h5 class="m-0" style="color:var(--text-title);"></h5>
                    ${addRoleBtn}
                </div>
                <div class="card border-0 shadow-sm" style="background:var(--bg-card); max-width: 600px;">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead><tr style="color:var(--text-muted);"><th>Sistem Rolü</th><th class="text-end">İşlem</th></tr></thead>
                            <tbody id="rolesTbody"></tbody>
                        </table>
                    </div>
                    <div id="rolesPg" class="pb-3"></div>
                </div>
                
                <div class="modal fade" id="createRoleModal" tabindex="-1">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content" style="background:var(--bg-card); color:var(--text-main);">
                            <div class="modal-header border-bottom border-secondary">
                                <h5 class="modal-title">Yeni Rol Ekle</h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="mb-3">
                                    <label class="form-label text-muted small">Rol Adı (Maks 200 karakter)</label>
                                    <input type="text" id="newRoleNameInput" class="form-control" maxlength="20" placeholder="Örn: Sınıf Başkanı" style="background:var(--bg-input); border-color:var(--border-color); color:var(--text-main);">
                                </div>
                                <label class="form-label text-muted small mb-2">Başlangıç Yetkileri</label>
                                <div id="newRolePermsContainer" class="row g-2" style="max-height: 250px; overflow-y: auto;">
                                    </div>
                            </div>
                            <div class="modal-footer border-top border-secondary">
                                <button class="btn btn-secondary" data-bs-dismiss="modal">İptal</button>
                                <button class="btn btn-success" onclick="ui.saveNewRole()">Oluştur</button>
                            </div>
                        </div>
                    </div>
                </div>`;
            ui.renderRolesTable();
        } catch (e) { container.innerHTML = `<div class="alert alert-danger">${e.message}</div>`; }
    }

    // --- Fonksiyonları Dışarı Aç (Window.UI) ---
    window.ui = {
        show, hide, setText, backOrHome,
        renderSidebar, switchView, toggleTheme,

        approveRequest: async (id) => {
            if (!confirm("Bu kullanıcıyı onaylamak istiyor musunuz?")) return;

            // 1. İşlem yapılan butonu bul ve loading durumuna al
            const approveBtn = document.querySelector(`button[onclick="ui.approveRequest(${id})"]`);
            const rejectBtn = document.querySelector(`button[onclick="ui.rejectRequest(${id})"]`);

            if (approveBtn) {
                // Orijinal metni sakla, spinner ekle ve butonu kilitle
                approveBtn.dataset.originalText = approveBtn.innerHTML;
                approveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Mail Gönderiliyor...';
                approveBtn.disabled = true;
            }
            if (rejectBtn) rejectBtn.disabled = true; // Çakışmayı önlemek için reddet butonunu da kilitle

            try {
                const roleId = document.getElementById(`reqRole_${id}`).value;
                await api.post(`/api/admin/requests/approve/${id}`, { newRoleId: parseInt(roleId) });

                alert("Kullanıcı onaylandı ve bilgilendirme maili gönderildi.");
                ui.switchView('requests'); // Tabloyu yeniler (butonlar kendiliğinden sıfırlanır)
            } catch (e) {
                alert(e.message || "Bir hata oluştu.");

                // 2. Hata olursa butonları eski haline getir (sayfa yenilenmezse)
                if (approveBtn) {
                    approveBtn.innerHTML = approveBtn.dataset.originalText;
                    approveBtn.disabled = false;
                }
                if (rejectBtn) rejectBtn.disabled = false;
            }
        },

        rejectRequest: async (id) => {
            const reason = prompt("Lütfen ret sebebini giriniz:");
            if (reason === null) return;
            if (reason.length > 200) { alert("Ret sebebi 200 karakterden uzun olamaz."); return; }

            // --- YENİ: Butonları bul ve yükleniyor (spinner) moduna al ---
            const approveBtn = document.querySelector(`button[onclick="ui.approveRequest(${id})"]`);
            const rejectBtn = document.querySelector(`button[onclick="ui.rejectRequest(${id})"]`);

            if (rejectBtn) {
                // Orijinal metni sakla, spinner ekle ve butonu kilitle
                rejectBtn.dataset.originalText = rejectBtn.innerHTML;
                rejectBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Mail Gönderiliyor...';
                rejectBtn.disabled = true;
            }
            // Aynı anda onayla butonuna basılmasını engellemek için onu da kilitle
            if (approveBtn) approveBtn.disabled = true;

            try {
                await api.post(`/api/admin/requests/reject`, { requestId: id, rejectionReason: reason });

                alert("Talep reddedildi ve bilgilendirme maili gönderildi.");
                ui.switchView('requests'); // Tabloyu yeniler (butonlar kendiliğinden sıfırlanır)
            } catch (e) {
                alert(e.message || "Bir hata oluştu.");

                // Hata durumunda butonları eski haline getir
                if (rejectBtn) {
                    rejectBtn.innerHTML = rejectBtn.dataset.originalText;
                    rejectBtn.disabled = false;
                }
                if (approveBtn) approveBtn.disabled = false;
            }
        },
        deleteUser: async (id) => {
            if (confirm("Kullanıcı silinsin mi?")) {
                try {
                    await api.del(`/api/Admin/users/${id}`);
                    ui.switchView('users');
                } catch (e) { alert(e.message); }
            }
        },

        // --- 1. KAYIT İSTEKLERİ RENDER & SAYFALAMA ---
        renderRequestsTable: () => {
            const tbody = document.getElementById('reqTbody'); if (!tbody) return;
            const state = pgState.requests;
            const start = (state.page - 1) * ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE);

            let rows = paginated.map(r => `
                <tr>
                    <td class="fw-bold">${r.username}</td>
                    <td>
                        <select id="reqRole_${r.id}" class="form-select form-select-sm small-select" style="max-width: 150px; background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                            ${state.roles.map(x => `<option value="${x.id}" ${x.name === 'Görüntüleyici' ? 'selected' : ''}>${x.name}</option>`).join("")}
                        </select>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-success" onclick="ui.approveRequest(${r.id})">Onayla</button>
                        <button class="btn btn-sm btn-danger ms-1" onclick="ui.rejectRequest(${r.id})">Reddet</button>
                    </td>
                </tr>`).join("");

            tbody.innerHTML = rows || '<tr><td colspan="3" class="text-center text-muted py-4">Bekleyen kayıt talebi yok.</td></tr>';
            renderPagination('reqPg', state.page, state.data.length, ITEMS_PER_PAGE, 'ui.changeReqPage');
        },
        changeReqPage: (p) => { pgState.requests.page = p; ui.renderRequestsTable(); },

        // --- 2. KULLANICILAR RENDER & SAYFALAMA ---
        renderUsersTable: () => {
            const tbody = document.getElementById('usersTbody'); if (!tbody) return;
            const state = pgState.users;
            const start = (state.page - 1) * ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE);

            let rows = paginated.map(u => {
                const roleBadges = u.roles.map(r => `<span class="badge bg-secondary me-1">${r}</span>`).join('');
                const isAdmin = u.roles.includes(window.APP_CONFIG.ADMIN_ROLE_NAME);

                let actionButtons = '';

                // Roller Butonu
                if (window.auth.hasPermission('User.ManageRoles')) {
                    actionButtons += `<button class="btn btn-outline-primary btn-sm" onclick="ui.openUserRolesModal(${u.id}, '${u.username}')" title="Roller"><i class="bi bi-shield-check"></i> Roller</button> `;
                }

                // Cihazlar Butonu
                if (window.auth.hasPermission('User.ManageComputers')) {
                    actionButtons += `<button class="btn btn-outline-success btn-sm" onclick="ui.openUserComputerAccessModal(${u.id}, '${u.username}')" title="Cihazlar"><i class="bi bi-pc-display"></i> Cihazlar</button> `;
                }

                // Etiketler Butonu
                if (window.auth.hasPermission('User.ManageTags')) {
                    actionButtons += `<button class="btn btn-outline-warning btn-sm" onclick="ui.openUserTagAccessModal(${u.id}, '${u.username}')" title="Etiketler"><i class="bi bi-tags"></i> Etiketler</button> `;
                }

                // Sil Butonu (Hem Yönetici rolüne hem de Role yönetimi iznine sahip mi diye bakıyoruz)
                if (window.auth.hasRole('Yönetici') && window.auth.hasPermission('User.ManageRoles')) {
                    if (isAdmin) {
                        actionButtons += `<span class="btn btn-sm disabled opacity-25" title="Yönetici Silinemez"><i class="bi bi-shield-lock-fill"></i></span> `;
                    } else {
                        actionButtons += `<button class="btn btn-outline-danger btn-sm" onclick="ui.deleteUser(${u.id})" title="Sil"><i class="bi bi-trash"></i></button> `;
                    }
                }

                if (actionButtons === '') {
                    actionButtons = `<span class="text-muted small fst-italic">İşlem Yetkisi Yok</span>`;
                }

                return `<tr>
    <td class="fw-bold" style="color:#38bdf8;">${u.username}</td>
    <td>${roleBadges || '<span class="text-muted small fst-italic">Rol Atanmamış</span>'}</td>
    <td class="text-end">
        <div class="d-flex justify-content-end gap-1 flex-wrap">
            ${actionButtons}
        </div>
    </td>
</tr>`;
            }).join("");

            tbody.innerHTML = rows || '<tr><td colspan="3" class="text-center text-muted py-4">Listelenecek başka kullanıcı bulunamadı.</td></tr>';
            renderPagination('usersPg', state.page, state.data.length, ITEMS_PER_PAGE, 'ui.changeUsersPage');
        },
        changeUsersPage: (p) => { pgState.users.page = p; ui.renderUsersTable(); },

        // --- 3. ROLLER VE YETKİLER RENDER & SAYFALAMA ---
        // --- 3. ROLLER VE YETKİLER RENDER & SAYFALAMA ---
        renderRolesTable: () => {
            const tbody = document.getElementById('rolesTbody'); if (!tbody) return;
            const state = pgState.roles;
            const start = (state.page - 1) * ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE);

            // Yetkisi olanlara sil butonunu da gösterelim
            const canManageRoles = window.auth.hasPermission('Role.Manage');

            let rows = paginated.map(r => `<tr>
                    <td class="fw-bold" style="color:var(--text-main);"><i class="bi bi-shield-fill text-warning me-2"></i> ${r.name}</td>
                    <td class="text-end">
                        <button class="btn btn-primary btn-sm me-1" onclick="ui.openRolePermissionsModal(${r.id}, '${r.name}')"><i class="bi bi-list-check"></i> Yetkileri Düzenle</button>
                        ${canManageRoles ? `<button class="btn btn-outline-danger btn-sm" onclick="ui.deleteRole(${r.id}, '${r.name}')"><i class="bi bi-trash"></i> Sil</button>` : ''}
                    </td>
                </tr>`).join("");

            tbody.innerHTML = rows || '<tr><td colspan="2" class="text-center text-muted py-4">Sistemde rol bulunamadı.</td></tr>';
            renderPagination('rolesPg', state.page, state.data.length, ITEMS_PER_PAGE, 'ui.changeRolesPage');
        },
        changeRolesPage: (p) => { pgState.roles.page = p; ui.renderRolesTable(); },

        // --- ETİKETLER ANA TABLO ---
        loadTagTable: async () => {
            try {
                const tags = await api.get("/api/Admin/tags");
                const selectedFilters = $('#tagSelect').val() || [];
                pgState.tags.data = selectedFilters.length > 0
                    ? tags.filter(t => selectedFilters.includes(t.name))
                    : tags;
                pgState.tags.page = 1;
                ui.renderTagsTable();
            } catch (e) { console.error(e); }
        },
        renderTagsTable: () => {
            const tbody = document.getElementById("tagTableBody"); if (!tbody) return;
            const state = pgState.tags;
            const start = (state.page - 1) * ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE);

            tbody.innerHTML = paginated.map(t => `
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
                </tr>`).join('');

            if (state.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-4">Aranan kriterlere uygun etiket bulunamadı.</td></tr>';
            }
            renderPagination('tagsPg', state.page, state.data.length, ITEMS_PER_PAGE, 'ui.changeTagsPage');
        },
        changeTagsPage: (p) => { pgState.tags.page = p; ui.renderTagsTable(); },

        createNewTag: async () => {
            const nameInput = document.getElementById("newTagName");
            const name = nameInput.value.trim();
            if (name) {
                try {
                    await api.post("/api/Admin/tags", { name });
                    nameInput.value = "";
                    await switchView('tags');
                    if (window.loadFilterTags) window.loadFilterTags();
                } catch (e) { alert("Etiket eklenirken hata: " + e.message); }
            }
        },
        deleteTag: async (id) => {
            if (confirm("Etiket silinsin mi?")) {
                try {
                    await api.del(`/api/Admin/tags/${id}`);
                    ui.switchView('tags');
                    if (window.loadFilterTags) window.loadFilterTags();
                } catch (e) { alert(e.message); }
            }
        },

        // --- ETİKET ATA MODALI ---
        openAssignModal: async (tagId, tagName) => {
            document.getElementById("assignTagId").value = tagId;

            // YENİ EKLENEN: Arama kutusunu sıfırla
            const searchInput = document.getElementById("tagAssignSearchInput");
            if (searchInput) searchInput.value = "";

            const listContainer = document.getElementById("assignComputerList");
            listContainer.innerHTML = '<div class="text-center py-3"><div class="spinner-border spinner-border-sm"></div></div>';
            document.getElementById('tagAssignPg').innerHTML = '';

            new bootstrap.Modal(document.getElementById("tagAssignModal")).show();

            try {
                const [allComputers, assignedIds] = await Promise.all([
                    api.get("/api/Admin/computers/all"),
                    api.get(`/api/Admin/tags/${tagId}/assigned-computer-ids`)
                ]);
                const activeComputers = allComputers.filter(c => !c.isDeleted);
                pgState.tagAssign.data = activeComputers;
                pgState.tagAssign.filtered = activeComputers;
                pgState.tagAssign.assignedIds = assignedIds;
                pgState.tagAssign.page = 1;
                ui.renderTagAssignList();
            } catch (e) { listContainer.innerHTML = '<div class="text-danger p-3 text-center">Cihazlar yüklenirken bir hata oluştu.</div>'; }
        },
        renderTagAssignList: () => {
            const listContainer = document.getElementById("assignComputerList"); if (!listContainer) return;
            const state = pgState.tagAssign;
            const start = (state.page - 1) * MODAL_ITEMS_PER_PAGE;

            // YENİ EKLENEN: state.data yerine state.filtered kullanıyoruz
            const paginated = state.filtered.slice(start, start + MODAL_ITEMS_PER_PAGE);

            if (state.filtered.length === 0) {
                listContainer.innerHTML = '<div class="text-center p-3 text-muted">Aramaya uygun aktif cihaz bulunamadı.</div>';
                document.getElementById('tagAssignPg').innerHTML = '';
                return;
            }

            listContainer.innerHTML = paginated.map(c => `
                <label class="list-group-item d-flex justify-content-between align-items-center" style="background:transparent; color:var(--text-main); border-color:var(--border-color); cursor:pointer;">
                    <div class="d-flex align-items-center overflow-hidden me-3" style="flex: 1;">
                        <input class="form-check-input me-2 comp-check flex-shrink-0" type="checkbox" value="${c.id}" ${state.assignedIds.includes(c.id) ? 'checked' : ''} onchange="ui.toggleTagAssign(${c.id}, this.checked)">
                        <span class="fw-bold text-truncate" title="${c.displayName || c.machineName}">${c.displayName || c.machineName}</span>
                    </div>
                    <small class="text-muted flex-shrink-0" style="font-family:monospace;">${c.ipAddress || 'IP Yok'}</small>
                </label>`).join('');

            // YENİ EKLENEN: Sayfalama için de state.filtered.length kullanıyoruz
            renderPagination('tagAssignPg', state.page, state.filtered.length, MODAL_ITEMS_PER_PAGE, 'ui.changeTagAssignPage');
        },
        changeTagAssignPage: (p) => { pgState.tagAssign.page = p; ui.renderTagAssignList(); },
        toggleTagAssign: (id, isChecked) => {
            if (isChecked) pgState.tagAssign.assignedIds.push(id);
            else pgState.tagAssign.assignedIds = pgState.tagAssign.assignedIds.filter(x => x !== id);
        },
        saveTagAssignments: async () => {
            const tagId = document.getElementById("assignTagId").value;
            try {
                await api.post(`/api/Admin/tags/${tagId}/assign-computers`, { computerIds: pgState.tagAssign.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById("tagAssignModal")).hide();
                alert("Atama işlemi başarılı!");
            } catch (e) { alert("Hata: " + e.message); }
        },

        // --- ROL YETKİLERİ MODALI ---
        openRolePermissionsModal: async (roleId, roleName) => {
            document.getElementById('editRoleIdInput').value = roleId;
            document.getElementById('modalRoleNameText').innerText = roleName;
            const container = document.getElementById('permissionsCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            let pgDiv = document.getElementById('rolePermPg');
            if (!pgDiv) { pgDiv = document.createElement('div'); pgDiv.id = 'rolePermPg'; pgDiv.className = 'mt-3 w-100'; container.parentNode.appendChild(pgDiv); }
            else { pgDiv.innerHTML = ''; }

            new bootstrap.Modal(document.getElementById('rolePermissionsModal')).show();

            try {
                const [allPerms, rolePerms] = await Promise.all([
                    api.get('/api/Admin/permissions'),
                    api.get(`/api/Admin/roles/${roleId}/permissions`)
                ]);
                pgState.rolePerm.data = allPerms;
                pgState.rolePerm.assignedIds = rolePerms;
                pgState.rolePerm.page = 1;
                ui.renderRolePermList();
            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },
        renderRolePermList: () => {
            const container = document.getElementById('permissionsCheckboxContainer'); if (!container) return;
            const state = pgState.rolePerm;
            const start = (state.page - 1) * MODAL_ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + MODAL_ITEMS_PER_PAGE);

            container.innerHTML = paginated.map(p => `
                <div class="col-12">
                    <label class="permission-card d-flex align-items-center w-100 py-2" for="perm_${p.id}">
                        <input class="form-check-input custom-toggle m-0 me-3 flex-shrink-0" type="checkbox" id="perm_${p.id}" value="${p.id}" ${state.assignedIds.includes(p.id) ? 'checked' : ''} onchange="ui.toggleRolePerm(${p.id}, this.checked)">
                        <div class="flex-grow-1" style="min-width: 0;">
                            <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem; white-space: normal; word-break: normal;">${p.description || p.name}</div>
                        </div>
                    </label>
                </div>`).join('');
            renderPagination('rolePermPg', state.page, state.data.length, MODAL_ITEMS_PER_PAGE, 'ui.changeRolePermPage');
        },
        changeRolePermPage: (p) => { pgState.rolePerm.page = p; ui.renderRolePermList(); },
        toggleRolePerm: (id, isChecked) => {
            if (isChecked) pgState.rolePerm.assignedIds.push(id);
            else pgState.rolePerm.assignedIds = pgState.rolePerm.assignedIds.filter(x => x !== id);
        },
        saveRolePermissions: async () => {
            const roleId = document.getElementById('editRoleIdInput').value;
            try {
                await api.post(`/api/Admin/roles/${roleId}/permissions`, { permissionIds: pgState.rolePerm.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('rolePermissionsModal')).hide();
                alert("Yetkiler başarıyla güncellendi. Değişikliklerin size yansıması için çıkış yapıp tekrar girmelisiniz.");
            } catch (e) { alert(e.message); }
        },

        // --- KULLANICI ROLLERİ MODALI ---
        openUserRolesModal: async (userId, username) => {
            document.getElementById('editUserRole_UserId').value = userId;
            document.getElementById('roleModalUserName').innerText = username;
            const container = document.getElementById('userRolesCheckboxContainer');
            container.innerHTML = '<div class="text-center"><div class="spinner-border text-info"></div></div>';

            let pgDiv = document.getElementById('userRolesPg');
            if (!pgDiv) { pgDiv = document.createElement('div'); pgDiv.id = 'userRolesPg'; pgDiv.className = 'mt-3'; container.parentNode.appendChild(pgDiv); }
            else { pgDiv.innerHTML = ''; }

            new bootstrap.Modal(document.getElementById('userRolesModal')).show();

            try {
                const [users, allRoles] = await Promise.all([api.get('/api/Admin/users'), api.get('/api/Admin/roles')]);
                const user = users.find(u => u.id === userId);
                const assignableRoles = allRoles;

                pgState.userRoles.data = assignableRoles;
                pgState.userRoles.assignedIds = assignableRoles.filter(r => user && user.roles.includes(r.name)).map(r => r.id);
                pgState.userRoles.page = 1;
                ui.renderUserRolesList();
            } catch (e) { container.innerHTML = `<div class="text-danger">${e.message}</div>`; }
        },
        renderUserRolesList: () => {
            const container = document.getElementById('userRolesCheckboxContainer'); if (!container) return;
            const state = pgState.userRoles;
            const start = (state.page - 1) * MODAL_ITEMS_PER_PAGE;

            container.innerHTML = state.data.slice(start, start + MODAL_ITEMS_PER_PAGE).map(r => `
                <label class="permission-card d-flex align-items-center w-100" for="urole_${r.id}">
                    <input class="form-check-input custom-toggle m-0 me-3" type="checkbox" id="urole_${r.id}" value="${r.id}" ${state.assignedIds.includes(r.id) ? 'checked' : ''} onchange="ui.toggleUserRole(${r.id}, this.checked)">
                    <div class="fw-bold" style="color:var(--text-title);">${r.name}</div>
                </label>`).join('');
            renderPagination('userRolesPg', state.page, state.data.length, MODAL_ITEMS_PER_PAGE, 'ui.changeUserRolesPage');
        },
        changeUserRolesPage: (p) => { pgState.userRoles.page = p; ui.renderUserRolesList(); },
        toggleUserRole: (id, isChecked) => {
            if (isChecked) pgState.userRoles.assignedIds.push(id);
            else pgState.userRoles.assignedIds = pgState.userRoles.assignedIds.filter(x => x !== id);
        },
        saveUserRoles: async () => {
            const userId = parseInt(document.getElementById('editUserRole_UserId').value);
            const selectedRoleIds = pgState.userRoles.assignedIds;

            // "Yönetici" rolünün ID'sini bulalım
            const adminRole = pgState.userRoles.data.find(r => r.name === window.APP_CONFIG.ADMIN_ROLE_NAME);
            const isAdminSelected = adminRole && selectedRoleIds.includes(adminRole.id);

            try {
                // Eğer kullanıcıdan yönetici rolü alınmak isteniyorsa kontrol yap
                if (!isAdminSelected) {
                    const allUsers = await api.get('/api/Admin/users');
                    const adminCount = allUsers.filter(u => u.roles.includes(window.APP_CONFIG.ADMIN_ROLE_NAME)).length;

                    // Düzenlenen kullanıcı şu an admin mi?
                    const currentUser = allUsers.find(u => u.id === userId);
                    const isCurrentlyAdmin = currentUser && currentUser.roles.includes(window.APP_CONFIG.ADMIN_ROLE_NAME);

                    if (isCurrentlyAdmin && adminCount <= 1) {
                        alert("Sistemde kalan son yönetici yetkisini kaldıramazsınız!");
                        return;
                    }
                }

                await api.put(`/api/Admin/users/${userId}/change-roles`, { newRoleIds: selectedRoleIds });
                bootstrap.Modal.getInstance(document.getElementById('userRolesModal')).hide();
                ui.switchView('users');
                alert("Roller başarıyla güncellendi.");
            } catch (e) {
                alert("Hata: " + e.message);
            }
        },

        // --- KULLANICI CİHAZ ERİŞİMİ MODALI (Arama Filtresi Dahil) ---
        openUserComputerAccessModal: async (userId, username) => {
            document.getElementById('accessModal_UserId').value = userId;
            document.getElementById('computerAccessUserName').innerText = username;
            document.getElementById('computerSearchInput').value = "";
            const container = document.getElementById('userComputerCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            let pgDiv = document.getElementById('userCompPg');
            if (!pgDiv) { pgDiv = document.createElement('div'); pgDiv.id = 'userCompPg'; pgDiv.className = 'mt-3 w-100'; container.parentNode.appendChild(pgDiv); }
            else { pgDiv.innerHTML = ''; }

            new bootstrap.Modal(document.getElementById('userComputerAccessModal')).show();

            try {
                const [allComputers, accessInfo] = await Promise.all([
                    api.get('/api/Admin/computers/all'),
                    api.get(`/api/Admin/users/${userId}/access`)
                ]);

                pgState.userComp.data = allComputers;
                pgState.userComp.filtered = allComputers;
                pgState.userComp.assignedIds = accessInfo.computerIds;
                pgState.userComp.page = 1;
                ui.renderUserCompList();
            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },
        filterComputerCheckboxes: () => {
            const input = document.getElementById('computerSearchInput').value.toLowerCase();
            pgState.userComp.filtered = pgState.userComp.data.filter(c =>
                ((c.displayName || c.machineName) + " " + (c.ipAddress || "")).toLowerCase().includes(input)
            );
            pgState.userComp.page = 1;
            ui.renderUserCompList();
        },
        renderUserCompList: () => {
            const container = document.getElementById('userComputerCheckboxContainer'); if (!container) return;
            const state = pgState.userComp;
            const start = (state.page - 1) * MODAL_ITEMS_PER_PAGE;
            const now = new Date().getTime();

            container.innerHTML = state.filtered.slice(start, start + MODAL_ITEMS_PER_PAGE).map(c => {
                const lastSeenTime = new Date(c.lastSeen).getTime();
                const isOnline = (now - lastSeenTime) <= 150000;

                let statusHtml = c.isDeleted ? `<span class="badge bg-danger ms-2" style="font-size:0.65rem;">Silinmiş</span>` :
                    (isOnline ? `<span class="badge bg-success ms-2" style="font-size:0.65rem;">Aktif</span>` :
                        `<span class="badge bg-secondary ms-2" style="font-size:0.65rem;">Pasif</span>`);

                return `<div class="col-md-6 computer-access-item">
                    <label class="permission-card d-flex align-items-center w-100" for="ucomp_${c.id}" style="${c.isDeleted ? 'opacity:0.6;' : ''}">
                        <input class="form-check-input custom-toggle m-0 me-3" type="checkbox" id="ucomp_${c.id}" value="${c.id}" ${state.assignedIds.includes(c.id) ? 'checked' : ''} onchange="ui.toggleUserComp(${c.id}, this.checked)">
                        <div>
                            <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem;">${c.displayName || c.machineName} ${statusHtml}</div>
                            <div style="font-size:0.75rem; color:var(--text-muted); font-family:monospace;">${c.ipAddress || '-'}</div>
                        </div>
                    </label>
                </div>`;
            }).join('');
            renderPagination('userCompPg', state.page, state.filtered.length, MODAL_ITEMS_PER_PAGE, 'ui.changeUserCompPage');
        },
        changeUserCompPage: (p) => { pgState.userComp.page = p; ui.renderUserCompList(); },
        toggleUserComp: (id, isChecked) => {
            if (isChecked) pgState.userComp.assignedIds.push(id);
            else pgState.userComp.assignedIds = pgState.userComp.assignedIds.filter(x => x !== id);
        },
        saveUserComputerAccess: async () => {
            const userId = document.getElementById('accessModal_UserId').value;
            try {
                await api.post(`/api/Admin/users/${userId}/assign-computers`, { computerIds: pgState.userComp.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('userComputerAccessModal')).hide();
            } catch (e) { alert(e.message); }
        },

        // --- KULLANICI ETİKET ERİŞİMİ MODALI ---
        openUserTagAccessModal: async (userId, username) => {
            document.getElementById('tagAccessModal_UserId').value = userId;
            document.getElementById('tagAccessUserName').innerText = username;
            const container = document.getElementById('userTagCheckboxContainer');
            container.innerHTML = '<div class="text-center py-4 w-100"><div class="spinner-border text-info"></div></div>';

            let pgDiv = document.getElementById('userTagPg');
            if (!pgDiv) { pgDiv = document.createElement('div'); pgDiv.id = 'userTagPg'; pgDiv.className = 'mt-3 w-100'; container.parentNode.appendChild(pgDiv); }
            else { pgDiv.innerHTML = ''; }

            new bootstrap.Modal(document.getElementById('userTagAccessModal')).show();

            try {
                const [allTags, accessInfo] = await Promise.all([
                    api.get('/api/Admin/tags'),
                    api.get(`/api/Admin/users/${userId}/access`)
                ]);

                pgState.userTag.data = allTags;
                pgState.userTag.assignedIds = accessInfo.tagIds;
                pgState.userTag.page = 1;
                ui.renderUserTagList();
            } catch (e) { container.innerHTML = `<div class="text-danger w-100 px-3">${e.message}</div>`; }
        },
        renderUserTagList: () => {
            const container = document.getElementById('userTagCheckboxContainer'); if (!container) return;
            const state = pgState.userTag;
            const start = (state.page - 1) * MODAL_ITEMS_PER_PAGE;

            if (state.data.length === 0) {
                container.innerHTML = `<span class="text-muted small">Sistemde hiç etiket bulunamadı. Önce etiket oluşturun.</span>`;
                document.getElementById('userTagPg').innerHTML = '';
                return;
            }

            container.innerHTML = state.data.slice(start, start + MODAL_ITEMS_PER_PAGE).map(t => `
                <label class="permission-card d-flex align-items-center flex-grow-1" for="utag_${t.id}" style="min-width: 150px;">
                    <input class="form-check-input custom-toggle m-0 me-2" type="checkbox" id="utag_${t.id}" value="${t.id}" ${state.assignedIds.includes(t.id) ? 'checked' : ''} onchange="ui.toggleUserTag(${t.id}, this.checked)">
                    <div class="fw-bold" style="color:var(--text-title);"><i class="bi bi-tag-fill text-secondary me-1"></i> ${t.name}</div>
                </label>`).join('');
            renderPagination('userTagPg', state.page, state.data.length, MODAL_ITEMS_PER_PAGE, 'ui.changeUserTagPage');
        },
        changeUserTagPage: (p) => { pgState.userTag.page = p; ui.renderUserTagList(); },
        toggleUserTag: (id, isChecked) => {
            if (isChecked) pgState.userTag.assignedIds.push(id);
            else pgState.userTag.assignedIds = pgState.userTag.assignedIds.filter(x => x !== id);
        },
        saveUserTagAccess: async () => {
            const userId = document.getElementById('tagAccessModal_UserId').value;
            try {
                await api.post(`/api/Admin/users/${userId}/assign-tags`, { tagIds: pgState.userTag.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('userTagAccessModal')).hide();
                if (window.loadFilterTags) window.loadFilterTags();
            } catch (e) { alert(e.message); }
        },
        filterTagAssignComputers: () => {
            const input = document.getElementById('tagAssignSearchInput').value.toLowerCase();
            pgState.tagAssign.filtered = pgState.tagAssign.data.filter(c =>
                ((c.displayName || c.machineName) + " " + (c.ipAddress || "")).toLowerCase().includes(input)
            );
            pgState.tagAssign.page = 1;
            ui.renderTagAssignList();
        },
        // --- YENİ ROL EKLEME FONKSİYONLARI ---
        openCreateRoleModal: async () => {
            document.getElementById('newRoleNameInput').value = ''; // Inputu temizle
            const permsContainer = document.getElementById('newRolePermsContainer');
            permsContainer.innerHTML = '<div class="text-center w-100 py-3"><div class="spinner-border text-info spinner-border-sm"></div></div>';

            new bootstrap.Modal(document.getElementById('createRoleModal')).show();

            try {
                // Sistemdeki tüm yetkileri çek
                const allPerms = await api.get('/api/Admin/permissions');

                permsContainer.innerHTML = allPerms.map(p => `
                    <div class="col-12">
                        <label class="permission-card d-flex align-items-center w-100 py-2" for="new_perm_${p.id}" style="cursor:pointer;">
                            <input class="form-check-input custom-toggle new-role-perm-cb m-0 me-3 flex-shrink-0" type="checkbox" id="new_perm_${p.id}" value="${p.id}">
                            <div class="flex-grow-1" style="min-width: 0;">
                                <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem;">${p.description || p.name}</div>
                            </div>
                        </label>
                    </div>`).join('');
            } catch (e) {
                permsContainer.innerHTML = `<div class="text-danger small px-2">Yetkiler yüklenemedi: ${e.message}</div>`;
            }
        },

        saveNewRole: async () => {
            const roleName = document.getElementById('newRoleNameInput').value.trim();
            if (!roleName) { alert("Lütfen rol adı giriniz."); return; }
            if (roleName.length > 20) { alert("Rol adı en fazla 20 karakter olabilir."); return; }

            // İşaretli checkbox'ların id'lerini topla
            const selectedPerms = Array.from(document.querySelectorAll('.new-role-perm-cb:checked')).map(cb => parseInt(cb.value));

            try {
                await api.post('/api/Admin/roles', {
                    name: roleName,
                    permissionIds: selectedPerms
                });

                bootstrap.Modal.getInstance(document.getElementById('createRoleModal')).hide();
                alert("Yeni rol başarıyla eklendi!");
                ui.switchView('roles'); // Tabloyu yenile
            } catch (e) {
                alert("Hata: " + (e.message || "Rol eklenirken bir sorun oluştu."));
            }
        },
        deleteRole: async (id, name) => {
            if (!confirm(`'${name}' rolünü silmek istediğinize emin misiniz?`)) return;

            try {
                await api.del(`/api/Admin/roles/${id}`);
                alert("Rol başarıyla silindi.");
                ui.switchView('roles'); // Tabloyu anında yenile
            } catch (e) {
                alert("Hata: " + e.message); // Kullanıcı varsa burada hata mesajını gösterecek
            }
        }
    };

    // --- Tema Başlatma (Sayfa Yüklenince) ---
    (function initTheme() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        document.documentElement.setAttribute('data-theme', savedTheme);
    })();

})();