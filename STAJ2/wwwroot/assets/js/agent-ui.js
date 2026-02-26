// STAJ2/wwwroot/assets/js/agent-ui.js

// --- GRAFİKLER İÇİN EKLENEN GLOBAL DEĞİŞKENLER ---
let historyCharts = { cpu: null, ram: null, disks: {} };
let currentHistoryData = { cpuRam: [], disks: [] };

// --- DEĞİŞİKLİK 1: İki sekme için ayrı etiket dizileri ---
let selectedLiveTags = [];
let selectedAllTags = [];
let allAgents = [];
let allSystemComputers = [];

// --- PAGINATION İÇİN EKLENEN DEĞİŞKENLER ---
let currentLivePage = 1;
let currentAllPage = 1;
const itemsPerPage = 2;

// --- 1. ANA TABLO VE FİLTRELEME ---

window.applyFilter = () => {
    const tags = $('#tagSelect').val() || [];

    const isLiveTab = document.getElementById('nav-computers') && document.getElementById('nav-computers').classList.contains('active');
    const isAllTab = document.getElementById('nav-all-computers') && document.getElementById('nav-all-computers').classList.contains('active');

    if (isLiveTab) {
        selectedLiveTags = tags;
        currentLivePage = 1;
        renderTable();
    } else if (isAllTab) {
        selectedAllTags = tags;
        currentAllPage = 1;
        if (typeof renderAllComputersTable === "function") renderAllComputersTable();
    }
};



window.loadFilterTags = async () => {
    try {
        const tags = await api.get("/api/Computer/tags");

        const $filterSelect = $('#tagSelect');
        const $modalSelect = $('#modalTagSelect');

        // YENİ: Listeyi temizlemeden önce halihazırda seçili olan etiketleri hafızaya al
        const currentFilterVals = $filterSelect.val();
        const currentModalVals = $modalSelect.val();

        $filterSelect.empty();
        $modalSelect.empty();

        tags.forEach(t => {
            $filterSelect.append(new Option(t.name, t.name, false, false));
            $modalSelect.append(new Option(t.name, t.name, false, false));
        });

        // YENİ: Hafızaya alınan seçimleri geri yükle (Eğer silinmemişlerse)
        if (currentFilterVals) $filterSelect.val(currentFilterVals);
        if (currentModalVals) $modalSelect.val(currentModalVals);

        $filterSelect.trigger('change.select2');
        $modalSelect.trigger('change.select2');

    } catch (e) {
        console.error("Filtre etiketleri yüklenemedi", e);
    }
};


async function loadAgents() {
    try {
        const res = await fetch("/api/agent-telemetry/latest", {
            cache: "no-store",
            headers: { "Authorization": `Bearer ${auth.getToken()}` }
        });
        if (!res.ok) throw new Error("Veri çekilemedi");
        allAgents = await res.json();
        renderTable();
    } catch (e) {
        console.error(e);
    }
}

