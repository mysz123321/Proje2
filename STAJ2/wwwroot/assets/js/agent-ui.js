// STAJ2/wwwroot/assets/js/agent-ui.js

let selectedTags = [];
let allAgents = [];

// Modal Instance'ları (Global)
let thresholdModalInstance = null;
let renameModalInstance = null;
let tagsModalInstance = null;

// --- 1. VERİ YÜKLEME ---

window.loadAgents = async function () {
    try {
        // Tablo yoksa işlem yapma (Başka bir sayfada olabiliriz)
        if (!document.getElementById("agentRows")) return;

        const res = await fetch("/api/agent-telemetry/latest?_=" + new Date().getTime(), {
            cache: "no-store",
            headers: auth.getAuthHeaders()
        });

        if (!res.ok) throw new Error("Veri çekilemedi");
        allAgents = await res.json();
        renderTable();
    } catch (e) {
        console.error(e);
        const tbody = document.getElementById("agentRows");
        if (tbody) tbody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Bağlantı hatası: ${e.message}</td></tr>`;
    }
};

window.applyFilter = function () {
    selectedTags = $('#tagSelect').val() || [];
    renderTable();
};

window.loadFilterTags = async function () {
    try {
        const tags = await api.get("/api/Users/tags");
        const $select = $('#tagSelect');
        $select.empty();
        tags.forEach(t => { $select.append(new Option(t.name, t.name, false, false)); });
        $select.trigger('change'); // Select2 güncellensin
    } catch (e) { console.error("Filtreler yüklenemedi", e); }
};

// --- 2. TABLO VE BUTONLAR ---

function renderTable() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    // YETKİ KONTROLÜ: Yönetici VEYA Denetleyici düzenleme yapabilir
    const canEdit = auth.hasRole("Yönetici") || auth.hasRole("Denetleyici");

    // Filtreleme
    const filtered = selectedTags.length === 0
        ? allAgents
        : allAgents.filter(a => selectedTags.every(t => a.tags && a.tags.includes(t)));

    if (filtered.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">Gösterilecek cihaz bulunamadı.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleTimeString() : "-";

        // Etiket Rozetleri
        const tagsHtml = (a.tags || []).map(t =>
            `<span class="badge bg-primary text-white me-1" style="font-size:0.7rem;">${t}</span>`
        ).join("");

        const safeName = (a.displayName || a.machineName || "").replace(/'/g, "\\'");

        // BUTONLAR (canEdit true ise görünür)
        const actionButtons = canEdit ? `
            <div class="btn-group btn-group-sm" role="group">
                <button class="btn btn-outline-warning text-white" onclick="openRenameModal(${a.computerId}, '${safeName}')" title="İsim Değiştir">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-outline-primary text-white" onclick="openTagsModal(${a.computerId})" title="Etiketle">
                    <i class="bi bi-tags-fill"></i>
                </button>
                <button class="btn btn-outline-info text-white" onclick="openThresholdSettings(${a.computerId})" title="Eşik Ayarları">
                    <i class="bi bi-gear-fill"></i>
                </button>
            </div>` : "";

        return `
        <tr>
            <td>
                <div class="fw-bold text-white">${a.displayName || a.machineName}</div>
                <div class="mt-1">${tagsHtml}</div>
            </td>
            <td class="align-middle">${a.ip || "-"}</td>
            <td class="align-middle"><span class="badge ${a.cpuUsage > 80 ? 'bg-danger' : 'bg-success'}">%${a.cpuUsage?.toFixed(1) ?? "0"}</span></td>
            <td class="align-middle"><span class="badge ${a.ramUsage > 80 ? 'bg-danger' : 'bg-success'}">%${a.ramUsage?.toFixed(1) ?? "0"}</span></td>
            <td class="align-middle small text-muted">${formatDisks(a)}</td>
            <td class="align-middle small text-muted">${ts}</td>
            <td class="align-middle text-end">${actionButtons}</td>
        </tr>`;
    }).join("");
}

function formatDisks(a) {
    if (!a.diskUsage || !a.totalDiskGb) return "-";
    const u = a.diskUsage.split(' ').filter(x => x);
    const s = a.totalDiskGb.split(' ').filter(x => x);
    let ds = [];
    for (let i = 0; i < u.length; i += 2) {
        const label = u[i];
        const usage = u[i + 1];
        const szIdx = s.indexOf(label);
        const sz = szIdx !== -1 ? parseFloat(s[szIdx + 1]).toFixed(0) : "?";
        ds.push(`<div style="white-space:nowrap;"><span class="text-info fw-bold">${label}</span> <span class="text-white">${usage}</span> <span class="text-secondary">(${sz}GB)</span></div>`);
    }
    return ds.join('');
}

// --- 3. İŞLEVSEL FONKSİYONLAR (MODALLAR) ---

// A. İSİM DEĞİŞTİRME
window.openRenameModal = function (id, currentName) {
    document.getElementById('renameComputerId').value = id;
    document.getElementById('currentComputerName').value = currentName;
    document.getElementById('newComputerName').value = "";

    const el = document.getElementById('renameModal');
    if (renameModalInstance) renameModalInstance.dispose();
    renameModalInstance = new bootstrap.Modal(el);
    renameModalInstance.show();
};

window.saveComputerName = async function () {
    const id = document.getElementById('renameComputerId').value;
    const newName = document.getElementById('newComputerName').value.trim();
    if (!newName) return alert("İsim boş olamaz.");

    try {
        // GÜNCEL ENDPOINT: ComputerController
        await api.put("/api/Computer/update-display-name", { id: parseInt(id), newDisplayName: newName });
        renameModalInstance.hide();
        loadAgents();
    } catch (e) { alert(e.message); }
};

// B. ETİKETLEME (AYRI PENCERE)
window.openTagsModal = async function (computerId) {
    document.getElementById('tagModalComputerId').value = computerId;

    try {
        const el = document.getElementById('tagsModal');
        if (tagsModalInstance) tagsModalInstance.dispose();
        tagsModalInstance = new bootstrap.Modal(el);

        // GÜNCEL ENDPOINT: ComputerController
        const [details, allTags] = await Promise.all([
            api.get(`/api/Computer/${computerId}`), // /api/Computer/{id} -> Detay
            api.get("/api/Users/tags")
        ]);

        const $select = $('#modalTagSelect');
        $select.empty();

        allTags.forEach(t => {
            const isSelected = details.tags && details.tags.includes(t.name);
            $select.append(new Option(t.name, t.name, isSelected, isSelected));
        });

        $select.select2({
            dropdownParent: $('#tagsModal'),
            width: '100%',
            placeholder: "Etiket seçin..."
        }).trigger('change');

        tagsModalInstance.show();
    } catch (e) { alert("Etiketler yüklenemedi: " + e.message); }
};

window.saveTags = async function () {
    const id = document.getElementById('tagModalComputerId').value;
    const tags = $('#modalTagSelect').val() || [];

    try {
        // GÜNCEL ENDPOINT: ComputerController
        await api.put(`/api/Computer/${id}/tags`, { tags: tags });
        tagsModalInstance.hide();
        loadAgents();
        alert("✅ Etiketler güncellendi.");
    } catch (e) { alert(e.message); }
};

// C. EŞİK AYARLARI
window.openThresholdSettings = async function (computerId) {
    document.getElementById('modalComputerId').value = computerId;

    try {
        const el = document.getElementById('thresholdModal');
        if (thresholdModalInstance) thresholdModalInstance.dispose();
        thresholdModalInstance = new bootstrap.Modal(el);

        // GÜNCEL ENDPOINTLER: ComputerController
        const [disks, details] = await Promise.all([
            api.get(`/api/Computer/${computerId}/disks`), // /api/Computer/{id}/disks
            api.get(`/api/Computer/${computerId}`)        // /api/Computer/{id}
        ]);

        document.getElementById('cpuThresholdInput').value = details.cpuThreshold || "";
        document.getElementById('ramThresholdInput').value = details.ramThreshold || "";

        const container = document.getElementById('diskThresholdsContainer');
        container.innerHTML = '';

        if (disks.length === 0) container.innerHTML = '<div class="text-muted text-center">Disk verisi yok.</div>';
        else {
            disks.forEach(d => {
                container.innerHTML += `
                <div class="d-flex align-items-center justify-content-between mb-2 p-2 rounded border border-secondary" style="background: rgba(255,255,255,0.05);">
                    <div>
                        <span class="fw-bold text-info">${d.diskName}</span> 
                        <small class="text-muted ms-1">(${d.totalSizeGb.toFixed(0)} GB)</small>
                    </div>
                    <div class="input-group input-group-sm" style="width: 120px;">
                        <input type="number" class="form-control bg-dark text-white border-secondary disk-threshold-input" 
                               data-name="${d.diskName}" 
                               value="${d.thresholdPercent || ""}" 
                               min="0" max="100">
                        <span class="input-group-text bg-secondary text-white border-secondary">%</span>
                    </div>
                </div>`;
            });
        }

        thresholdModalInstance.show();
    } catch (e) { alert("Ayarlar yüklenemedi: " + e.message); }
};

window.saveThresholdsWithValidation = async function () {
    const id = document.getElementById('modalComputerId').value;
    const cpu = document.getElementById('cpuThresholdInput').value;
    const ram = document.getElementById('ramThresholdInput').value;

    const disks = [];
    document.querySelectorAll('.disk-threshold-input').forEach(i => {
        disks.push({
            diskName: i.getAttribute('data-name'),
            thresholdPercent: i.value ? parseFloat(i.value) : null
        });
    });

    try {
        // GÜNCEL ENDPOINT: ComputerController
        await api.put(`/api/Computer/update-thresholds/${id}`, {
            cpuThreshold: cpu ? parseFloat(cpu) : null,
            ramThreshold: ram ? parseFloat(ram) : null,
            diskThresholds: disks
        });
        thresholdModalInstance.hide();
        loadAgents();
        alert("✅ Eşik değerleri kaydedildi.");
    } catch (e) { alert(e.message); }
};

// --- BAŞLAT ---
$(document).ready(function () {
    if (document.getElementById('tagSelect')) {
        loadFilterTags();
    }

    // agentRows elementinin varlığı loadAgents içinde kontrol edildiği için
    // interval'i güvenle başlatabiliriz.
    setInterval(loadAgents, 10000);
});