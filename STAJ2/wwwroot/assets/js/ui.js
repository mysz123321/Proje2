// STAJ2/wwwroot/assets/js/ui.js

(function () {
    // --- SAYFALAMA VE HAFIZA (STATE) YÖNETİMİ ---
    const ITEMS_PER_PAGE = 7;
    const MODAL_ITEMS_PER_PAGE = 6;

    let pgState = {
        requests: { data: [], roles: [], page: 1 },
        users: { data: [], actions: [], page: 1 },
        roles: { data: [], page: 1 },
        tags: { data: [], page: 1 },
        rolePerm: { data: [], assignedIds: [], page: 1 },
        tagAssign: { data: [], filtered: [], assignedIds: [], page: 1 },
        userRoles: { data: [], assignedIds: [], page: 1 },
        userComp: { data: [], filtered: [], assignedIds: [], page: 1 },
        userTag: { data: [], assignedIds: [], page: 1 },
        newRolePerm: { data: [], assignedIds: [], page: 1 },
        reports: {
            bestCpu: { data: [], page: 1 },
            worstCpu: { data: [], page: 1 },
            bestRam: { data: [], page: 1 },
            worstRam: { data: [], page: 1 }
        }
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
            const sidebarItems = await api.get('/api/Ui/sidebar-items');
            let html = '';

            const mainItems = sidebarItems.filter(item => !item.isProtected);
            const adminItems = sidebarItems.filter(item => item.isProtected);

            mainItems.forEach(item => {
                const isActive = item.targetView === 'computers' ? 'active' : '';

                html += `
        <li class="nav-item">
            <a href="javascript:void(0)" id="nav-${item.targetView}" class="nav-link ${isActive}" onclick="ui.switchView('${item.targetView}')">
                <i class="${item.icon || 'bi bi-circle'}"></i> <span>${item.title}</span>
            </a>
        </li>`;
            });

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

            case 'history':
                title.innerText = "Geçmiş Metrikler";
                subtitle.innerText = "Cihazların CPU, RAM ve Disk geçmişlerini detaylı olarak inceleyin.";

                // Filtre alanını gizle (Geçmiş sayfasında kendi filtrelerimiz olacak)
                const historyFilterEl = document.getElementById('globalFilters');
                if (historyFilterEl) { historyFilterEl.classList.remove('d-flex'); historyFilterEl.classList.add('d-none'); }

                content.innerHTML = `
                <div class="row h-100" style="min-height: 75vh;">
                    <div class="col-lg-3 border-end border-secondary pe-lg-4 mb-4" style="border-color: var(--border-color) !important;">
                        <div class="card border-0 shadow-sm" style="background:var(--bg-card); position: sticky; top: 20px;">
                            <div class="card-body">
                                <h5 class="fw-bold mb-4" style="color:var(--text-title);"><i class="bi bi-sliders"></i> Kontrol Paneli</h5>

                                <div class="mb-3">
                                    <label class="form-label fw-bold small mb-1" style="color:var(--text-muted);">CİHAZ SEÇİMİ</label>
                                    <select id="historyPageComputerSelect" class="form-select" style="background:var(--bg-input); color:var(--text-main); border-color:var(--border-input);">
                                        <option value="">Yükleniyor...</option>
                                    </select>
                                </div>

                                <div class="mb-3">
                                    <label class="form-label fw-bold small mb-1" style="color:var(--text-muted);">BAŞLANGIÇ ZAMANI</label>
                                    <input type="datetime-local" id="historyStart" class="form-control" style="background:var(--bg-input); color:var(--text-main); border-color:var(--border-input);">
                                </div>

                                <div class="mb-4">
                                    <label class="form-label fw-bold small mb-1" style="color:var(--text-muted);">BİTİŞ ZAMANI</label>
                                    <input type="datetime-local" id="historyEnd" class="form-control" style="background:var(--bg-input); color:var(--text-main); border-color:var(--border-input);">
                                </div>

                                <button class="btn btn-primary w-100 fw-bold shadow-sm mb-4" onclick="fetchHistoryMetrics()">
                                    <i class="bi bi-search me-2"></i> Getir ve Çiz
                                </button>

                                <div id="diskFiltersContainer" style="display:none; padding-top: 15px; border-top: 1px solid var(--border-color);">
                                    <div class="d-flex align-items-center mb-3">
                                        <i class="bi bi-hdd-network text-info me-2"></i>
                                        <h6 class="mb-0 small fw-bold text-uppercase" style="color:var(--text-muted);">Diskleri Göster</h6>
                                    </div>
                                    <div id="diskCheckboxes" class="row g-2"></div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="col-lg-9 ps-lg-4">
                        <div id="historyPlaceholder" class="text-center py-5 mt-5" style="display: block;">
                            <div class="opacity-50 mb-3" style="color: var(--text-muted);">
                                <i class="bi bi-graph-up display-1"></i>
                            </div>
                            <h4 class="fw-light" style="color: var(--text-title);">Lütfen sol taraftan cihaz ve tarih seçerek analize başlayın.</h4>
                        </div>

                        <div id="historyResults" style="display:none; width: 100%;">
                            <div id="chartsContainer" class="d-flex flex-column gap-4 w-100 pb-4">
                                <div class="card border border-secondary shadow-sm" style="background-color: var(--bg-card);">
    <div class="card-header border-bottom border-secondary text-info fw-bold"><i class="bi bi-cpu"></i> CPU Kullanımı</div>
    <div class="card-body p-2" style="overflow: hidden;">
        <div style="position: relative; height: 250px; width: 100%;">
            <canvas id="cpuChart"></canvas>
        </div>
        <div id="cpuMiniReport" class="mt-3 p-2 rounded" style="background: var(--bg-card-muted); border: 1px solid var(--border-color); display: none;"></div>
    </div>
</div>  

                                <div class="card border border-secondary shadow-sm" style="background-color: var(--bg-card);">
    <div class="card-header border-bottom border-secondary text-danger fw-bold"><i class="bi bi-memory"></i> RAM Kullanımı</div>
    <div class="card-body p-2" style="overflow: hidden;">
        <div style="position: relative; height: 250px; width: 100%;">
            <canvas id="ramChart"></canvas>
        </div>
        <div id="ramMiniReport" class="mt-3 p-2 rounded" style="background: var(--bg-card-muted); border: 1px solid var(--border-color); display: none;"></div>
    </div>
</div>

                                <div id="dynamicDiskCharts" class="d-flex flex-column gap-4"></div>
                            </div>
                        </div>
                    </div>
                </div>`;

                // Sayfa yüklendiğinde Cihazları dropdown'a dolduracak fonksiyonu çağırıyoruz
                if (window.ui.loadHistoryComputers) window.ui.loadHistoryComputers();
                break;
            case 'reports':
                title.innerText = "Performans Raporları";
                subtitle.innerText = "Cihazların CPU ve RAM ortalamalarına göre detaylı analizi.";

                const reportFilterEl = document.getElementById('globalFilters');
                if (reportFilterEl) { reportFilterEl.classList.remove('d-flex'); reportFilterEl.classList.add('d-none'); }

                content.innerHTML = `
                <div class="container-fluid p-0">
                    <div class="row mb-4">
                        <div class="col-md-6">
                            <div class="card border-0 shadow-sm" style="background:var(--bg-card); border-left: 4px solid #0dcaf0 !important;">
                                <div class="card-body">
                                    <h6 class="text-uppercase fw-bold mb-2" style="color:var(--text-muted); font-size: 0.8rem;"><i class="bi bi-cpu"></i> Genel CPU Ortalaması</h6>
                                    <h2 id="global-cpu-avg" class="mb-0" style="color:#0dcaf0;">Yükleniyor...</h2>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="card border-0 shadow-sm mt-3 mt-md-0" style="background:var(--bg-card); border-left: 4px solid #d63384 !important;">
                                <div class="card-body">
                                    <h6 class="text-uppercase fw-bold mb-2" style="color:var(--text-muted); font-size: 0.8rem;"><i class="bi bi-memory"></i> Genel RAM Ortalaması</h6>
                                    <h2 id="global-ram-avg" class="mb-0" style="color:#d63384;">Yükleniyor...</h2>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row g-3">
                        <div class="col-xl-3 col-lg-6 col-md-6">
                            <div class="card border-0 shadow-sm h-100" style="background:var(--bg-card); border-top: 4px solid #198754 !important;">
                                <div class="card-header border-bottom border-secondary p-2" style="background:transparent; color:var(--text-title);">
                                    <h6 class="mb-0 fw-bold" style="font-size: 0.85rem;"><i class="bi bi-check-circle-fill text-success me-1"></i>En İyi CPU <span class="text-muted fw-normal" style="font-size: 0.7rem;">(Ort. Altı)</span></h6>
                                </div>
                                <div class="card-body p-0 d-flex flex-column justify-content-between">
                                    <div class="table-responsive">
                                        <table class="table table-sm table-hover align-middle mb-0">
                                            <tbody id="best-cpu-body">
                                                <tr><td class="text-center py-4 text-muted">Hesaplanıyor...</td></tr>
                                            </tbody>
                                        </table>
                                    </div>
                                    <div id="bestCpuPg" class="py-2"></div>
                                </div>
                            </div>
                        </div>

                        <div class="col-xl-3 col-lg-6 col-md-6">
                            <div class="card border-0 shadow-sm h-100" style="background:var(--bg-card); border-top: 4px solid #dc3545 !important;">
                                <div class="card-header border-bottom border-secondary p-2" style="background:transparent; color:var(--text-title);">
                                    <h6 class="mb-0 fw-bold" style="font-size: 0.85rem;"><i class="bi bi-x-circle-fill text-danger me-1"></i>En Kötü CPU <span class="text-muted fw-normal" style="font-size: 0.7rem;">(Ort. Üstü)</span></h6>
                                </div>
                                <div class="card-body p-0 d-flex flex-column justify-content-between">
                                    <div class="table-responsive">
                                        <table class="table table-sm table-hover align-middle mb-0">
                                            <tbody id="worst-cpu-body">
                                                <tr><td class="text-center py-4 text-muted">Hesaplanıyor...</td></tr>
                                            </tbody>
                                        </table>
                                    </div>
                                    <div id="worstCpuPg" class="py-2"></div>
                                </div>
                            </div>
                        </div>

                        <div class="col-xl-3 col-lg-6 col-md-6">
                            <div class="card border-0 shadow-sm h-100" style="background:var(--bg-card); border-top: 4px solid #198754 !important;">
                                <div class="card-header border-bottom border-secondary p-2" style="background:transparent; color:var(--text-title);">
                                    <h6 class="mb-0 fw-bold" style="font-size: 0.85rem;"><i class="bi bi-check-circle-fill text-success me-1"></i>En İyi RAM <span class="text-muted fw-normal" style="font-size: 0.7rem;">(Ort. Altı)</span></h6>
                                </div>
                                <div class="card-body p-0 d-flex flex-column justify-content-between">
                                    <div class="table-responsive">
                                        <table class="table table-sm table-hover align-middle mb-0">
                                            <tbody id="best-ram-body">
                                                <tr><td class="text-center py-4 text-muted">Hesaplanıyor...</td></tr>
                                            </tbody>
                                        </table>
                                    </div>
                                    <div id="bestRamPg" class="py-2"></div>
                                </div>
                            </div>
                        </div>

                        <div class="col-xl-3 col-lg-6 col-md-6">
                            <div class="card border-0 shadow-sm h-100" style="background:var(--bg-card); border-top: 4px solid #dc3545 !important;">
                                <div class="card-header border-bottom border-secondary p-2" style="background:transparent; color:var(--text-title);">
                                    <h6 class="mb-0 fw-bold" style="font-size: 0.85rem;"><i class="bi bi-x-circle-fill text-danger me-1"></i>En Kötü RAM <span class="text-muted fw-normal" style="font-size: 0.7rem;">(Ort. Üstü)</span></h6>
                                </div>
                                <div class="card-body p-0 d-flex flex-column justify-content-between">
                                    <div class="table-responsive">
                                        <table class="table table-sm table-hover align-middle mb-0">
                                            <tbody id="worst-ram-body">
                                                <tr><td class="text-center py-4 text-muted">Hesaplanıyor...</td></tr>
                                            </tbody>
                                        </table>
                                    </div>
                                    <div id="worstRamPg" class="py-2"></div>
                                </div>
                            </div>
                        </div>
                    </div>
                    </div>
                </div>`;

                if (window.ui.loadReportsView) window.ui.loadReportsView();
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
            const users = await api.get("/api/Admin/users");
            pgState.users.data = users;
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
            pgState.roles.data = roles.filter(r => r.name !== window.APP_CONFIG.ADMIN_ROLE_NAME);
            pgState.roles.page = 1;

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
                                    <label class="form-label text-muted small">Rol Adı (Maks 20 karakter)</label>
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
            const result = await Swal.fire({
                title: 'Onaylıyor musunuz?',
                text: "Bu kullanıcıyı onaylamak istiyor musunuz?",
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Evet, Onayla',
                cancelButtonText: 'İptal'
            });

            if (!result.isConfirmed) return;

            const approveBtn = document.querySelector(`button[onclick="ui.approveRequest(${id})"]`);
            const rejectBtn = document.querySelector(`button[onclick="ui.rejectRequest(${id})"]`);

            if (approveBtn) {
                approveBtn.dataset.originalText = approveBtn.innerHTML;
                approveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Mail Gönderiliyor...';
                approveBtn.disabled = true;
            }
            if (rejectBtn) rejectBtn.disabled = true;

            try {
                const roleId = document.getElementById(`reqRole_${id}`).value;
                const response = await api.post(`/api/admin/requests/approve/${id}`, { newRoleId: parseInt(roleId) });

                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                ui.switchView('requests');
            } catch (e) {
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });

                if (approveBtn) {
                    approveBtn.innerHTML = approveBtn.dataset.originalText;
                    approveBtn.disabled = false;
                }
                if (rejectBtn) rejectBtn.disabled = false;
            }
        },

        rejectRequest: async (id) => {
            const { value: reason } = await Swal.fire({
                title: 'Ret Sebebi',
                input: 'textarea',
                inputLabel: 'Lütfen ret sebebini giriniz:',
                showCancelButton: true,
                confirmButtonText: 'Reddet',
                cancelButtonText: 'İptal',
                inputValidator: (value) => {
                    if (!value) return 'Ret sebebi boş bırakılamaz!'; // Kullanıcı iptale basmak yerine boş gönderemesin diye ufak bir UI UX kuralı olarak kaldı
                }
            });

            if (!reason) return;

            const approveBtn = document.querySelector(`button[onclick="ui.approveRequest(${id})"]`);
            const rejectBtn = document.querySelector(`button[onclick="ui.rejectRequest(${id})"]`);

            if (rejectBtn) {
                rejectBtn.dataset.originalText = rejectBtn.innerHTML;
                rejectBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Mail Gönderiliyor...';
                rejectBtn.disabled = true;
            }
            if (approveBtn) approveBtn.disabled = true;

            try {
                const response = await api.post(`/api/admin/requests/reject`, { requestId: id, rejectionReason: reason });

                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                ui.switchView('requests');
            } catch (e) {
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });

                if (rejectBtn) {
                    rejectBtn.innerHTML = rejectBtn.dataset.originalText;
                    rejectBtn.disabled = false;
                }
                if (approveBtn) approveBtn.disabled = false;
            }
        },
        deleteUser: async (id) => {
            const result = await Swal.fire({
                text: "Kullanıcı silinsin mi?",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Evet, Sil!',
                cancelButtonText: 'İptal'
            });

            if (result.isConfirmed) {
                try {
                    const response = await api.del(`/api/Admin/users/${id}`);
                    Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                    ui.switchView('users');
                } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
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

                if (window.auth.hasPermission('User.ManageRoles')) {
                    actionButtons += `<button class="btn btn-outline-primary btn-sm" onclick="ui.openUserRolesModal(${u.id}, '${u.username}')" title="Roller"><i class="bi bi-shield-check"></i> Roller</button> `;
                }

                if (window.auth.hasPermission('User.ManageComputers')) {
                    actionButtons += `<button class="btn btn-outline-success btn-sm" onclick="ui.openUserComputerAccessModal(${u.id}, '${u.username}')" title="Cihazlar"><i class="bi bi-pc-display"></i> Cihazlar</button> `;
                }

                if (window.auth.hasPermission('User.ManageTags')) {
                    actionButtons += `<button class="btn btn-outline-warning btn-sm" onclick="ui.openUserTagAccessModal(${u.id}, '${u.username}')" title="Etiketler"><i class="bi bi-tags"></i> Etiketler</button> `;
                }

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
        renderRolesTable: () => {
            const tbody = document.getElementById('rolesTbody'); if (!tbody) return;
            const state = pgState.roles;
            const start = (state.page - 1) * ITEMS_PER_PAGE;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE);

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

            try {
                const response = await api.post("/api/Admin/tags", { name });
                nameInput.value = "";
                await switchView('tags');
                if (window.loadFilterTags) window.loadFilterTags();
                Swal.fire({ title: response.title, text: response.message, icon: 'success', timer: 1500, showConfirmButton: false });
            } catch (e) {
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });
            }
        },
        deleteTag: async (id) => {
            const result = await Swal.fire({
                text: "Etiket silinecektir.",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Sil',
                cancelButtonText: 'İptal'
            });

            if (result.isConfirmed) {
                try {
                    const response = await api.del(`/api/Admin/tags/${id}`);
                    ui.switchView('tags');
                    if (window.loadFilterTags) window.loadFilterTags();
                    Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
            }
        },

        // --- ETİKET ATA MODALI ---
        openAssignModal: async (tagId, tagName) => {
            document.getElementById("assignTagId").value = tagId;

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
                const response = await api.post(`/api/Admin/tags/${tagId}/assign-computers`, { computerIds: pgState.tagAssign.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById("tagAssignModal")).hide();
                Swal.fire({ title: response.title, text: response.message, icon: 'success', timer: 1500, showConfirmButton: false });
            } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
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
                const response = await api.post(`/api/Admin/roles/${roleId}/permissions`, { permissionIds: pgState.rolePerm.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('rolePermissionsModal')).hide();
                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
            } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
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

            try {
                // Hiçbir kontrol yapmadan direkt Backend'e gönder!
                const response = await api.put(`/api/Admin/users/${userId}/change-roles`, { newRoleIds: selectedRoleIds });

                bootstrap.Modal.getInstance(document.getElementById('userRolesModal')).hide();
                ui.switchView('users');
                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
            } catch (e) {
                // Eğer son yöneticiyse, Backend'den gelen "Sistemde kalan son..." uyarısı burada patlayacak.
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });
            }
        },

        // --- KULLANICI CİHAZ ERİŞİMİ MODALI ---
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
                const response = await api.post(`/api/Admin/users/${userId}/assign-computers`, { computerIds: pgState.userComp.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('userComputerAccessModal')).hide();
                Swal.fire({ title: response.title, text: response.message, icon: 'success', timer: 1500, showConfirmButton: false });
            } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
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
                const response = await api.post(`/api/Admin/users/${userId}/assign-tags`, { tagIds: pgState.userTag.assignedIds });
                bootstrap.Modal.getInstance(document.getElementById('userTagAccessModal')).hide();
                if (window.loadFilterTags) window.loadFilterTags();
                Swal.fire({ title: response.title, text: response.message, icon: 'success', timer: 1500, showConfirmButton: false });
            } catch (e) { Swal.fire({ title: e.title, text: e.message, icon: 'warning' }); }
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
            document.getElementById('newRoleNameInput').value = '';
            const permsContainer = document.getElementById('newRolePermsContainer');
            permsContainer.innerHTML = '<div class="text-center w-100 py-3"><div class="spinner-border text-info spinner-border-sm"></div></div>';

            let pgDiv = document.getElementById('newRolePermPg');
            if (!pgDiv) {
                pgDiv = document.createElement('div');
                pgDiv.id = 'newRolePermPg';
                pgDiv.className = 'mt-3 w-100';
                permsContainer.parentNode.appendChild(pgDiv);
            } else {
                pgDiv.innerHTML = '';
            }

            new bootstrap.Modal(document.getElementById('createRoleModal')).show();

            try {
                const allPerms = await api.get('/api/Admin/permissions');
                pgState.newRolePerm.data = allPerms;
                pgState.newRolePerm.assignedIds = [];
                pgState.newRolePerm.page = 1;
                ui.renderNewRolePermList();
            } catch (e) {
                permsContainer.innerHTML = `<div class="text-danger small px-2">Yetkiler yüklenemedi: ${e.message}</div>`;
            }
        },

        saveNewRole: async () => {
            const roleName = document.getElementById('newRoleNameInput').value.trim();
            const selectedPerms = pgState.newRolePerm.assignedIds;

            try {
                const response = await api.post('/api/Admin/roles', { name: roleName, permissionIds: selectedPerms });
                bootstrap.Modal.getInstance(document.getElementById('createRoleModal')).hide();
                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                ui.switchView('roles');
            } catch (e) {
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });
            }
        },
        deleteRole: async (id, name) => {
            const result = await Swal.fire({
                text: `'${name}' rolünü silmek istediğinize emin misiniz?`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Evet, Sil!',
                cancelButtonText: 'İptal'
            });

            if (!result.isConfirmed) return;

            try {
                const response = await api.del(`/api/Admin/roles/${id}`);
                Swal.fire({ title: response.title, text: response.message, icon: 'success' });
                ui.switchView('roles');
            } catch (e) {
                Swal.fire({ title: e.title, text: e.message, icon: 'warning' });
            }
        },
        renderNewRolePermList: () => {
            const container = document.getElementById('newRolePermsContainer');
            if (!container) return;

            const ITEMS_FOR_THIS_MODAL = 5;

            const state = pgState.newRolePerm;
            const start = (state.page - 1) * ITEMS_FOR_THIS_MODAL;
            const paginated = state.data.slice(start, start + ITEMS_FOR_THIS_MODAL);

            container.innerHTML = paginated.map(p => `
<div class="col-12">
    <label class="permission-card d-flex align-items-center w-100 py-2" for="new_perm_${p.id}" style="cursor:pointer;">
        <input class="form-check-input custom-toggle new-role-perm-cb m-0 me-3 flex-shrink-0" 
               type="checkbox" id="new_perm_${p.id}" value="${p.id}" 
               ${state.assignedIds.includes(p.id) ? 'checked' : ''} 
               onchange="ui.toggleNewRolePerm(${p.id}, this.checked)">
        <div class="flex-grow-1" style="min-width: 0;">
            <div class="fw-bold" style="color:var(--text-title); font-size:0.9rem;">${p.description || p.name}</div>
        </div>
    </label>
</div>`).join('');

            renderPagination('newRolePermPg', state.page, state.data.length, ITEMS_FOR_THIS_MODAL, 'ui.changeNewRolePermPage');
        },

        changeNewRolePermPage: (p) => {
            pgState.newRolePerm.page = p;
            ui.renderNewRolePermList();
        },

        toggleNewRolePerm: (id, isChecked) => {
            if (isChecked) {
                if (!pgState.newRolePerm.assignedIds.includes(id)) pgState.newRolePerm.assignedIds.push(id);
            } else {
                pgState.newRolePerm.assignedIds = pgState.newRolePerm.assignedIds.filter(x => x !== id);
            }
        },

        loadHistoryComputers: async () => {
            const selectEl = document.getElementById('historyPageComputerSelect');
            if (!selectEl) return;

            try {
                // HATA BURADAYDI: Admin endpoint'i yerine standart ve güvenli endpoint'i kullanıyoruz.
                // Bu sayede kullanıcı sadece kendi yetkisi olan (görebildiği) cihazları listede görecek.
                const computers = await api.get('/api/Computer');
                const activeComputers = computers.filter(c => !c.isDeleted);

                let optionsHtml = '<option value="">-- Cihaz Seçiniz --</option>';
                activeComputers.forEach(c => {
                    optionsHtml += `<option value="${c.id}">${c.displayName || c.machineName}</option>`;
                });

                selectEl.innerHTML = optionsHtml;
            } catch (e) {
                // Eğer hata olursa konsola yazdırsın ama sistemi patlatıp logout atmasın
                console.warn("Cihazlar yüklenirken bir hata oluştu:", e);
                selectEl.innerHTML = '<option value="">Cihazlar yüklenemedi</option>';
            }
        },

        showReportDetails: async (computerId, computerName, metricType, diskName = null) => {
            document.getElementById('rdm-computer-name').innerText = computerName;
            document.getElementById('rdm-metric-type').innerText = metricType;

            document.getElementById('rdm-loading').style.display = 'block';
            document.getElementById('rdm-content').style.display = 'none';
            document.getElementById('rdm-loading').innerHTML = '<div class="spinner-border text-info" role="status"></div><div class="mt-2 text-muted small">Performanslı veritabanı analizi yapılıyor...</div>';

            const modal = new bootstrap.Modal(document.getElementById('reportDetailModal'));
            modal.show();

            try {
                // 1. Genel özet istatistiklerini çek
                let apiUrl = `/api/Computer/${computerId}/metrics-summary?metricType=${metricType}`;
                if (diskName) apiUrl += `&diskName=${encodeURIComponent(diskName)}`;

                const summary = await window.api.get(apiUrl);

                if (summary.totalCount === 0) {
                    document.getElementById('rdm-loading').innerHTML = '<div class="text-warning"><i class="bi bi-exclamation-triangle fs-4 d-block mb-2"></i> Bu cihaz için hiç veri bulunamadı.</div>';
                    return;
                }

                document.getElementById('rdm-total-count').innerText = summary.totalCount + " Adet";
                document.getElementById('rdm-max-val').innerText = `%${Math.round(summary.maxVal)}`;
                document.getElementById('rdm-min-val').innerText = `%${Math.round(summary.minVal)}`;
                document.getElementById('rdm-max-count').innerText = `${summary.maxCount} Kez`;
                document.getElementById('rdm-min-count').innerText = `${summary.minCount} Kez`;

                document.getElementById('rdm-loading').style.display = 'none';
                document.getElementById('rdm-content').style.display = 'block';

                // 2. YENİ: Trend Analizi için Son 5 veri gününün verilerini çek ve modal içine yerleştir
                let trendContainer = document.getElementById('rdm-trend-analysis');
                if (!trendContainer) {
                    trendContainer = document.createElement('div');
                    trendContainer.id = 'rdm-trend-analysis';
                    document.getElementById('rdm-content').appendChild(trendContainer);
                }

                // HATA BURADAYDI: ÇÖZÜM İÇİN IF KONTROLÜ EKLENDİ
                if (metricType === 'CPU') {
                    // Eğer CPU ise hiç Backend'e istek atma, doğrudan sonucu yazdır
                    trendContainer.innerHTML = window.ui.calculateReportTrend([], 'CPU');
                } else {
                    // RAM veya Disk ise yükleniyor animasyonu göster ve veritabanından çek
                    trendContainer.innerHTML = '<div class="text-center mt-4"><div class="spinner-border spinner-border-sm text-secondary"></div><small class="ms-2 text-muted">Son 5 veri günü baz alınarak trend analizi yapılıyor...</small></div>';

                    let trendApiUrl = `/api/Computer/${computerId}/metrics-trend?metricType=${metricType}`;
                    if (diskName) trendApiUrl += `&diskName=${encodeURIComponent(diskName)}`;

                    const trendData = await window.api.get(trendApiUrl);
                    trendContainer.innerHTML = window.ui.calculateReportTrend(trendData, metricType);
                }

            } catch (error) {
                console.error("Metrik detayları çekilirken hata:", error);
                document.getElementById('rdm-loading').innerHTML = `<div class="text-danger"><i class="bi bi-x-circle fs-4 d-block mb-2"></i> Veriler alınamadı: <br><small>${error.message}</small></div>`;
            }
        },

        // Modal içine eklenecek, veriyi 5'e bölüp hesaplayan trend algoritması
        calculateReportTrend: (dataList, metricType, isHistory = false) => {
            // 1. CPU İÇİN ÖZEL MANTIK
            if (metricType === 'CPU') {
                return `
            <div class="mt-4 p-3 border rounded shadow-sm d-flex align-items-center border-secondary" style="background: rgba(108, 117, 125, 0.1);">
                <i class="bi bi-cpu text-secondary fs-3 me-3"></i>
                <div>
                    <h6 class="mb-1 fw-bold" style="color: var(--text-title); font-size: 0.85rem;">CPU Kullanım Eğilimi</h6>
                    <small style="color: var(--text-muted);">CPU anlık bir işlem birimidir, depolama alanı gibi "dolum" tahmini yapılamaz. Bunun yerine cihazın genel zorlanma seviyesi anlık olarak izlenmelidir.</small>
                </div>
            </div>`;
            }

            // 2. YETERLİ VERİ KONTROLÜ (En az 15 Ham Veri)
            if (!dataList || dataList.length < 15) {
                return `
            <div class="mt-4 p-3 border rounded shadow-sm d-flex align-items-center border-secondary" style="background: rgba(108, 117, 125, 0.1);">
                <i class="bi bi-hourglass text-secondary fs-3 me-3"></i>
                <div>
                    <h6 class="mb-1 fw-bold" style="color: var(--text-title); font-size: 0.85rem;">Tahmini Dolum Analizi</h6>
                    <small style="color: var(--text-muted);">Bilimsel bir trend tahmini yapabilmek için en az 15 metrik ölçümüne ihtiyaç var. Mevcut ölçüm sayısı: ${dataList ? dataList.length : 0}</small>
                </div>
            </div>`;
            }

            // 3. DOĞRUSAL REGRESYON (En Küçük Kareler - OLS)
            // Filtre veya ortalama yok! Tüm ham veri (anlık sıçramalar/indirmeler dahil) formüle giriyor.
            let n = dataList.length;
            let sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;

            // Verilerin zamana göre sıralı olduğundan emin olalım
            let sortedData = [...dataList].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
            let startTime = new Date(sortedData[0].createdAt).getTime();

            sortedData.forEach(p => {
                let x = (new Date(p.createdAt).getTime() - startTime) / 1000; // Saniye cinsinden X ekseni
                let y = p.value; // Yüzde cinsinden Y ekseni
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumXX += x * x;
            });

            let denominator = (n * sumXX) - (sumX * sumX);
            let slope = denominator !== 0 ? ((n * sumXY) - (sumX * sumY)) / denominator : 0;

            // 4. TAHMİNLEME VE ARAYÜZ YAZDIRMA
            let titleText = isHistory ? "Bilimsel Dolum Tahmini (Seçili Tarih Aralığı)" : "Bilimsel Dolum Tahmini (Son 3 Gün)";
            let predictionPrefix = isHistory ? "Seçilen tarih aralığındaki gerçek trende göre" : "Son 3 gündeki gerçek trende göre";

            let predictionText = '';
            let iconClass = 'bi-info-circle text-info';
            let bgClass = 'rgba(13, 202, 240, 0.1)';
            let borderClass = 'border-info';

            // En son gönderilen gerçek veriyi alıyoruz
            let currentVal = sortedData[sortedData.length - 1].value;
            let remainingVal = 100 - currentVal;

            if (slope > 0.00001) { // Eğer yükseliş (eğim) varsa
                if (remainingVal <= 0) {
                    predictionText = "Kritik Uyarı: Bu metrik halihazırda tam kapasiteye (%100) ulaşmış durumda!";
                    iconClass = 'bi-exclamation-octagon text-danger';
                    bgClass = 'rgba(220, 53, 69, 0.1)';
                    borderClass = 'border-danger';
                } else {
                    let timeToFullSeconds = remainingVal / slope;
                    let days = Math.floor(timeToFullSeconds / (60 * 60 * 24));
                    let hours = Math.floor((timeToFullSeconds % (60 * 60 * 24)) / (60 * 60));

                    if (days > 365) {
                        predictionText = "Artış ivmesi çok düşük. Bu hızla devam ederse dolması 1 yıldan uzun sürecek.";
                        iconClass = 'bi-shield-check text-success';
                        bgClass = 'rgba(25, 135, 84, 0.1)';
                        borderClass = 'border-success';
                    } else {
                        predictionText = `${predictionPrefix} yaklaşık <b>${days} gün ${hours} saat</b> sonra kapasite tamamen dolacak.`;
                        iconClass = 'bi-clock-history text-warning';
                        bgClass = 'rgba(255, 193, 7, 0.1)';
                        borderClass = 'border-warning';
                    }
                }
            } else {
                predictionText = "Kullanım düşüş eğiliminde veya tamamen stabil. Dolum riski tespit edilmedi.";
                iconClass = 'bi-graph-down-arrow text-success';
                bgClass = 'rgba(25, 135, 84, 0.1)';
                borderClass = 'border-success';
            }

            // Algoritma Kalite Çıktıları (Geliştirici için)
            let debugInfoHtml = `
        <div class="mt-3 pt-3 border-top border-secondary" style="font-size: 0.75rem; font-family: monospace; opacity: 0.9;">
            <div class="fw-bold text-info mb-1"><i class="bi bi-robot"></i> Algoritma: OLS Linear Regression (Filtresiz)</div>
            <div class="d-flex justify-content-between text-muted small">
                <span>Analize Giren Toplam Ham Veri: ${sortedData.length} Nokta</span>
            </div>
            <div class="text-warning fw-bold mt-1" style="font-size: 0.8rem;">
                Saniye Başına Artış (Slope): ${slope.toFixed(8)}
            </div>
        </div>
    `;

            return `
        <div class="mt-4 p-3 border rounded shadow-sm d-flex align-items-start ${borderClass}" style="background: ${bgClass};">
            <i class="bi ${iconClass} fs-3 me-3 mt-1"></i>
            <div class="w-100">
                <h6 class="mb-1 fw-bold" style="color: var(--text-title); font-size: 0.85rem;">${titleText}</h6>
                <small style="color: var(--text-muted); display: block;">${predictionText}</small>
                ${debugInfoHtml}
            </div>
        </div>
    `;
        },
        renderReportList: (stateKey, tbodyId, valKey, colorClass) => {
            const tbody = document.getElementById(tbodyId);
            if (!tbody) return;

            const ITEMS_PER_PAGE_REPORTS = 2;
            const state = pgState.reports[stateKey];
            const start = (state.page - 1) * ITEMS_PER_PAGE_REPORTS;
            const paginated = state.data.slice(start, start + ITEMS_PER_PAGE_REPORTS);

            if (state.data.length === 0) {
                tbody.innerHTML = `<tr><td class="text-center text-muted py-3 fst-italic" colspan="2">Bu kategoride cihaz yok.</td></tr>`;
                document.getElementById(stateKey + 'Pg').innerHTML = '';
                return;
            }

            const metricType = valKey === 'averageCpu' ? 'CPU' : 'RAM';

            tbody.innerHTML = paginated.map(d => {
                const targetId = d.computerId || d.id;
                return `
                <tr style="border-bottom: 1px solid var(--border-color);">
                    <td class="ps-4 fw-bold align-middle" style="color:var(--text-title);">
                        ${d.computerName}
                        <button class="btn btn-sm btn-link text-info p-0 ms-2" onclick="window.ui.showReportDetails(${targetId}, '${d.computerName}', '${metricType}')" title="Metrik Analizini Gör">
                            <i class="bi bi-info-circle-fill fs-5"></i>
                        </button>
                    </td>
                    <td class="text-end pe-4 align-middle" style="font-family: monospace; font-size: 1.1rem; color: var(--bs-${colorClass});">%${d[valKey]}</td>
                </tr>`;
            }).join('');

            renderPagination(stateKey + 'Pg', state.page, state.data.length, ITEMS_PER_PAGE_REPORTS, `ui.changeReportPage_${stateKey}`);
        },

        changeReportPage_bestCpu: (p) => { pgState.reports.bestCpu.page = p; ui.renderReportList('bestCpu', 'best-cpu-body', 'averageCpu', 'success'); },
        changeReportPage_worstCpu: (p) => { pgState.reports.worstCpu.page = p; ui.renderReportList('worstCpu', 'worst-cpu-body', 'averageCpu', 'danger'); },
        changeReportPage_bestRam: (p) => { pgState.reports.bestRam.page = p; ui.renderReportList('bestRam', 'best-ram-body', 'averageRam', 'success'); },
        changeReportPage_worstRam: (p) => { pgState.reports.worstRam.page = p; ui.renderReportList('worstRam', 'worst-ram-body', 'averageRam', 'danger'); },

        loadReportsView: async () => {
            try {
                const report = await window.api.getPerformanceReport();

                console.log("Gelen Performans Raporu:", report);

                document.getElementById('global-cpu-avg').innerText = `%${report.globalAverageCpu}`;
                document.getElementById('global-ram-avg').innerText = `%${report.globalAverageRam}`;

                if (!report.devices || report.devices.length === 0) {
                    const emptyMsg = `<tr><td class="text-center text-muted py-4">Değerlendirilecek cihaz metriği bulunamadı.</td></tr>`;
                    document.getElementById('best-cpu-body').innerHTML = emptyMsg;
                    document.getElementById('worst-cpu-body').innerHTML = emptyMsg;
                    document.getElementById('best-ram-body').innerHTML = emptyMsg;
                    document.getElementById('worst-ram-body').innerHTML = emptyMsg;
                    return;
                }

                const bestCpu = report.devices.filter(d => d.averageCpu <= report.globalAverageCpu).sort((a, b) => a.averageCpu - b.averageCpu);
                const worstCpu = report.devices.filter(d => d.averageCpu > report.globalAverageCpu).sort((a, b) => b.averageCpu - a.averageCpu);
                const bestRam = report.devices.filter(d => d.averageRam <= report.globalAverageRam).sort((a, b) => a.averageRam - b.averageRam);
                const worstRam = report.devices.filter(d => d.averageRam > report.globalAverageRam).sort((a, b) => b.averageRam - a.averageRam);

                pgState.reports.bestCpu.data = bestCpu; pgState.reports.bestCpu.page = 1;
                pgState.reports.worstCpu.data = worstCpu; pgState.reports.worstCpu.page = 1;
                pgState.reports.bestRam.data = bestRam; pgState.reports.bestRam.page = 1;
                pgState.reports.worstRam.data = worstRam; pgState.reports.worstRam.page = 1;

                ui.renderReportList('bestCpu', 'best-cpu-body', 'averageCpu', 'success');
                ui.renderReportList('worstCpu', 'worst-cpu-body', 'averageCpu', 'danger');
                ui.renderReportList('bestRam', 'best-ram-body', 'averageRam', 'success');
                ui.renderReportList('worstRam', 'worst-ram-body', 'averageRam', 'danger');

                let diskSection = document.getElementById('diskSectionWrapper');

                if (!diskSection) {
                    const mainContainer = document.getElementById('dynamic-content');
                    if (mainContainer) {
                        diskSection = document.createElement('div');
                        diskSection.id = 'diskSectionWrapper';
                        diskSection.className = 'mt-5 pt-3';
                        mainContainer.appendChild(diskSection);
                    }
                }

                if (diskSection) {
                    let globalDisksHtml = '';
                    if (report.globalDiskAverages && report.globalDiskAverages.length > 0) {
                        globalDisksHtml = `<div class="row justify-content-center mb-4">`;
                        report.globalDiskAverages.forEach(gd => {
                            globalDisksHtml += `
                            <div class="col-12 col-sm-6 col-lg mb-3">
                                <div class="card h-100 shadow-sm" style="background-color: var(--bg-card, #1e293b); border-radius: 10px; border: 1px solid var(--border-color, #334155) !important;">
                                    <div class="card-body d-flex flex-column justify-content-center align-items-center py-4">
                                        <div class="fw-bold mb-2 text-uppercase d-flex align-items-center" style="font-size: 0.9rem; letter-spacing: 1px; color: var(--text-muted, #94a3b8);">
                                            <i class="bi bi-hdd-fill me-2 fs-5" style="color: #38bdf8;"></i>${gd.diskName} GENEL ORT.
                                        </div>
                                        <h2 class="fw-bolder mb-0" style="color: var(--text-main, #e2e8f0); font-family: monospace; font-size: 2rem;">
                                            %${gd.averageUsedPercent}
                                        </h2>
                                    </div>
                                </div>
                            </div>`;
                        });
                        globalDisksHtml += `</div>`;
                    }

                    let allDiskCardsHtml = '';

                    report.devices.forEach(device => {
                        if (!device.disks || device.disks.length === 0) return;

                        let cardHtml = `
                        <div class="col-md-4 mb-4">
                            <div class="card h-100 shadow-sm border-0" style="background-color: var(--bg-card, #1e293b); border-radius: 10px; border: 1px solid var(--border-color, #334155) !important;">
                                <div class="card-header fw-bold" style="background-color: transparent; border-bottom: 1px solid var(--border-color, #334155); color: var(--text-main, #e2e8f0);">
                                    <i class="bi bi-hdd-network me-2" style="color: #38bdf8;"></i> ${device.computerName}
                                </div>
                                <div class="card-body" style="color: var(--text-main, #e2e8f0);">
                        `;

                        device.disks.forEach(disk => {
                            let colorClass = "bg-primary";
                            let textClass = "text-primary fw-bold";

                            if (disk.diskStatus === "Kötü") {
                                colorClass = "bg-danger";
                                textClass = "text-danger fw-bold";
                            } else if (disk.diskStatus === "İyi") {
                                colorClass = "bg-success";
                                textClass = "text-success fw-bold";
                            }

                            const targetId = device.computerId || device.id;
                            const safeDiskName = disk.diskName.replace(/\\/g, '\\\\');

                            cardHtml += `
                            <div class="mb-3">
                                <div class="d-flex justify-content-between align-items-center mb-1" style="font-size: 0.9rem;">
                                    <div>
                                        <span>Disk ${disk.diskName} <small style="color: var(--text-muted, #94a3b8);">(${disk.diskStatus})</small></span>
                                        <button class="btn btn-sm btn-link text-info p-0 ms-1" onclick="window.ui.showReportDetails(${targetId}, '${device.computerName}', 'Disk ${safeDiskName}', '${safeDiskName}')" title="Metrik Analizini Gör">
                                            <i class="bi bi-info-circle-fill"></i>
                                        </button>
                                    </div>
                                    <span class="${textClass}">%${disk.averageUsedPercent}</span>
                                </div>
                                <div class="progress" style="height: 8px; background-color: var(--border-color, #334155);">
                                    <div class="progress-bar ${colorClass}" role="progressbar" style="width: ${disk.averageUsedPercent}%"></div>
                                </div>
                            </div>
                            `;
                        });

                        cardHtml += `
                                </div>
                            </div>
                        </div>
                        `;

                        allDiskCardsHtml += cardHtml;
                    });

                    if (allDiskCardsHtml === '') {
                        allDiskCardsHtml = `<div class="col-12 text-muted fst-italic">Henüz disk verisi toplanmamış.</div>`;
                    }

                    diskSection.innerHTML = `
                    <h5 class="fw-bold mb-3" style="color: var(--text-main, #e2e8f0);">
                        <i class="bi bi-device-hdd me-2" style="color: #38bdf8;"></i> Cihaz Disk Durumları
                    </h5>
                    ${globalDisksHtml}
                    <div class="row" id="diskReportsContainer">
                        ${allDiskCardsHtml}
                    </div>
                    `;
                }

            } catch (error) {
                console.error("Rapor çekilirken hata:", error);
                const errorMsg = `<tr><td class="text-center text-danger py-4"><i class="bi bi-exclamation-triangle"></i> Yüklenirken hata oluştu.</td></tr>`;

                const bestCpuBody = document.getElementById('best-cpu-body');
                if (bestCpuBody) {
                    bestCpuBody.innerHTML = errorMsg;
                    document.getElementById('worst-cpu-body').innerHTML = errorMsg;
                    document.getElementById('best-ram-body').innerHTML = errorMsg;
                    document.getElementById('worst-ram-body').innerHTML = errorMsg;
                }
            }
        }
    };

    // --- Tema Başlatma (Sayfa Yüklenince) ---
    (function initTheme() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        document.documentElement.setAttribute('data-theme', savedTheme);
    })();

})();