function renderTable() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    // YENİ: Tek bir rol kontrolü yerine spesifik yetkileri (Permissions) alıyoruz
    const canRename = window.auth.hasPermission("Computer.Rename");
    const canSetThreshold = window.auth.hasPermission("Computer.SetThreshold");
    const canAssignTag = window.auth.hasPermission("Computer.AssignTag");
    const canFilterHistory = window.auth.hasPermission("Computer.Filter");

    const canEdit = canRename || canSetThreshold || canAssignTag || canFilterHistory;
    const now = new Date().getTime();

    const liveAndFilteredAgents = allAgents.filter(a => {
        const matchesTags = selectedLiveTags.length === 0 || selectedLiveTags.every(t => a.tags && a.tags.includes(t));
        if (!matchesTags) return false;

        if (!a.ts) return false;
        const agentTime = new Date(a.ts).getTime();
        return (now - agentTime) <= 90000;
    });

    // --- PAGINATION MANTIĞI ---
    const totalPages = Math.ceil(liveAndFilteredAgents.length / itemsPerPage);
    if (currentLivePage > totalPages && totalPages > 0) currentLivePage = totalPages;

    const startIndex = (currentLivePage - 1) * itemsPerPage;
    const paginatedAgents = liveAndFilteredAgents.slice(startIndex, startIndex + itemsPerPage);

    tbody.innerHTML = paginatedAgents.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
        const tags = (a.tags || []).map(t => `<span class="pill" style="font-size:0.65rem; margin-right:3px;">${t}</span>`).join("");

        // YENİ: Butonları sadece o yetkiye sahipse HTML'e ekle
        let actionButtons = `<div style="display:flex; gap:5px;">`;
        if (canRename) actionButtons += `<button class="btn primary small" onclick="handleRename(${a.computerId}, '${a.displayName || a.machineName}')" title="İsim Değiştir">✏️</button>`;
        if (canSetThreshold) actionButtons += `<button class="btn warning small" onclick="openThresholdSettings(${a.computerId})" title="Limit Ayarları">⚙️</button>`;
        if (canAssignTag) actionButtons += `<button class="btn btn-tag small" onclick="openTagModal(${a.computerId})" title="Etiketle">🏷️</button>`;
        if (canFilterHistory) actionButtons += `<button class="btn btn-history small" onclick="openHistoryModal(${a.computerId})" title="Geçmiş Kayıtlar"><i class="bi bi-list-ul"></i></button>`;
        actionButtons += `</div>`;

        let diskContent = "-";
        if (a.diskUsage) {
            diskContent = `<span style="font-size:0.8rem; color:var(--text-main);">${a.diskUsage}</span>`;
        }

        const ipDisplay = a.ip || "-";
        const cpuLimit = a.cpuThreshold || 90;
        const ramLimit = a.ramThreshold || 90;
        const cpuColor = (a.cpuUsage > cpuLimit) ? "#ef4444" : "var(--text-main)";
        const ramColor = (a.ramUsage > ramLimit) ? "#ef4444" : "var(--text-main)";

        return `
            <tr style="color: var(--text-main) !important;">
                <td>
                    <div class="fw-bold" style="color:var(--text-title);">${a.displayName || a.machineName}</div>
                    <div style="margin-top:2px;">${tags}</div>
                    <div style="font-size:0.7rem; color:var(--text-muted); margin-top:2px;">${ts}</div>
                </td>
                <td style="color:var(--text-muted); font-family:monospace;">${ipDisplay}</td>
                <td style="font-weight:bold; color:${cpuColor};">${Math.round(a.cpuUsage)}%</td>
                <td style="font-weight:bold; color:${ramColor};">${Math.round(a.ramUsage)}%</td>
                <td style="color: var(--text-main) !important;">${diskContent}</td>
                <td><span class="badge bg-success">Aktif</span></td>
                ${canEdit ? `<td>${actionButtons}</td>` : ''}
            </tr>
        `;
    }).join("");
    renderPaginationControls('livePagination', currentLivePage, totalPages, 'changeLivePage');
}

// --- 2. YÖNETİM FONKSİYONLARI ---

window.handleRename = (id, currentName) => {
    document.getElementById("renameComputerId").value = id;
    document.getElementById("newComputerName").value = currentName;
    const modal = new bootstrap.Modal(document.getElementById("renameModal"));
    modal.show();
};

window.saveComputerName = async () => {
    const id = document.getElementById("renameComputerId").value;
    const newName = document.getElementById("newComputerName").value.trim();
    if (!newName) return alert("İsim alanı boş bırakılamaz!");
    if (newName.length > 200) return alert("Bilgisayar ismi 200 karakterden uzun olamaz!");

    try {
        await api.put(`/api/Computer/update-display-name`, { id: parseInt(id), newDisplayName: newName });
        const modal = bootstrap.Modal.getInstance(document.getElementById("renameModal"));
        modal.hide();
        loadAgents();
        if (typeof loadAllComputers === "function") loadAllComputers();
    } catch (e) { alert(e.message); }
};

window.openTagModal = async (id) => {
    document.getElementById("tagModalComputerId").value = id;
    const modal = new bootstrap.Modal(document.getElementById("tagsModal"));

    let agent = allAgents.find(a => a.computerId == id);
    if (!agent) agent = allSystemComputers.find(c => c.id == id);

    const existingTags = agent ? (agent.tags || []) : [];
    $('#modalTagSelect').val(existingTags).trigger('change');

    modal.show();
};

window.saveTags = async () => {
    const id = document.getElementById("tagModalComputerId").value;
    const selectedTags = $('#modalTagSelect').val() || [];

    try {
        await api.put(`/api/Computer/${id}/tags`, { tags: selectedTags });
        const modal = bootstrap.Modal.getInstance(document.getElementById("tagsModal"));
        modal.hide();
        loadAgents();
        if (typeof loadAllComputers === "function") loadAllComputers();
    } catch (e) { alert(e.message); }
};

