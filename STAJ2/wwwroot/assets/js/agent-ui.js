// STAJ2/wwwroot/assets/js/agent-ui.js

let selectedTags = [];
let allAgents = [];

// --- 1. ANA TABLO VE FİLTRELEME ---

window.applyFilter = () => {
    selectedTags = $('#tagSelect').val() || [];
    renderTable();
};

async function loadFilterTags() {
    try {
        const tags = await api.get("/api/Users/tags");

        // Hem filtre hem modal select'lerini doldur
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

    const filtered = selectedTags.length === 0
        ? allAgents
        : allAgents.filter(a => selectedTags.every(t => a.tags && a.tags.includes(t)));

    tbody.innerHTML = filtered.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";

        const tags = (a.tags || []).map(t => `<span class="pill" style="font-size:0.65rem; margin-right:3px;">${t}</span>`).join("");

        // DÜZELTME: openThresholdSettings artık sadece ID alıyor
        const actionButtons = canEdit ? `
            <div style="display:flex; gap:5px;">
                <button class="btn primary border small" onclick="handleRename(${a.computerId}, '${a.displayName || a.machineName}')" title="İsim Değiştir">✏️</button>
                <button class="btn warning border small" onclick="openThresholdSettings(${a.computerId})" title="Limit Ayarları">⚙️</button>
                <button class="btn ghost border small" onclick="openTagModal(${a.computerId})" title="Etiketle">🏷️</button>
            </div>` : "";

        let diskContent = "-";
        if (a.diskUsage) {
            diskContent = `<span style="font-size:0.8rem; color:var(--text-main);">${a.diskUsage}</span>`;
        }

        const ipDisplay = a.ip || "-";

        // Tabloda gösterilen renkler için yine de elimizdeki veriyi kullanıyoruz (varsayılan 90)
        // Çünkü bu sadece anlık uyarı rengi, veritabanı kaydı değil.
        const cpuLimit = a.cpuThreshold || 90;
        const ramLimit = a.ramThreshold || 90;

        const cpuColor = (a.cpuUsage > cpuLimit) ? "#ef4444" : "var(--text-main)";
        const ramColor = (a.ramUsage > ramLimit) ? "#ef4444" : "var(--text-main)";

        return `
            <tr>
                <td>
                    <div class="fw-bold" style="color:var(--text-title);">${a.displayName || a.machineName}</div>
                    <div style="margin-top:2px;">${tags}</div>
                    <div style="font-size:0.7rem; color:var(--text-muted); margin-top:2px;">${ts}</div>
                </td>
                <td style="color:var(--text-muted); font-family:monospace;">${ipDisplay}</td>
                <td style="font-weight:bold; color:${cpuColor};">${Math.round(a.cpuUsage)}%</td>
                <td style="font-weight:bold; color:${ramColor};">${Math.round(a.ramUsage)}%</td>
                <td>${diskContent}</td>
                <td><small style="color:var(--text-muted);">Aktif</small></td>
                ${canEdit ? `<td>${actionButtons}</td>` : ''}
            </tr>
        `;
    }).join("");
}

// --- 2. YÖNETİM FONKSİYONLARI ---

window.handleRename = (id, currentName) => {
    document.getElementById("renameComputerId").value = id;
    document.getElementById("currentComputerName").value = currentName;
    document.getElementById("newComputerName").value = "";
    const modal = new bootstrap.Modal(document.getElementById("renameModal"));
    modal.show();
};

window.saveComputerName = async () => {
    const id = document.getElementById("renameComputerId").value;
    const newName = document.getElementById("newComputerName").value;
    if (!newName) return alert("Yeni isim giriniz");
    try {
        await api.put(`/api/Computer/update-display-name`, { id: parseInt(id), newDisplayName: newName });
        const modal = bootstrap.Modal.getInstance(document.getElementById("renameModal"));
        modal.hide();
        loadAgents();
    } catch (e) { alert(e.message); }
};

window.openTagModal = async (id) => {
    document.getElementById("tagModalComputerId").value = id;
    const modal = new bootstrap.Modal(document.getElementById("tagsModal"));

    // Select2'yi temizle/güncelle
    const agent = allAgents.find(a => a.computerId == id);
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
    } catch (e) { alert(e.message); }
};

// DÜZELTİLEN FONKSİYON: Limit Ayarları
// Artık parametre olarak cpu/ram almıyor, gidip veritabanından çekiyor.
window.openThresholdSettings = async (id) => {
    document.getElementById("modalComputerId").value = id;

    // Yükleniyor durumuna getir
    document.getElementById("cpuThresholdInput").value = "";
    document.getElementById("ramThresholdInput").value = "";
    document.getElementById("cpuThresholdInput").placeholder = "Yükleniyor...";
    document.getElementById("ramThresholdInput").placeholder = "Yükleniyor...";

    const container = document.getElementById("diskThresholdsContainer");
    container.innerHTML = '<div class="text-center"><div class="spinner-border spinner-border-sm text-info"></div></div>';

    try {
        // 1. Bilgisayarın GENEL bilgilerini (CPU/RAM limitleri) çek
        const computerData = await api.get(`/api/Computer/${id}`);
        document.getElementById("cpuThresholdInput").value = computerData.cpuThreshold || "";
        document.getElementById("ramThresholdInput").value = computerData.ramThreshold || "";

        document.getElementById("cpuThresholdInput").placeholder = "Örn: 80";
        document.getElementById("ramThresholdInput").placeholder = "Örn: 90";

        // 2. Diskleri çek
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
        document.getElementById("cpuThresholdInput").placeholder = "Hata";
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
            diskThresholdsList.push({
                diskName: name,
                thresholdPercent: val
            });
        }
    });

    const payload = {
        cpuThreshold: cpu,
        ramThreshold: ram,
        diskThresholds: diskThresholdsList
    };

    try {
        await api.put(`/api/Computer/update-thresholds/${id}`, payload);
        const modal = bootstrap.Modal.getInstance(document.getElementById("thresholdModal"));
        modal.hide();
        loadAgents();
    } catch (e) {
        alert("Hata: " + e.message);
    }
};

// --- BAŞLATMA ---
$(document).ready(function () {
    $('#modalTagSelect').select2({
        dropdownParent: $('#tagsModal'),
        tags: true,
        placeholder: "Etiket seçin veya yazın..."
    });

    loadFilterTags();
    loadAgents();
    setInterval(loadAgents, 5000);
});