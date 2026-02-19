// STAJ2/wwwroot/assets/js/agent-ui.js

// --- DEĞİŞİKLİK 1: İki sekme için ayrı etiket dizileri ---
let selectedLiveTags = [];
let selectedAllTags = [];
let allAgents = [];
let allSystemComputers = [];

// --- 1. ANA TABLO VE FİLTRELEME ---

window.applyFilter = () => {
    const tags = $('#tagSelect').val() || [];

    // DEĞİŞİKLİK 2: Hangi sekmenin aktif olduğunu kontrol ediyoruz
    const isLiveTab = document.getElementById('nav-computers') && document.getElementById('nav-computers').classList.contains('active');
    const isAllTab = document.getElementById('nav-all-computers') && document.getElementById('nav-all-computers').classList.contains('active');

    // Filtreyi sadece aktif olan sekmenin hafızasına yazıp, o tabloyu yeniliyoruz
    if (isLiveTab) {
        selectedLiveTags = tags;
        renderTable();
    } else if (isAllTab) {
        selectedAllTags = tags;
        if (typeof renderAllComputersTable === "function") renderAllComputersTable();
    }
};

async function loadFilterTags() {
    try {
        const tags = await api.get("/api/Users/tags");

        const $filterSelect = $('#tagSelect');
        const $modalSelect = $('#modalTagSelect');

        $filterSelect.empty();
        $modalSelect.empty();

        tags.forEach(t => {
            $filterSelect.append(new Option(t.name, t.name, false, false));
            $modalSelect.append(new Option(t.name, t.name, false, false));
        });

        $filterSelect.trigger('change');
        $modalSelect.trigger('change');

    } catch (e) {
        console.error("Filtre etiketleri yüklenemedi", e);
    }
}

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

    const canEdit = auth.hasRole("Yönetici") || auth.hasRole("Denetleyici");
    const now = new Date().getTime();

    // DEĞİŞİKLİK 3: Sadece Canlı sekmesine ait etiket (selectedLiveTags) filtresini uyguluyoruz
    const liveAndFilteredAgents = allAgents.filter(a => {
        const matchesTags = selectedLiveTags.length === 0 || selectedLiveTags.every(t => a.tags && a.tags.includes(t));
        if (!matchesTags) return false;

        if (!a.ts) return false;
        const agentTime = new Date(a.ts).getTime();
        return (now - agentTime) <= 90000;
    });

    tbody.innerHTML = liveAndFilteredAgents.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
        const tags = (a.tags || []).map(t => `<span class="pill" style="font-size:0.65rem; margin-right:3px;">${t}</span>`).join("");

        const actionButtons = canEdit ? `
            <div style="display:flex; gap:5px;">
                <button class="btn primary small" onclick="handleRename(${a.computerId}, '${a.displayName || a.machineName}')" title="İsim Değiştir">✏️</button>
                <button class="btn warning small" onclick="openThresholdSettings(${a.computerId})" title="Limit Ayarları">⚙️</button>
                <button class="btn btn-tag small" onclick="openTagModal(${a.computerId})" title="Etiketle">🏷️</button>
                <button class="btn btn-history small" onclick="openHistoryModal(${a.computerId})" title="Geçmiş Kayıtlar">
                    <i class="bi bi-list-ul"></i>
                </button>
            </div>` : "";

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

// --- 3. GEÇMİŞ METRİK FONKSİYONLARI ---