window.openThresholdSettings = async (id) => {
    document.getElementById("modalComputerId").value = id;
    document.getElementById("cpuThresholdInput").value = "";
    document.getElementById("ramThresholdInput").value = "";
    document.getElementById("cpuThresholdInput").placeholder = "Yükleniyor...";
    document.getElementById("ramThresholdInput").placeholder = "Yükleniyor...";

    const container = document.getElementById("diskThresholdsContainer");
    container.innerHTML = '<div class="text-center"><div class="spinner-border spinner-border-sm text-info"></div></div>';

    try {
        const computerData = await api.get(`/api/Computer/${id}`);
        document.getElementById("cpuThresholdInput").value = computerData.cpuThreshold || "";
        document.getElementById("ramThresholdInput").value = computerData.ramThreshold || "";

        const disks = await api.get(`/api/Computer/${id}/disks`);

        if (!disks || disks.length === 0) {
            container.innerHTML = '<div class="text-muted small text-center">Disk bulunamadı.</div>';
        } else {
            let html = "";
            disks.forEach(d => {
                html += `
                    <div class="disk-row mb-2 d-flex align-items-center justify-content-between p-2 rounded" style="background:var(--bg-hover); border:1px solid var(--border-color);">
                        <span class="fw-bold" style="color:var(--text-main);">${d.diskName}</span>
                        <div class="d-flex align-items-center gap-2">
                            <label class="small text-muted mb-0">Eşik %:</label>
                            <input type="number" 
                                   data-diskname="${d.diskName}"
                                   class="form-control form-control-sm disk-threshold-input" 
                                   value="${d.thresholdPercent || 90}" 
                                   min="0" max="100"
                                   style="width:70px; background:var(--bg-input); color:var(--text-input); border-color:var(--border-input);">
                        </div>
                    </div>`;
            });
            container.innerHTML = html;
        }
    } catch (e) {
        console.error(e);
        container.innerHTML = '<div class="text-danger small">Veri alınamadı.</div>';
    }

    const modal = new bootstrap.Modal(document.getElementById("thresholdModal"));
    modal.show();
};

window.saveThresholdsWithValidation = async () => {
    const id = document.getElementById("modalComputerId").value;
    const cpuVal = document.getElementById("cpuThresholdInput").value;
    const ramVal = document.getElementById("ramThresholdInput").value;

    const cpu = cpuVal ? parseInt(cpuVal) : null;
    const ram = ramVal ? parseInt(ramVal) : null;

    const diskInputs = document.querySelectorAll('.disk-threshold-input');
    const diskThresholdsList = [];

    diskInputs.forEach(input => {
        const name = input.getAttribute("data-diskname");
        const val = input.value ? parseInt(input.value) : null;
        if (name) {
            diskThresholdsList.push({ diskName: name, thresholdPercent: val });
        }
    });

    const payload = { cpuThreshold: cpu, ramThreshold: ram, diskThresholds: diskThresholdsList };

    try {
        await api.put(`/api/Computer/update-thresholds/${id}`, payload);
        const modal = bootstrap.Modal.getInstance(document.getElementById("thresholdModal"));
        modal.hide();
        loadAgents();
        if (typeof loadAllComputers === "function") loadAllComputers();
    } catch (e) { alert("Hata: " + e.message); }
};

// --- 3. GEÇMİŞ METRİK FONKSİYONLARI (GRAFİKLİ SOL MENÜLÜ YAPI) ---

window.openHistoryModal = (id) => {
    document.getElementById("historyComputerId").value = id;

    if (historyCharts.cpu) historyCharts.cpu.destroy();
    if (historyCharts.ram) historyCharts.ram.destroy();
    Object.values(historyCharts.disks).forEach(chart => chart.destroy());
    historyCharts.disks = {};

    const dynamicDiskCharts = document.getElementById("dynamicDiskCharts");
    if (dynamicDiskCharts) dynamicDiskCharts.innerHTML = "";

    const diskCheckboxes = document.getElementById("diskCheckboxes");
    if (diskCheckboxes) diskCheckboxes.innerHTML = "";

    const diskFiltersContainer = document.getElementById("diskFiltersContainer");
    if (diskFiltersContainer) diskFiltersContainer.style.display = "none";

    document.getElementById("historyResults").style.display = "none";
    document.getElementById("historyPlaceholder").style.display = "block";

    const now = new Date();
    const yesterday = new Date(now.getTime() - (24 * 60 * 60 * 1000));

    const formatToInput = (date) => {
        const offset = date.getTimezoneOffset() * 60000;
        return new Date(date.getTime() - offset).toISOString().slice(0, 16);
    };

    document.getElementById("historyStart").value = formatToInput(yesterday);
    document.getElementById("historyEnd").value = formatToInput(now);

    new bootstrap.Modal(document.getElementById("historyModal")).show();
};

window.fetchHistoryMetrics = async () => {
    const id = document.getElementById("historyComputerId").value;
    const start = document.getElementById("historyStart").value;
    const end = document.getElementById("historyEnd").value;

    if (!start || !end) return alert("Lütfen tarih aralığı seçiniz.");

    const results = document.getElementById("historyResults");
    const placeholder = document.getElementById("historyPlaceholder");

    placeholder.innerHTML = '<div class="text-center py-5 mt-5"><div class="spinner-border text-info"></div><div class="mt-3 text-muted">Veriler analiz ediliyor...</div></div>';
    placeholder.style.display = "block";
    results.style.display = "none";
    document.getElementById("diskFiltersContainer").style.display = "none";

    try {
        const data = await api.get(`/api/Computer/${id}/metrics-history?start=${start}&end=${end}`);

        if (!data.cpuRam || data.cpuRam.length === 0) {
            placeholder.innerHTML = '<div class="text-center py-5 mt-5"><h5 class="text-muted fw-light mt-4">Bu tarih aralığında kayıt bulunamadı.</h5></div>';
            return;
        }

        currentHistoryData = data;

        if (currentHistoryData.cpuRam) {
            currentHistoryData.cpuRam.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
        }
        if (currentHistoryData.disks) {
            currentHistoryData.disks.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
        }

        placeholder.style.display = "none";
        results.style.display = "block";
        document.getElementById("diskFiltersContainer").style.display = "block";

        renderBaseCharts(data.cpuRam);
        generateDiskFilters(data.disks);

        placeholder.innerHTML = `<div class="opacity-25 mb-3 text-muted"><i class="bi bi-graph-up display-1"></i></div><h4 class="text-muted fw-light">Lütfen sol taraftan tarih aralığı seçerek analize başlayın.</h4>`;

    } catch (e) {
        placeholder.innerHTML = `<div class="text-center py-5 mt-5 text-danger"><i class="bi bi-exclamation-triangle me-2"></i> Metrikler yüklenirken hata: ${e.message}</div>`;
    }
};

function renderBaseCharts(cpuRamData) {
    const labels = cpuRamData.map(m => formatChartDate(m.createdAt));
    const cpuData = cpuRamData.map(m => m.cpuUsage);
    const ramData = cpuRamData.map(m => m.ramUsage);

    if (historyCharts.cpu) historyCharts.cpu.destroy();
    if (historyCharts.ram) historyCharts.ram.destroy();

    historyCharts.cpu = createLineChart('cpuChart', 'CPU Kullanımı (%)', labels, cpuData, '#38bdf8');
    historyCharts.ram = createLineChart('ramChart', 'RAM Kullanımı (%)', labels, ramData, '#facc15');
}