window.openHistoryModal = (id) => {
    document.getElementById("historyComputerId").value = id;
    document.getElementById("historyTableBody").innerHTML = "";
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

    const container = document.getElementById("historyTableBody");
    const results = document.getElementById("historyResults");
    const placeholder = document.getElementById("historyPlaceholder");

    container.innerHTML = '<tr><td colspan="4" class="text-center p-5"><div class="spinner-border text-info"></div></td></tr>';
    placeholder.style.display = "none";
    results.style.display = "block";

    try {
        const data = await api.get(`/api/Computer/${id}/metrics-history?start=${start}&end=${end}`);

        if (!data.cpuRam || data.cpuRam.length === 0) {
            container.innerHTML = '<tr><td colspan="4" class="text-center p-4 text-muted">Kayıt bulunamadı.</td></tr>';
            return;
        }

        container.innerHTML = data.cpuRam.map(m => {
            const time = new Date(m.createdAt).toLocaleString();

            const disks = data.disks
                .filter(d => Math.abs(new Date(d.createdAt).getTime() - new Date(m.createdAt).getTime()) < 10000)
                .map(d => `<span class="badge bg-secondary me-1" style="font-weight:500;">${d.diskName}: %${Math.round(d.usedPercent)}</span>`)
                .join("");

            return `
                <tr class="border-bottom border-secondary">
                    <td class="small ps-4 fw-bold text-light opacity-75">${time}</td>
                    <td><span class="badge bg-info text-dark fw-bold px-3 py-2" style="font-size:0.9rem;">%${Math.round(m.cpuUsage)}</span></td>
                    <td><span class="badge bg-warning text-dark fw-bold px-3 py-2" style="font-size:0.9rem;">%${Math.round(m.ramUsage)}</span></td>
                    <td class="pe-4 text-light">${disks || '<span class="text-muted small">Veri yok</span>'}</td>
                </tr>`;
        }).join("");

    } catch (e) {
        container.innerHTML = `<tr><td colspan="4" class="text-center text-danger p-5"><i class="bi bi-exclamation-triangle me-2"></i> ${e.message}</td></tr>`;
    }
};

// --- 4. TÜM BİLGİSAYARLAR SEKMESİ ---

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

    const canEdit = auth.hasRole("Yönetici") || auth.hasRole("Denetleyici");

    // DEĞİŞİKLİK 4: Tüm bilgisayarlar sekmesine ait etiket (selectedAllTags) filtresini uyguluyoruz
    const filtered = selectedAllTags.length === 0
        ? allSystemComputers
        : allSystemComputers.filter(a => selectedAllTags.every(t => a.tags && a.tags.includes(t)));

    tbody.innerHTML = filtered.map(c => {
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

        const actionButtons = canEdit ? `
            <div style="display:flex; gap:5px;">
                ${!c.isDeleted ? `
                    <button class="btn primary small" onclick="handleRename(${c.id}, '${c.displayName || c.machineName}')" title="İsim Değiştir">✏️</button>
                    <button class="btn warning small" onclick="openThresholdSettings(${c.id})" title="Limit Ayarları">⚙️</button>
                    <button class="btn btn-tag small" onclick="openTagModal(${c.id})" title="Etiketle">🏷️</button>
                ` : ""}
                <button class="btn btn-history small" onclick="openHistoryModal(${c.id})" title="Geçmiş Kayıtlar"><i class="bi bi-list-ul"></i></button>
                ${(!c.isActive && !c.isDeleted) ? `
                    <button class="btn danger small" onclick="deleteComputer(${c.id})" title="Sil">🗑️</button>
                ` : ""}
            </div>` : "";

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

// --- BAŞLATMA ---
$(document).ready(function () {
    $('#modalTagSelect').select2({
        dropdownParent: $('#tagsModal'),
        tags: false,
        placeholder: "Sistemden bir etiket seçiniz..."
    });

    // --- YENİ EKLENEN KOD: YAZI YAZMAYI VE ARAMAYI TAMAMEN KAPATIR ---
    // Herhangi bir select2 menüsü açıldığında, içindeki yazı alanını "Sadece Okunabilir" yapar
    $(document).on('select2:open', function () {
        document.querySelectorAll('.select2-search__field').forEach(input => {
            input.readOnly = true;        // Klavye girişini engeller
            input.style.cursor = 'pointer'; // Fare imlecini yazı imlecinden çıkartıp el işaretine çevirir
        });
    });
    // ------------------------------------------------------------------

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

    loadFilterTags();
    loadAgents();
    setInterval(loadAgents, 5000);
});