function formatChartDate(dateString) {
    const d = new Date(dateString);
    return d.toLocaleDateString('tr-TR', {
        day: 'numeric',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function generateDiskFilters(disksData) {
    const diskNames = [...new Set(disksData.map(d => d.diskName))];

    const container = document.getElementById('diskCheckboxes');
    if (!container) return;
    container.innerHTML = '';

    const dynamicChartsContainer = document.getElementById('dynamicDiskCharts');
    if (dynamicChartsContainer) dynamicChartsContainer.innerHTML = '';

    historyCharts.disks = {};

    diskNames.forEach(diskName => {
        const checkboxHtml = `
            <div class="col-4">
                <div class="form-check form-switch p-2 border rounded d-flex flex-column align-items-center justify-content-center text-center shadow-sm h-100" style="background: var(--bg-card); border-color: var(--border-color) !important; margin: 0; padding-left: 0 !important;">
                    <label class="form-check-label fw-bold mb-1 w-100" style="font-size:0.75rem; color:var(--text-main); cursor:pointer;" for="chk_disk_${diskName}">${diskName}</label>
                    <input class="form-check-input disk-toggle custom-toggle m-0" type="checkbox" id="chk_disk_${diskName}" value="${diskName}" style="float:none; width: 2.2em; height: 1.1em;">
                </div>
            </div>
        `;
        container.insertAdjacentHTML('beforeend', checkboxHtml);

        const chartId = `diskChart_${diskName.replace(/[^a-zA-Z0-9]/g, '')}`;

        const chartHtml = `
            <div class="card border border-secondary shadow-sm" id="container_${chartId}" style="background-color: var(--bg-card); display: none;">
                <div class="card-body p-2" style="overflow: hidden;">
                    <div style="position: relative; height: 180px; width: 100%;">
                        <canvas id="${chartId}"></canvas>
                    </div>
                </div>
            </div>
        `;
        dynamicChartsContainer.insertAdjacentHTML('beforeend', chartHtml);

        document.getElementById(`chk_disk_${diskName}`).addEventListener('change', function () {
            toggleDiskChart(this.checked, diskName, chartId);
        });
    });
}

function toggleDiskChart(isVisible, diskName, chartId) {
    const container = document.getElementById(`container_${chartId}`);

    if (isVisible) {
        container.style.display = 'block';

        const diskData = currentHistoryData.disks.filter(d => d.diskName === diskName);

        const labels = diskData.map(d => formatChartDate(d.createdAt));
        const diskUsageData = diskData.map(d => d.usedPercent);

        historyCharts.disks[diskName] = createLineChart(chartId, `${diskName} Doluluk Oranı (%)`, labels, diskUsageData, '#10b981');
    } else {
        container.style.display = 'none';
        if (historyCharts.disks[diskName]) {
            historyCharts.disks[diskName].destroy();
            delete historyCharts.disks[diskName];
        }
    }
}

function createLineChart(canvasId, labelText, labels, dataPoints, colorHex) {
    const ctx = document.getElementById(canvasId).getContext('2d');

    const isLight = document.documentElement.getAttribute('data-theme') === 'light';
    const textColor = isLight ? '#334155' : '#e2e8f0';
    const gridColor = isLight ? '#cbd5e1' : '#334155';

    return new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: labelText,
                data: dataPoints,
                borderColor: colorHex,
                backgroundColor: colorHex + '33',
                borderWidth: 2,
                tension: 0.3,
                fill: true,
                pointRadius: 1,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            plugins: {
                legend: { labels: { color: textColor, font: { weight: 'bold' } } },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                x: { ticks: { color: textColor, maxTicksLimit: 10 }, grid: { color: gridColor } },
                y: { min: 0, max: 100, ticks: { color: textColor }, grid: { color: gridColor } }
            }
        }
    });
}

window.loadAllComputers = async () => {
    try {
        const res = await fetch("/api/Computer", {
            headers: { "Authorization": `Bearer ${auth.getToken()}` }
        });
        if (!res.ok) throw new Error("Cihazlar çekilemedi");
        allSystemComputers = await res.json();
        renderAllComputersTable();
    } catch (e) {
        console.error(e);
    }
};

window.renderAllComputersTable = () => {
    const tbody = document.getElementById("allComputersRows");
    if (!tbody) return;

    // YENİ: Tüm bilgisayarlar sekmesi için de ayrı yetki kontrolleri
    const canRename = window.auth.hasPermission("Computer.Rename");
    const canSetThreshold = window.auth.hasPermission("Computer.SetThreshold");
    const canAssignTag = window.auth.hasPermission("Computer.AssignTag");
    const canFilterHistory = window.auth.hasPermission("Computer.Filter");
    const canDelete = window.auth.hasPermission("Computer.Delete");

    const canEdit = canRename || canSetThreshold || canAssignTag || canFilterHistory || canDelete;

    const filtered = selectedAllTags.length === 0
        ? allSystemComputers
        : allSystemComputers.filter(a => selectedAllTags.every(t => a.tags && a.tags.includes(t)));

    // --- PAGINATION MANTIĞI ---
    const totalPages = Math.ceil(filtered.length / itemsPerPage);
    if (currentAllPage > totalPages && totalPages > 0) currentAllPage = totalPages;

    const startIndex = (currentAllPage - 1) * itemsPerPage;
    const paginatedComputers = filtered.slice(startIndex, startIndex + itemsPerPage);

    tbody.innerHTML = paginatedComputers.map(c => {
        const lastSeen = new Date(c.lastSeen).toLocaleString();
        const tags = (c.tags || []).map(t => `<span class="pill" style="font-size:0.65rem; margin-right:3px;">${t}</span>`).join("");

        let statusBadge = "";
        if (c.isDeleted) {
            statusBadge = `<span class="badge bg-danger">Silinmiş</span>`;
        } else if (c.isActive) {
            statusBadge = `<span class="badge bg-success">Aktif</span>`;
        } else {
            statusBadge = `<span class="badge bg-secondary">Pasif</span>`;
        }

        // YENİ: Butonları o işleme özel yetkiye göre ekliyoruz
        let actionButtons = `<div style="display:flex; gap:5px;">`;
        if (!c.isDeleted) {
            if (canRename) actionButtons += `<button class="btn primary small" onclick="handleRename(${c.id}, '${c.displayName || c.machineName}')" title="İsim Değiştir">✏️</button>`;
            if (canSetThreshold) actionButtons += `<button class="btn warning small" onclick="openThresholdSettings(${c.id})" title="Limit Ayarları">⚙️</button>`;
            if (canAssignTag) actionButtons += `<button class="btn btn-tag small" onclick="openTagModal(${c.id})" title="Etiketle">🏷️</button>`;
        }

        if (canFilterHistory) actionButtons += `<button class="btn btn-history small" onclick="openHistoryModal(${c.id})" title="Geçmiş Kayıtlar"><i class="bi bi-list-ul"></i></button>`;

        if (!c.isActive && !c.isDeleted && canDelete) {
            actionButtons += `<button class="btn danger small" onclick="deleteComputer(${c.id})" title="Sil">🗑️</button>`;
        }
        actionButtons += `</div>`;

        return `
            <tr style="color: var(--text-main) !important; ${c.isDeleted ? 'opacity: 0.6;' : ''}">
                <td>
                    <div class="fw-bold" style="color:var(--text-title); ${c.isDeleted ? 'text-decoration: line-through;' : ''}">${c.displayName || c.machineName}</div>
                    <div style="margin-top:2px;">${tags}</div>
                </td>
                <td style="color:var(--text-muted); font-family:monospace;">${c.ipAddress || "-"}</td>
                <td style="font-size:0.85rem; color:var(--text-muted);">${lastSeen}</td>
                <td>${statusBadge}</td>
                ${canEdit ? `<td>${actionButtons}</td>` : ''}
            </tr>
        `;
    }).join("");
    renderPaginationControls('allPagination', currentAllPage, totalPages, 'changeAllPage');
};

window.deleteComputer = async (id) => {
    if (!confirm("Bu bilgisayarı silmek istediğinize emin misiniz? (Geçmiş metrikleri filtrelenerek bulunmaya devam edecektir)")) return;

    try {
        await api.del(`/api/Computer/${id}`);
        loadAllComputers();
        loadAgents();
        if (typeof loadAllComputers === "function") loadAllComputers();
    } catch (e) {
        alert(e.message);
    }
};

window.changeLivePage = (page) => {
    currentLivePage = page;
    renderTable();
};

window.changeAllPage = (page) => {
    currentAllPage = page;
    renderAllComputersTable();
};

function renderPaginationControls(containerId, currentPage, totalPages, changeFnName) {
    const container = document.getElementById(containerId);
    if (!container) return;

    if (totalPages <= 1) {
        container.innerHTML = '';
        return;
    }

    let html = '<ul class="pagination pagination-sm mb-0 shadow-sm">';

    html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="javascript:void(0)" onclick="window.${changeFnName}(${currentPage - 1})">Önceki</a>
             </li>`;

    for (let i = 1; i <= totalPages; i++) {
        html += `<li class="page-item ${currentPage === i ? 'active' : ''}">
                    <a class="page-link" href="javascript:void(0)" onclick="window.${changeFnName}(${i})">${i}</a>
                 </li>`;
    }

    html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="javascript:void(0)" onclick="window.${changeFnName}(${currentPage + 1})">Sonraki</a>
             </li>`;

    html += '</ul>';
    container.innerHTML = html;
}

// --- BAŞLATMA ---
$(document).ready(function () {
    $('#modalTagSelect').select2({
        dropdownParent: $('#tagsModal'),
        tags: false,
        placeholder: "Sistemden bir etiket seçiniz..."
    });

    $(document).on('select2:open', function () {
        document.querySelectorAll('.select2-search__field').forEach(input => {
            input.readOnly = true;
            input.style.cursor = 'pointer';
        });
    });

    $('#main-nav').on('click', '.nav-link', function () {
        setTimeout(() => {
            const id = $(this).attr('id');
            if (id === 'nav-computers') {
                $('#tagSelect').val(selectedLiveTags).trigger('change.select2');
            } else if (id === 'nav-all-computers') {
                $('#tagSelect').val(selectedAllTags).trigger('change.select2');
            } else {
                $('#tagSelect').val([]).trigger('change.select2');
            }
        }, 50);
    });

    window.loadFilterTags();
    loadAgents();
    setInterval(loadAgents, 5000);
});